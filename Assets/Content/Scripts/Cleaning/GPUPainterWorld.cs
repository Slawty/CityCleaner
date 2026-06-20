using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;

public class GPUPainterWorld : MonoBehaviour
{
    const int OverlapBufferSize = 32;

    [Header("Setup")]
    [SerializeField] Camera cam;
    [SerializeField] LayerMask paintMask;
    [SerializeField] int textureResolution = 1024;

    [Header("Brush")]
    [SerializeField] Texture2D brushTexture;
    [SerializeField] float brushWorldSize = 0.5f;
    [SerializeField] float cleanSpeed = 4f;

    readonly Collider[] overlapColliders = new Collider[OverlapBufferSize];
    readonly HashSet<GPUPaintableObject> paintablesInBrush = new();

    Material localPaintMaterial;
    float nextUpdateTime;
    bool isPainting;
    WaterSprayTool waterTool;

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
            PaintBrushAt(hit.point, cleanSpeed * Time.deltaTime);
            return;
        }

        DirtNest dirtNest = hit.collider.GetComponentInParent<DirtNest>();
        if (dirtNest != null && waterTool != null)
            dirtNest.ApplyDamageOverTime(waterTool.DamagePerSecond);
    }

    void PaintBrushAt(Vector3 brushCenter, float cleanStrength)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            brushCenter,
            brushWorldSize,
            overlapColliders,
            paintMask,
            QueryTriggerInteraction.Ignore);

        paintablesInBrush.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            GPUPaintableObject paintable = overlapColliders[i].GetComponentInParent<GPUPaintableObject>();
            if (paintable == null || !paintablesInBrush.Add(paintable))
                continue;

            ApplyBrushStroke(paintable, brushCenter, cleanStrength);
        }

        if (paintablesInBrush.Count == 0 || Time.time <= nextUpdateTime)
            return;

        nextUpdateTime = Time.time + 0.1f;

        foreach (GPUPaintableObject paintable in paintablesInBrush)
            UpdatePaintableTracking(paintable, brushCenter);
    }

    public void Bind(WaterSprayTool tool)
    {
        waterTool = tool;
    }

    public void StartPainting()
    {
        isPainting = true;
    }

    public void StopPainting()
    {
        isPainting = false;
    }

    void ApplyBrushStroke(GPUPaintableObject paintable, Vector3 brushCenter, float cleanStrength)
    {
        if (paintable.isClean)
            return;

        if (!paintable.IsInitialized)
            paintable.Initialize(textureResolution);

        Renderer targetRenderer = paintable.GetComponent<Renderer>();
        if (targetRenderer == null)
            return;

        localPaintMaterial.SetVector("_BrushWorldPos", brushCenter);
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
    }

    void UpdatePaintableTracking(GPUPaintableObject paintable, Vector3 brushCenter)
    {
        if (paintable.isClean)
        {
            Managers.UI.SetCleanProgressBarPercent(100f);
            return;
        }

        paintable.UpdateTracking(brushCenter);

        if (!paintable.isClean)
            return;

        Vector3 dirToPlayer = (transform.position - brushCenter).normalized;
        dirToPlayer.y = 1f;
        Vector3 coinSpawnPos = paintable.CoinSpawnPos == null ? brushCenter : paintable.CoinSpawnPos.position;
        coinSpawnPos += dirToPlayer * 0.25f;
        Managers.Spawning.SpawnCoins(paintable.winCoins, coinSpawnPos, dirToPlayer).Forget();
    }
}
