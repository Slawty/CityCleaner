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
    [SerializeField] Shader paintBrushShader;
    [SerializeField] Texture2D brushTexture;
    [SerializeField] float brushWorldSize = 0.5f;
    [SerializeField] float cleanSpeed = 4f;
    [Header("Dirt")]
    [SerializeField] bool spawnDirtChunksWhileCleaning;
    [SerializeField] float dirtChunksPerSecond = 3f;

    public float BrushWorldSize
    {
        get => brushWorldSize;
        set => brushWorldSize = value;
    }

    public float CleanSpeed
    {
        get => cleanSpeed;
        set => cleanSpeed = value;
    }

    public LayerMask PaintMask => paintMask;
    public bool IsPainting => isPainting;

    readonly Collider[] overlapColliders = new Collider[OverlapBufferSize];
    readonly HashSet<GPUPaintableObject> paintablesInBrush = new();

    Material localPaintMaterial;
    float nextUpdateTime;
    float dirtSpawnAccumulator;
    bool isPainting;
    WaterSprayTool waterTool;

    void Awake()
    {
        if (paintBrushShader == null)
            throw new System.InvalidOperationException($"{nameof(GPUPainterWorld)} on {name}: {nameof(paintBrushShader)} is not assigned.");

        localPaintMaterial = new Material(paintBrushShader);
    }

    void Update()
    {
        if (!isPainting)
        {
            CleaningDebugStats.ClearStroke();
            return;
        }

        CleaningDebugStats.IsPainting = true;
        CleaningDebugStats.CleanSpeed = cleanSpeed;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, paintMask, QueryTriggerInteraction.Ignore))
        {
            CleaningDebugStats.RayHit = false;
            return;
        }

        CleaningDebugStats.RayHit = true;

        GPUPaintableObject paintable = hit.collider.GetComponentInParent<GPUPaintableObject>();
        CleaningDebugStats.HitPaintableName = paintable != null ? paintable.name : "none";

        float cleanStrength = cleanSpeed * Time.deltaTime;
        CleaningDebugStats.LastCleanStrength = cleanStrength;
        PaintBrushAt(hit.point, hit.normal, cleanStrength, paintZoneDirt: paintable == null);

        if (paintable != null)
            return;

        DirtNest dirtNest = hit.collider.GetComponentInParent<DirtNest>();
        if (dirtNest != null && waterTool != null)
            dirtNest.ApplyDamageOverTime(waterTool.DamagePerSecond);
    }

    void PaintBrushAt(Vector3 brushCenter, Vector3 hitNormal, float cleanStrength, bool paintZoneDirt)
    {
        int zoneMapsPainted = paintZoneDirt ? PaintZoneDirtMaps(brushCenter, hitNormal, cleanStrength) : 0;
        CleaningDebugStats.ZoneDirtPainted = zoneMapsPainted > 0;
        CleaningDebugStats.ZoneMapsPainted = zoneMapsPainted;

        int hitCount = Physics.OverlapSphereNonAlloc(
            brushCenter,
            brushWorldSize,
            overlapColliders,
            paintMask,
            QueryTriggerInteraction.Ignore);

        paintablesInBrush.Clear();

        float dirtMultiplierSum = 0f;
        int dirtyPaintableCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            GPUPaintableObject paintable = overlapColliders[i].GetComponentInParent<GPUPaintableObject>();
            if (paintable == null || !paintablesInBrush.Add(paintable))
                continue;

            if (!paintable.isClean)
            {
                dirtyPaintableCount++;
                dirtMultiplierSum += paintable.DirtChunkMultiplier;
            }

            ApplyBrushStroke(paintable, brushCenter, hitNormal, cleanStrength);
        }

        if (dirtyPaintableCount > 0)
            SpawnCleaningDirt(brushCenter, dirtMultiplierSum / dirtyPaintableCount);

        CleaningDebugStats.PaintablesInBrush = paintablesInBrush.Count;
        UpdatePrimaryPaintableDebugStats();

        if (paintablesInBrush.Count == 0 || Time.time <= nextUpdateTime)
            return;

        nextUpdateTime = Time.time + 0.1f;

        foreach (GPUPaintableObject paintable in paintablesInBrush)
            UpdatePaintableTracking(paintable, brushCenter);

        UpdatePrimaryPaintableDebugStats();
    }

    void UpdatePrimaryPaintableDebugStats()
    {
        foreach (GPUPaintableObject paintable in paintablesInBrush)
        {
            CleaningDebugStats.PrimaryPaintableName = paintable.name;
            CleaningDebugStats.PrimaryCleanPercent = paintable.GetCleanPercent();
            CleaningDebugStats.PrimaryHasMaskOnRenderer = paintable.RendererHasMaskBound();
            CleaningDebugStats.PrimaryHasZoneOnRenderer = paintable.RendererHasZoneDirtBound();
            return;
        }

        CleaningDebugStats.PrimaryPaintableName = "none";
        CleaningDebugStats.PrimaryCleanPercent = 0f;
        CleaningDebugStats.PrimaryHasMaskOnRenderer = false;
        CleaningDebugStats.PrimaryHasZoneOnRenderer = false;
    }

    int PaintZoneDirtMaps(Vector3 brushCenter, Vector3 hitNormal, float cleanStrength)
    {
        int zoneMapsPainted = 0;

        foreach (ZoneDirtMap zoneDirtMap in ZoneDirtMap.ActiveMaps)
        {
            if (zoneDirtMap == null || !zoneDirtMap.ContainsWorldPoint(brushCenter))
                continue;

            zoneDirtMap.PaintAtWorldPos(brushCenter, hitNormal, brushWorldSize, cleanStrength, brushTexture);
            zoneMapsPainted++;
        }

        return zoneMapsPainted;
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

    void ApplyBrushStroke(GPUPaintableObject paintable, Vector3 brushCenter, Vector3 hitNormal, float cleanStrength)
    {
        if (paintable.isClean)
            return;

        if (!paintable.IsInitialized)
            paintable.Initialize(textureResolution);

        Renderer targetRenderer = paintable.GetComponent<Renderer>();
        if (targetRenderer == null)
            return;

        localPaintMaterial.SetVector("_BrushWorldPos", brushCenter);
        localPaintMaterial.SetVector("_BrushWorldNormal", hitNormal);
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

        Vector3 coinSpawnPos = paintable.CoinSpawnPos == null ? brushCenter : paintable.CoinSpawnPos.position;
        Managers.Spawning.SpawnCoins(paintable.winCoins, coinSpawnPos).Forget();
    }

    void SpawnCleaningDirt(Vector3 brushCenter, float dirtMultiplier)
    {
        if (!spawnDirtChunksWhileCleaning || dirtChunksPerSecond <= 0f || dirtMultiplier <= 0f)
            return;

        dirtSpawnAccumulator += dirtChunksPerSecond * dirtMultiplier * Time.deltaTime;
        int chunkCount = Mathf.FloorToInt(dirtSpawnAccumulator);
        if (chunkCount <= 0)
            return;

        dirtSpawnAccumulator -= chunkCount;

        Vector3 dirFromPlayer = brushCenter - transform.position;
        dirFromPlayer.y = 0f;
        if (dirFromPlayer.sqrMagnitude < 0.01f)
            dirFromPlayer = Vector3.forward;
        else
            dirFromPlayer.Normalize();

        for (int i = 0; i < chunkCount; i++)
        {
            Vector3 spawnPos = brushCenter + Random.insideUnitSphere * 0.15f;
            Vector3 spawnDirection = Quaternion.AngleAxis(Random.Range(-45f, 45f), Vector3.up) * dirFromPlayer;
            Managers.Spawning.SpawnPickupChunk(spawnPos, spawnDirection);
        }
    }
}
