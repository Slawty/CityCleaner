using UnityEngine;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;

public class GPUPainterWorld : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] Camera cam;
    [SerializeField] LayerMask paintMask;
    [SerializeField] int textureResolution = 1024;

    [Header("Brush")]
    [SerializeField] Texture2D brushTexture;
    [SerializeField] float brushWorldSize = 0.5f;
    [SerializeField] float cleanSpeed = 4f;

    Material localPaintMaterial;
    float nextUpdateTime;
    bool isPainting;

    void Awake()
    {
        localPaintMaterial = new Material(Shader.Find("Hidden/GPUPaintBrush"));
    }

    void Update()
    {
        if (!isPainting)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, paintMask, QueryTriggerInteraction.Ignore))
            return;

        GPUPaintableObject paintable = hit.collider.GetComponentInParent<GPUPaintableObject>();
        if (paintable != null)
        {
            PaintLocalMask(paintable, hit.point, cleanSpeed * Time.deltaTime);
            return;
        }

        DirtNest dirtNest = hit.collider.GetComponentInParent<DirtNest>();
        if (dirtNest != null)
            dirtNest.ApplyWaterDamage(cleanSpeed * Time.deltaTime);
    }

    public void StartPainting()
    {
        isPainting = true;
    }

    public void StopPainting()
    {
        isPainting = false;
    }

    void PaintLocalMask(GPUPaintableObject paintable, Vector3 hitPos, float cleanStrength)
    {
        if (paintable.isClean)
            return;

        if (!paintable.IsInitialized)
            paintable.Initialize(textureResolution);

        Renderer targetRenderer = paintable.GetComponent<Renderer>();
        if (targetRenderer == null)
            return;

        localPaintMaterial.SetVector("_BrushWorldPos", hitPos);
        localPaintMaterial.SetFloat("_BrushSize", brushWorldSize);
        localPaintMaterial.SetFloat("_Strength", cleanStrength);
        localPaintMaterial.SetTexture("_BrushTex", brushTexture);

        RenderTexture temp = RenderTexture.GetTemporary(paintable.maskTexture.descriptor);
        Graphics.Blit(paintable.maskTexture, temp);
        localPaintMaterial.SetTexture("_MainTex", temp);

        CommandBuffer commandBuffer = new CommandBuffer();
        commandBuffer.name = "GPUPaintLocalMask";
        commandBuffer.SetRenderTarget(paintable.maskTexture);
        commandBuffer.DrawRenderer(targetRenderer, localPaintMaterial);

        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Release();
        RenderTexture.ReleaseTemporary(temp);

        if (Time.time <= nextUpdateTime)
            return;

        paintable.UpdateTracking(hitPos);
        nextUpdateTime = Time.time + 0.1f;

        if (!paintable.isClean)
            return;

        Vector3 dirToPlayer = (transform.position - hitPos).normalized;
        dirToPlayer.y = 1f;
        Vector3 coinSpawnPos = paintable.CoinSpawnPos == null ? hitPos : paintable.CoinSpawnPos.position;
        coinSpawnPos += dirToPlayer * 0.25f;
        Managers.Spawning.SpawnCoins(paintable.winCoins, coinSpawnPos, dirToPlayer).Forget();
    }
}
