using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class GPUPainter : MonoBehaviour
{
    const int OverlapBufferSize = 32;

    [Header("Setup")]
    [SerializeField] Camera cam;
    [SerializeField] int textureResolution = 1024;
    [SerializeField] LayerMask paintMask;

    [Header("Brush")]
    [SerializeField] Texture2D brushTexture;
    [SerializeField] float brushWorldSize = 0.5f;
    [SerializeField] float cleanSpeed = 4f;

    readonly Collider[] overlapColliders = new Collider[OverlapBufferSize];
    readonly HashSet<GPUPaintableObject> paintablesInBrush = new();

    public Material paintMaterial;
    float nextUpdateTime;
    bool isPainting;

    void Awake()
    {
        paintMaterial = new Material(Shader.Find("Hidden/GPUPaintBrush"));
    }

    void Update()
    {
        if (!isPainting)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, paintMask, QueryTriggerInteraction.Ignore))
            return;

        GPUPaintableObject hitPaintable = hit.collider.GetComponentInParent<GPUPaintableObject>();
        PaintAtPosition(hit.point, hit.normal, cleanSpeed * Time.deltaTime, paintZoneDirt: hitPaintable == null);
    }

    public void StartPainting()
    {
        isPainting = true;
    }

    public void StopPainting()
    {
        isPainting = false;
    }

    public void Paint(GPUPaintableObject paintable, Vector3 hitPos, float cleanStrength)
    {
        PaintAtPosition(hitPos, Vector3.up, cleanStrength);
    }

    public void PaintAtPosition(Vector3 brushCenter, float cleanStrength, bool gooOnly = false)
    {
        PaintAtPosition(brushCenter, Vector3.up, cleanStrength, gooOnly, paintZoneDirt: false);
    }

    public void PaintAtPosition(Vector3 brushCenter, Vector3 hitNormal, float cleanStrength, bool gooOnly = false, bool paintZoneDirt = false)
    {
        if (paintZoneDirt)
            PaintZoneDirtMaps(brushCenter, hitNormal, cleanStrength);

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
            if (paintable == null || (gooOnly && !paintable.AllowGooCleaning) || !paintablesInBrush.Add(paintable))
                continue;

            ApplyBrushStroke(paintable, brushCenter, hitNormal, cleanStrength);
        }

        if (paintablesInBrush.Count == 0 || Time.time <= nextUpdateTime)
            return;

        nextUpdateTime = Time.time + 0.1f;

        foreach (GPUPaintableObject paintable in paintablesInBrush)
            UpdatePaintableTracking(paintable, brushCenter);
    }

    void PaintZoneDirtMaps(Vector3 brushCenter, Vector3 hitNormal, float cleanStrength)
    {
        foreach (ZoneDirtMap zoneDirtMap in ZoneDirtMap.ActiveMaps)
        {
            if (zoneDirtMap == null || !zoneDirtMap.ContainsWorldPoint(brushCenter))
                continue;

            zoneDirtMap.PaintAtWorldPos(brushCenter, hitNormal, brushWorldSize, cleanStrength, brushTexture);
        }
    }

    void ApplyBrushStroke(GPUPaintableObject paintable, Vector3 brushCenter, Vector3 hitNormal, float cleanStrength)
    {
        if (paintable.isClean)
            return;

        if (!paintable.IsInitialized)
            paintable.Initialize(textureResolution);

        Renderer renderer = paintable.GetComponent<Renderer>();
        MeshFilter meshFilter = paintable.GetComponent<MeshFilter>();
        if (renderer == null || meshFilter == null)
            return;

        paintMaterial.SetVector("_BrushWorldPos", brushCenter);
        paintMaterial.SetVector("_BrushWorldNormal", hitNormal);
        paintMaterial.SetFloat("_BrushSize", brushWorldSize);
        paintMaterial.SetFloat("_Strength", cleanStrength);
        paintMaterial.SetTexture("_BrushTex", brushTexture);

        RenderTexture temp = RenderTexture.GetTemporary(paintable.maskTexture.width, paintable.maskTexture.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(paintable.maskTexture, temp);
        paintMaterial.SetTexture("_MainTex", temp);

        CommandBuffer commandBuffer = new CommandBuffer();
        commandBuffer.name = "GPUPaint";
        commandBuffer.SetRenderTarget(paintable.maskTexture);
        commandBuffer.DrawRenderer(renderer, paintMaterial);

        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Release();
        RenderTexture.ReleaseTemporary(temp);
    }

    void UpdatePaintableTracking(GPUPaintableObject paintable, Vector3 brushCenter)
    {
        paintable.UpdateTracking(brushCenter);

        if (!paintable.isClean)
            return;

        Vector3 coinSpawnPos = paintable.CoinSpawnPos == null ? brushCenter : paintable.CoinSpawnPos.position;
        Managers.Spawning.SpawnCoins(paintable.winCoins, coinSpawnPos).Forget();
    }

}
