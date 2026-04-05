using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class PaintableObject : MonoBehaviour
{
    [Header("Tracking Resolution (lower = faster)")]
    [SerializeField]
    int trackingResolution = 128;
    int cleanThreshold = 200; // 255: fully clean

    public RenderTexture maskTexture { get; set; }

    RenderTexture coverageTexture;
    Texture2D coverageReadable;

    NativeArray<byte> coverageData;

    bool[] isClean;

    int cleanablePixelCount;
    int cleanedPixelCount;

    Renderer meshRenderer;
    Mesh mesh;

    Texture2D maskReadable;

    int visualResolution;

    // ----------------------------------------------------
    // INITIALIZATION
    // ----------------------------------------------------

    public void Initialize(RenderTexture runtimeMask)
    {
        maskTexture = runtimeMask;

        visualResolution = maskTexture.width;

        meshRenderer = GetComponent<Renderer>();
        mesh = GetComponent<MeshFilter>().sharedMesh;

        CreateCoverageTexture();
        InitializeCleanTracking();

        maskReadable = new Texture2D(
            trackingResolution,
            trackingResolution,
            TextureFormat.R8,
            false);
    }

    // ----------------------------------------------------
    // CREATE LOW-RES COVERAGE MASK
    // ----------------------------------------------------

    void CreateCoverageTexture()
    {
        coverageTexture = new RenderTexture(
            trackingResolution,
            trackingResolution,
            0,
            RenderTextureFormat.R8);

        coverageTexture.Create();

        Material mat =
            new Material(Shader.Find("Hidden/CoverageMask"));

        CommandBuffer cmd = new CommandBuffer();

        cmd.SetRenderTarget(coverageTexture);

        cmd.ClearRenderTarget(false, true, Color.black);

        cmd.DrawMesh(
            mesh,
            transform.localToWorldMatrix,
            mat);

        Graphics.ExecuteCommandBuffer(cmd);

        RenderTexture.active = coverageTexture;

        coverageReadable = new Texture2D(
            trackingResolution,
            trackingResolution,
            TextureFormat.R8,
            false);

        coverageReadable.ReadPixels(
            new Rect(0, 0, trackingResolution, trackingResolution),
            0, 0);

        coverageReadable.Apply();

        RenderTexture.active = null;

        coverageData =
            coverageReadable.GetRawTextureData<byte>();
    }

    // ----------------------------------------------------
    // INITIALIZE CLEAN TRACKING
    // ----------------------------------------------------

    void InitializeCleanTracking()
    {
        int size = trackingResolution * trackingResolution;

        isClean = new bool[size];

        cleanablePixelCount = 0;
        cleanedPixelCount = 0;

        for (int i = 0; i < size; i++)
        {
            if (coverageData[i] > 0)
                cleanablePixelCount++;
        }

        Debug.Log($"{name} cleanable pixels: {cleanablePixelCount}");
    }

    // ----------------------------------------------------
    // UPDATE CLEAN PROGRESS (FAST)
    // ----------------------------------------------------

    public void UpdateCleanPixels(Vector2 uv, float brushSizeUV)
    {
        if (cleanablePixelCount == 0)
            return;

        // Downscale visual mask into tracking resolution
        Graphics.Blit(maskTexture, coverageTexture);

        RenderTexture.active = coverageTexture;

        maskReadable.ReadPixels(
            new Rect(0, 0, trackingResolution, trackingResolution),
            0, 0);

        maskReadable.Apply();

        RenderTexture.active = null;

        var dirt =
            maskReadable.GetRawTextureData<byte>();

        int width = trackingResolution;

        int centerX =
            Mathf.RoundToInt(uv.x * width);

        int centerY =
            Mathf.RoundToInt(uv.y * width);

        int radius =
            Mathf.CeilToInt(
                brushSizeUV * width * 0.5f);

        int xMin = Mathf.Max(0, centerX - radius);
        int xMax = Mathf.Min(width, centerX + radius);

        int yMin = Mathf.Max(0, centerY - radius);
        int yMax = Mathf.Min(width, centerY + radius);

        for (int y = yMin; y < yMax; y++)
        {
            for (int x = xMin; x < xMax; x++)
            {
                int i = y * width + x;

                if (!isClean[i] &&
                    coverageData[i] > 0 &&
                    dirt[i] <= cleanThreshold)
                {
                    isClean[i] = true;
                    cleanedPixelCount++;
                }
            }
        }
    }

    // ----------------------------------------------------
    // GET CLEAN PERCENT (VERY FAST)
    // ----------------------------------------------------

    public float GetCleanPercent()
    {
        if (cleanablePixelCount == 0)
            return 0;

        return (float)cleanedPixelCount /
               cleanablePixelCount * 100f;
    }
}
