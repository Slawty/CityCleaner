using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Events;
using Unity.Collections;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class GPUPaintableObject : MonoBehaviour
{
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
    static readonly int DirtMaskShaderId = Shader.PropertyToID("_DirtMask");
    static readonly int LocalCleanMaskShaderId = Shader.PropertyToID("_LocalCleanMask");
    public bool IsInitialized { get; private set; }
    float nextDirtSpawn = 0f;
    float dirtSpawnStep;
    int dirtSpawnedCount = 0;

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

        CreateCoverageTexture();
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

    // ----------------------------------------------------
    // CREATE UV COVERAGE MASK (UNCHANGED)
    // ----------------------------------------------------

    void CreateCoverageTexture()
    {
        coverageTexture = new RenderTexture(trackingResolution, trackingResolution, 0, RenderTextureFormat.ARGB32);

        coverageTexture.Create();

        Material mat = new Material(Shader.Find("Hidden/CoverageMask"));

        CommandBuffer cmd = new CommandBuffer();

        cmd.SetRenderTarget(coverageTexture);

        cmd.ClearRenderTarget(false, true, Color.black);

        cmd.DrawMesh(mesh, Matrix4x4.identity, mat);

        Graphics.ExecuteCommandBuffer(cmd);

        RenderTexture.active = coverageTexture;

        coverageReadable = new Texture2D(trackingResolution, trackingResolution, TextureFormat.R8, false);

        coverageReadable.ReadPixels(new Rect(0, 0, trackingResolution, trackingResolution), 0, 0);

        coverageReadable.Apply();

        RenderTexture.active = null;

        coverageData = coverageReadable.GetRawTextureData<byte>();
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

        for (int i = 0; i < size; i++)
        {
            if (coverageData[i] > 0)
                pixelsToCleanCount++;
        }

        Debug.Log($"{name} real cleanable pixels: {pixelsToCleanCount}");
        pixelsToCleanCount = Mathf.RoundToInt(pixelsToCleanCount * cleanPercentage);
        Debug.Log($"{name} reduced to {cleanPercentage}%: {pixelsToCleanCount}. Total Size: {size}");
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

        // Downscale GPU mask
        Graphics.Blit(maskTexture, coverageTexture);

        RenderTexture.active = coverageTexture;

        maskReadable.ReadPixels(new Rect(0, 0, trackingResolution, trackingResolution), 0, 0);

        maskReadable.Apply();

        RenderTexture.active = null;

        var maskData = maskReadable.GetRawTextureData<byte>();

        int size = maskData.Length;

        for (int i = 0; i < size; i++)
        {
            // Only count valid UV pixels
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
        OnCleaned?.Invoke();
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
