using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;
using Unity.Collections;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class GPUPaintableObject : MonoBehaviour
{
    static readonly int DirtMaskShaderId = Shader.PropertyToID("_DirtMask");
    static readonly int LocalCleanMaskShaderId = Shader.PropertyToID("_LocalCleanMask");
    static readonly int UseZoneDirtShaderId = Shader.PropertyToID("_UseZoneDirt");
    static readonly int JobHighlightShaderId = Shader.PropertyToID("_JobHighlight");
    static readonly int CleanFlashShaderId = Shader.PropertyToID("_CleanFlash");

    public UnityAction OnInitialize;
    public UnityAction OnProgress;
    public UnityAction OnCleaned;
    public int winCoins = 1;
    public int winDirt = 3;
    public List<GPUPaintableObject> AdditionalObjectsToClean = new();
    public Transform CoinSpawnPos;
    [Header("Tracking Resolution")]
    [SerializeField] int trackingResolution = 128;

    [Header("Clean threshold")]
    [SerializeField] int cleanThreshold = 200;
    [SerializeField] float cleanPercentage = 0.85f;
    [Header("Tool Interaction")]
    [SerializeField] bool allowGooCleaning = false;
    [Header("Clean Flash")]
    const float cleanFlashDuration = 1f;
    const float cleanFlashPeak = 1f;
    public RenderTexture maskTexture;
    public RenderTexture coverageTexture;
    public Texture2D coverageReadable;
    NativeArray<byte> coverageData;

    public Texture2D maskReadable;
    public bool isClean;
    bool[] cleanedPixels;

    int pixelsToCleanCount;
    int cleanedPixelCount;
    Mesh mesh;
    Renderer cachedRenderer;
    MaterialPropertyBlock propertyBlock;
    public bool IsInitialized { get; private set; }
    float nextDirtSpawn = 0f;
    float dirtSpawnStep;
    int dirtSpawnedCount = 0;
    Coroutine cleanFlashRoutine;

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        cachedRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    // ----------------------------------------------------
    // INITIALIZE
    // ----------------------------------------------------

    public void Initialize(RenderTexture runtimeMask)
    {
        IsInitialized = true;
        maskTexture = runtimeMask;
        BindMaskToRenderer(maskTexture);

        BakeCoverageAndDirt();
        InitializeTracking();

        maskReadable = new Texture2D(trackingResolution, trackingResolution, TextureFormat.R8, false);
    }

    public void Initialize(int resolution)
    {
        IsInitialized = true;
        RenderTexture mask = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);

        mask.Create();

        Graphics.Blit(Texture2D.whiteTexture, mask);

        if (winDirt > 0)
        {
            dirtSpawnStep = 1f / winDirt;
            nextDirtSpawn = dirtSpawnStep;
            dirtSpawnedCount = 0;
        }

        Initialize(mask);

        OnInitialize?.Invoke();
    }

    void BindMaskToRenderer(RenderTexture runtimeMask)
    {
        cachedRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(DirtMaskShaderId, runtimeMask);
        propertyBlock.SetTexture(LocalCleanMaskShaderId, runtimeMask);
        cachedRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetJobHighlight(float amount)
    {
        cachedRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(JobHighlightShaderId, Mathf.Clamp01(amount));
        cachedRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetCleanFlash(float amount)
    {
        cachedRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(CleanFlashShaderId, Mathf.Clamp01(amount));
        cachedRenderer.SetPropertyBlock(propertyBlock);
    }

    ZoneDirtMap ResolveZoneDirtMap()
    {
        DirtArea dirtArea = GetComponentInParent<DirtArea>();
        if (dirtArea != null)
            return dirtArea.ZoneDirtMap;

        return GetComponentInParent<ZoneDirtMap>();
    }

    // ----------------------------------------------------
    // BAKE UV COVERAGE + ZONE DIRT (one draw, one readback)
    // ----------------------------------------------------

    void BakeCoverageAndDirt()
    {
        coverageTexture = new RenderTexture(trackingResolution, trackingResolution, 0, RenderTextureFormat.ARGB32);
        coverageTexture.Create();

        Shader bakeShader = Shader.Find("Hidden/CoverageDirtMask");
        Material bakeMaterial = new Material(bakeShader);

        ZoneDirtMap zoneDirtMap = ResolveZoneDirtMap();
        if (zoneDirtMap != null)
            zoneDirtMap.BindToBakeMaterial(bakeMaterial);
        else
            bakeMaterial.SetFloat(UseZoneDirtShaderId, 0f);

        CommandBuffer cmd = new CommandBuffer();
        cmd.SetRenderTarget(coverageTexture);
        cmd.ClearRenderTarget(false, true, Color.black);
        cmd.DrawMesh(mesh, transform.localToWorldMatrix, bakeMaterial);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();
        Destroy(bakeMaterial);

        RenderTexture.active = coverageTexture;

        coverageReadable = new Texture2D(trackingResolution, trackingResolution, TextureFormat.RGBA32, false);
        coverageReadable.ReadPixels(new Rect(0, 0, trackingResolution, trackingResolution), 0, 0);
        coverageReadable.Apply();

        RenderTexture.active = null;

        NativeArray<byte> rawPixels = coverageReadable.GetRawTextureData<byte>();
        int pixelCount = trackingResolution * trackingResolution;
        coverageData = new NativeArray<byte>(pixelCount, Allocator.Persistent);

        for (int pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
        {
            int byteIndex = pixelIndex * 4;
            coverageData[pixelIndex] = rawPixels[byteIndex];
        }

        rawPixels.Dispose();
    }

    void OnDestroy()
    {
        if (coverageData.IsCreated)
            coverageData.Dispose();
    }

    // ----------------------------------------------------
    // INIT TRACKING
    // ----------------------------------------------------

    void InitializeTracking()
    {
        int size = trackingResolution * trackingResolution;

        cleanedPixels = new bool[size];

        pixelsToCleanCount = 0;
        cleanedPixelCount = 0;

        NativeArray<byte> rawPixels = coverageReadable.GetRawTextureData<byte>();
        bool usesZoneDirt = ResolveZoneDirtMap() != null;

        for (int pixelIndex = 0; pixelIndex < size; pixelIndex++)
        {
            int byteIndex = pixelIndex * 4;
            bool hasCoverage = rawPixels[byteIndex] > 0;
            byte initialDirt = rawPixels[byteIndex + 1];
            bool hasDirt = usesZoneDirt ? initialDirt > cleanThreshold : hasCoverage;

            if (hasCoverage && hasDirt)
                pixelsToCleanCount++;
            else
                cleanedPixels[pixelIndex] = true;
        }

        Debug.Log($"{name} dirty pixels: {pixelsToCleanCount} (zone-aware: {usesZoneDirt})");

        if (pixelsToCleanCount == 0)
        {
            isClean = true;
            return;
        }

        pixelsToCleanCount = Mathf.Max(Mathf.RoundToInt(pixelsToCleanCount * cleanPercentage), 1);
        Debug.Log($"{name} target after {cleanPercentage:P0}: {pixelsToCleanCount}");
    }

    // ----------------------------------------------------
    // UPDATE TRACKING FROM GPU MASK
    // ----------------------------------------------------

    public void UpdateTracking(Vector3 hitPos)
    {
        if (isClean)
            return;

        if (pixelsToCleanCount == 0)
            return;

        RenderTexture maskDownscale = RenderTexture.GetTemporary(trackingResolution, trackingResolution, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(maskTexture, maskDownscale);

        RenderTexture.active = maskDownscale;

        maskReadable.ReadPixels(new Rect(0, 0, trackingResolution, trackingResolution), 0, 0);

        maskReadable.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(maskDownscale);

        var maskData = maskReadable.GetRawTextureData<byte>();

        int size = maskData.Length;

        for (int i = 0; i < size; i++)
        {
            if (!cleanedPixels[i] && maskData[i] <= cleanThreshold)
            {
                cleanedPixels[i] = true;
                cleanedPixelCount++;
            }
        }

        float currentPercent = GetCleanPercent();

        while (winDirt > 0 && currentPercent >= nextDirtSpawn)
        {
            SpawnDirtChunk(hitPos);
            dirtSpawnedCount++;
            nextDirtSpawn = dirtSpawnStep * (dirtSpawnedCount + 1);
        }

        if (currentPercent > cleanPercentage)
        {
            SetClean();
            if (AdditionalObjectsToClean.Count > 0)
            {
                foreach (var obj in AdditionalObjectsToClean)
                {
                    if (!obj.IsInitialized)
                        obj.Initialize(128);
                    obj.SetClean();
                }
            }
        }

        Managers.UI.SetCleanProgressBarPercent(currentPercent * 100f);

        OnProgress?.Invoke();
    }

    public void SetClean()
    {
        cleanedPixelCount = pixelsToCleanCount;
        isClean = true;
        // Clear visual dirt
        RenderTexture.active = maskTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
        PlayCleanFlash();
        OnCleaned?.Invoke();
    }

    void PlayCleanFlash()
    {
        if (cleanFlashRoutine != null)
            StopCoroutine(cleanFlashRoutine);

        cleanFlashRoutine = StartCoroutine(CleanFlashRoutine());
    }

    IEnumerator CleanFlashRoutine()
    {
        float elapsed = 0f;

        while (elapsed < cleanFlashDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / cleanFlashDuration;
            float fade = 1f - normalizedTime;
            SetCleanFlash(cleanFlashPeak * fade * fade);
            yield return null;
        }

        SetCleanFlash(0f);
        cleanFlashRoutine = null;
    }

    void SpawnDirtChunk(Vector3 hitPos)
    {
        Vector3 dirToPlayer = (Managers.Player.transform.position - hitPos).normalized;
        dirToPlayer.y = 1f;
        int rngAmount = Random.Range(1, 4);
        Managers.Spawning.SpawnTempChunks(rngAmount, hitPos + dirToPlayer * 0.15f, Vector3.up).Forget();
    }

    // ----------------------------------------------------
    // GET PERCENT
    // ----------------------------------------------------

    public float GetCleanPercent()
    {
        if (pixelsToCleanCount == 0)
            return 0;

        if (isClean)
            return 1f;

        return (float)cleanedPixelCount / (float)pixelsToCleanCount;
    }

    public bool AllowGooCleaning => allowGooCleaning;
}
