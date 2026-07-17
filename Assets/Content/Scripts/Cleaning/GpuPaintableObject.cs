using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class GPUPaintableObject : MonoBehaviour
{
    static readonly int DirtMaskShaderId = Shader.PropertyToID("_DirtMask");
    static readonly int LocalCleanMaskShaderId = Shader.PropertyToID("_LocalCleanMask");
    static readonly int JobHighlightShaderId = Shader.PropertyToID("_JobHighlight");
    static readonly int CleanFlashShaderId = Shader.PropertyToID("_CleanFlash");
    static readonly int GooReadyGlowShaderId = Shader.PropertyToID("_GooReadyGlow");

    public UnityAction OnInitialize;
    public UnityAction OnProgress;
    public UnityAction OnCleaned;
    public int winCoins = 1;
    public List<GPUPaintableObject> AdditionalObjectsToClean = new();
    public Transform CoinSpawnPos;
    [Header("Tracking Resolution")]
    [SerializeField] int trackingResolution = 128;

    [Header("Clean threshold")]
    [SerializeField] int cleanThreshold = 200;
    [SerializeField] float cleanPercentage = 0.85f;
    [Header("Area")]
    [SerializeField] bool countsTowardAreaProgress = true;
    [SerializeField] bool useContinuousProgress;
    [Header("Tool Interaction")]
    [SerializeField] bool allowGooCleaning = false;
    [Header("Growable")]
    [SerializeField] bool deferCleanMaterialSwapUntilGrowComplete;

    const string OutlineRenderingLayerName = "Outline";
    static uint outlineRenderingLayerMask;
    public RenderTexture maskTexture;
    public RenderTexture coverageTexture;
    public Texture2D coverageReadable;
    NativeArray<byte> coverageData;

    public Texture2D maskReadable;
    public bool isClean;
    bool[] cleanedPixels;

    int pixelsToCleanCount;
    int cleanedPixelCount;
    uint defaultRenderingLayerMask;
    Mesh mesh;
    Renderer cachedRenderer;
    MaterialPropertyBlock propertyBlock;
    public bool IsInitialized { get; private set; }
    public bool CountsTowardAreaProgress => countsTowardAreaProgress;
    public bool UseContinuousProgress => useContinuousProgress;
    public bool DeferCleanMaterialSwapUntilGrowComplete
    {
        get => deferCleanMaterialSwapUntilGrowComplete;
        set => deferCleanMaterialSwapUntilGrowComplete = value;
    }

    readonly CleanFlashPlayer cleanFlashPlayer = new();
    Renderer[] flashRenderers;
    bool usesCleanMaterial;
    bool pendingMaterialFinalize;

    MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

    void EnsureRenderer()
    {
        if (cachedRenderer != null)
            return;

        cachedRenderer = GetComponent<Renderer>();
        flashRenderers = new[] { cachedRenderer };
        defaultRenderingLayerMask = cachedRenderer.renderingLayerMask;
    }

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        EnsureRenderer();
        _ = PropertyBlock;
    }

    public void SetAimOutline(bool enabled)
    {
        EnsureRenderer();
        cachedRenderer.renderingLayerMask = enabled ? GetOutlineRenderingLayerMask() : defaultRenderingLayerMask;
    }

    static uint GetOutlineRenderingLayerMask()
    {
        if (outlineRenderingLayerMask != 0)
            return outlineRenderingLayerMask;

        outlineRenderingLayerMask = RenderingLayerMask.GetMask(OutlineRenderingLayerName);
        if (outlineRenderingLayerMask == 0)
            throw new System.InvalidOperationException($"Rendering layer '{OutlineRenderingLayerName}' is missing. Add it in Project Settings > Tags and Layers > Rendering Layers.");

        return outlineRenderingLayerMask;
    }

    void OnDisable()
    {
        SetAimOutline(false);
    }

    // ----------------------------------------------------
    // INITIALIZE
    // ----------------------------------------------------

    public void Initialize(RenderTexture runtimeMask)
    {
        IsInitialized = true;
        maskTexture = runtimeMask;
        BindMaskToRenderer(maskTexture);

        BakeCoverage();
        InitializeTracking();

        maskReadable = new Texture2D(trackingResolution, trackingResolution, TextureFormat.R8, false);
    }

    public void Initialize(int resolution)
    {
        IsInitialized = true;
        RenderTexture mask = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);

        mask.Create();

        Graphics.Blit(Texture2D.whiteTexture, mask);

        Initialize(mask);

        OnInitialize?.Invoke();
    }

    void BindMaskToRenderer(RenderTexture runtimeMask)
    {
        EnsureRenderer();
        cachedRenderer.GetPropertyBlock(PropertyBlock);
        PropertyBlock.SetTexture(DirtMaskShaderId, runtimeMask);
        PropertyBlock.SetTexture(LocalCleanMaskShaderId, runtimeMask);
        cachedRenderer.SetPropertyBlock(PropertyBlock);
    }

    public void SetJobHighlight(float amount)
    {
        if (usesCleanMaterial)
            return;

        EnsureRenderer();
        cachedRenderer.GetPropertyBlock(PropertyBlock);
        PropertyBlock.SetFloat(JobHighlightShaderId, Mathf.Clamp01(amount));
        cachedRenderer.SetPropertyBlock(PropertyBlock);
    }

    public void SetCleanFlash(float amount)
    {
        if (usesCleanMaterial)
            return;

        EnsureRenderer();
        cachedRenderer.GetPropertyBlock(PropertyBlock);
        PropertyBlock.SetFloat(CleanFlashShaderId, Mathf.Clamp01(amount));
        cachedRenderer.SetPropertyBlock(PropertyBlock);
    }

    public void SetGooReadyGlow(float amount)
    {
        if (usesCleanMaterial)
            return;

        EnsureRenderer();
        cachedRenderer.GetPropertyBlock(PropertyBlock);
        PropertyBlock.SetFloat(GooReadyGlowShaderId, Mathf.Clamp01(amount));
        cachedRenderer.SetPropertyBlock(PropertyBlock);
    }

    public void FinalizeCleanMaterial()
    {
        SetGooReadyGlow(0f);

        if (usesCleanMaterial)
            return;

        PlayCleanFlash(finalizeAfterFlash: true);
    }

    public void FinalizeCleanMaterialWithoutFlash()
    {
        SetGooReadyGlow(0f);

        if (usesCleanMaterial)
            return;

        if (CanSwapToCleanMaterial())
            SwapToCleanMaterial();
        else
            ReleaseMaskTexture();
    }

    public void StopCleanFlash()
    {
        EnsureRenderer();
        cleanFlashPlayer.Stop(invalidateRunning: true);
        if (!usesCleanMaterial)
            cleanFlashPlayer.ResetFlash(flashRenderers);
    }

    public Renderer GetFlashRenderer()
    {
        EnsureRenderer();
        return cachedRenderer;
    }

    // ----------------------------------------------------
    // BAKE UV COVERAGE
    // ----------------------------------------------------

    void BakeCoverage()
    {
        coverageTexture = new RenderTexture(trackingResolution, trackingResolution, 0, RenderTextureFormat.ARGB32);
        coverageTexture.Create();

        Shader bakeShader = Shader.Find("Hidden/CoverageDirtMask");
        Material bakeMaterial = new Material(bakeShader);

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
        cleanFlashPlayer.Stop(invalidateRunning: true);
        if (!usesCleanMaterial && cachedRenderer != null)
            cleanFlashPlayer.ResetFlash(flashRenderers);

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

        for (int pixelIndex = 0; pixelIndex < size; pixelIndex++)
        {
            int byteIndex = pixelIndex * 4;
            bool hasCoverage = rawPixels[byteIndex] > 0;

            if (hasCoverage)
                pixelsToCleanCount++;
            else
                cleanedPixels[pixelIndex] = true;
        }

        Debug.Log($"{name} dirty pixels: {pixelsToCleanCount}");

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
        if (isClean)
            return;

        cleanedPixelCount = pixelsToCleanCount;
        isClean = true;
        // Clear visual dirt
        RenderTexture.active = maskTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
        if (!deferCleanMaterialSwapUntilGrowComplete)
            PlayCleanFlash();

        OnCleaned?.Invoke();
    }

    void PlayCleanFlash(bool finalizeAfterFlash = false)
    {
        if (usesCleanMaterial)
            return;

        EnsureRenderer();
        pendingMaterialFinalize = finalizeAfterFlash;
        cleanFlashPlayer.Play(flashRenderers, OnCleanFlashComplete);
    }

    void OnCleanFlashComplete()
    {
        if (pendingMaterialFinalize)
        {
            pendingMaterialFinalize = false;

            if (CanSwapToCleanMaterial())
                SwapToCleanMaterial();
            else
                ReleaseMaskTexture();
        }
        else if (deferCleanMaterialSwapUntilGrowComplete)
            ReleaseMaskTexture();
        else if (CanSwapToCleanMaterial())
            SwapToCleanMaterial();
        else
            ReleaseMaskTexture();
    }

    bool CanSwapToCleanMaterial()
    {
        EnsureRenderer();
        Material[] materials = cachedRenderer.sharedMaterials;
        foreach (Material material in materials)
        {
            if (Managers.Materials.TryGetCleanReplacement(material, out _))
                return true;
        }

        return false;
    }

    void SwapToCleanMaterial()
    {
        EnsureRenderer();
        Material[] materials = cachedRenderer.sharedMaterials;
        bool changed = false;

        for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
        {
            if (!Managers.Materials.TryGetCleanReplacement(materials[materialIndex], out Material cleanMaterial))
                continue;

            materials[materialIndex] = cleanMaterial;
            changed = true;
        }

        if (!changed)
            return;

        cachedRenderer.SetPropertyBlock(null);
        cachedRenderer.sharedMaterials = materials;
        usesCleanMaterial = true;
        ReleaseMaskTexture();
    }

    void ReleaseMaskTexture()
    {
        if (maskTexture == null)
            return;

        maskTexture.Release();
        Destroy(maskTexture);
        maskTexture = null;
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

    public float GetProgressContribution()
    {
        if (useContinuousProgress)
            return GetCleanPercent();

        return isClean ? 1f : 0f;
    }

    public bool AllowGooCleaning => allowGooCleaning;
}
