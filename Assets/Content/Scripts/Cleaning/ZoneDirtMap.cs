using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ZoneDirtMap : MonoBehaviour
{
    static readonly int ZoneDirtTexId = Shader.PropertyToID("_ZoneDirtTex");
    static readonly int ZoneDirtTexXZId = Shader.PropertyToID("_ZoneDirtTexXZ");
    static readonly int ZoneDirtTexXYId = Shader.PropertyToID("_ZoneDirtTexXY");
    static readonly int ZoneDirtTexYZId = Shader.PropertyToID("_ZoneDirtTexYZ");
    static readonly int ZoneMinXZId = Shader.PropertyToID("_ZoneMinXZ");
    static readonly int ZoneMaxXZId = Shader.PropertyToID("_ZoneMaxXZ");
    static readonly int ZoneMinXYId = Shader.PropertyToID("_ZoneMinXY");
    static readonly int ZoneMaxXYId = Shader.PropertyToID("_ZoneMaxXY");
    static readonly int ZoneMinYZId = Shader.PropertyToID("_ZoneMinYZ");
    static readonly int ZoneMaxYZId = Shader.PropertyToID("_ZoneMaxYZ");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int BrushTexId = Shader.PropertyToID("_BrushTex");
    static readonly int BrushWorldPosId = Shader.PropertyToID("_BrushWorldPos");
    static readonly int BrushSizeId = Shader.PropertyToID("_BrushSize");
    static readonly int StrengthId = Shader.PropertyToID("_Strength");
    static readonly int ProjectionModeId = Shader.PropertyToID("_ProjectionMode");

    [Header("Zone Bounds")]
    [SerializeField] BoxCollider zoneBoundsCollider;

    [Header("Texture")]
    [SerializeField] int textureResolution = 1024;
    [SerializeField, Range(0f, 1f)] float initialDirt = 1f;
    [SerializeField] Texture2D initialZoneDirtTexture;

    [Header("Targets")]
    [SerializeField] bool autoCollectTargetRenderers = true;
    [SerializeField] List<Renderer> targetRenderers = new();

    [Header("Brush")]
    [SerializeField] Shader zoneBrushShader;

    public RenderTexture ZoneTexture { get; private set; }
    public Vector2 ZoneMinXZ => zoneMinXZ;
    public Vector2 ZoneMaxXZ => zoneMaxXZ;

    Material brushMaterial;
    MaterialPropertyBlock propertyBlock;
    RenderTexture zoneTextureXZ;
    RenderTexture zoneTextureXY;
    RenderTexture zoneTextureYZ;
    Vector2 zoneMinXZ;
    Vector2 zoneMaxXZ;
    Vector2 zoneMinXY;
    Vector2 zoneMaxXY;
    Vector2 zoneMinYZ;
    Vector2 zoneMaxYZ;

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (zoneBoundsCollider == null)
            zoneBoundsCollider = GetComponent<BoxCollider>();

        if (zoneBoundsCollider == null)
        {
            Debug.LogError($"ZoneDirtMap on {name} requires a BoxCollider defining zone bounds.");
            enabled = false;
            return;
        }

        if (zoneBrushShader == null)
            zoneBrushShader = Shader.Find("Hidden/ZoneDirtBrush");

        if (zoneBrushShader == null)
        {
            Debug.LogError("Missing shader Hidden/ZoneDirtBrush. Zone dirt painting is disabled.");
            enabled = false;
            return;
        }

        brushMaterial = new Material(zoneBrushShader);

        RebuildZoneTexture();
        UpdateZoneBounds();
        ApplyToTargetRenderers();
    }

    void OnDestroy()
    {
        ReleaseTexture(zoneTextureXZ);
        ReleaseTexture(zoneTextureXY);
        ReleaseTexture(zoneTextureYZ);

        if (brushMaterial != null)
            Destroy(brushMaterial);
    }

    public void RebuildZoneTexture()
    {
        ReleaseTexture(zoneTextureXZ);
        ReleaseTexture(zoneTextureXY);
        ReleaseTexture(zoneTextureYZ);

        zoneTextureXZ = CreateZoneTexture("XZ");
        zoneTextureXY = CreateZoneTexture("XY");
        zoneTextureYZ = CreateZoneTexture("YZ");

        // Legacy compatibility: old shaders/scripts may still read _ZoneDirtTex.
        ZoneTexture = zoneTextureXZ;
    }

    public void UpdateZoneBounds()
    {
        Bounds bounds = zoneBoundsCollider.bounds;
        zoneMinXZ = new Vector2(bounds.min.x, bounds.min.z);
        zoneMaxXZ = new Vector2(bounds.max.x, bounds.max.z);
        zoneMinXY = new Vector2(bounds.min.x, bounds.min.y);
        zoneMaxXY = new Vector2(bounds.max.x, bounds.max.y);
        zoneMinYZ = new Vector2(bounds.min.y, bounds.min.z);
        zoneMaxYZ = new Vector2(bounds.max.y, bounds.max.z);
    }

    public void CollectTargetRenderers()
    {
        targetRenderers.Clear();
        targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
    }

    public void ApplyToTargetRenderers()
    {
        if (autoCollectTargetRenderers)
            CollectTargetRenderers();

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(ZoneDirtTexId, zoneTextureXZ);
            propertyBlock.SetTexture(ZoneDirtTexXZId, zoneTextureXZ);
            propertyBlock.SetTexture(ZoneDirtTexXYId, zoneTextureXY);
            propertyBlock.SetTexture(ZoneDirtTexYZId, zoneTextureYZ);
            propertyBlock.SetVector(ZoneMinXZId, zoneMinXZ);
            propertyBlock.SetVector(ZoneMaxXZId, zoneMaxXZ);
            propertyBlock.SetVector(ZoneMinXYId, zoneMinXY);
            propertyBlock.SetVector(ZoneMaxXYId, zoneMaxXY);
            propertyBlock.SetVector(ZoneMinYZId, zoneMinYZ);
            propertyBlock.SetVector(ZoneMaxYZId, zoneMaxYZ);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void PaintAtWorldPos(Vector3 brushWorldPos, Vector3 hitNormal, float brushWorldSize, float cleanStrength, Texture2D brushTexture)
    {
        brushMaterial.SetTexture(BrushTexId, brushTexture != null ? brushTexture : Texture2D.whiteTexture);
        brushMaterial.SetVector(ZoneMinXZId, zoneMinXZ);
        brushMaterial.SetVector(ZoneMaxXZId, zoneMaxXZ);
        brushMaterial.SetVector(ZoneMinXYId, zoneMinXY);
        brushMaterial.SetVector(ZoneMaxXYId, zoneMaxXY);
        brushMaterial.SetVector(ZoneMinYZId, zoneMinYZ);
        brushMaterial.SetVector(ZoneMaxYZId, zoneMaxYZ);
        brushMaterial.SetVector(BrushWorldPosId, brushWorldPos);
        brushMaterial.SetFloat(BrushSizeId, brushWorldSize);
        brushMaterial.SetFloat(StrengthId, cleanStrength);

        Vector3 absNormal = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
        if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z)
        {
            PaintIntoTexture(zoneTextureXZ, 0);
            return;
        }

        if (absNormal.z >= absNormal.x)
        {
            PaintIntoTexture(zoneTextureXY, 1);
            return;
        }

        PaintIntoTexture(zoneTextureYZ, 2);
    }

    RenderTexture CreateZoneTexture(string suffix)
    {
        RenderTexture zoneTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.R8)
        {
            name = $"{name}_ZoneDirtRT_{suffix}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        zoneTexture.Create();

        RenderTexture activeTarget = RenderTexture.active;
        if (initialZoneDirtTexture != null)
        {
            Graphics.Blit(initialZoneDirtTexture, zoneTexture);
        }
        else
        {
            RenderTexture.active = zoneTexture;
            GL.Clear(true, true, new Color(initialDirt, initialDirt, initialDirt, 1f));
        }
        RenderTexture.active = activeTarget;
        return zoneTexture;
    }

    void PaintIntoTexture(RenderTexture zoneTexture, int projectionMode)
    {
        brushMaterial.SetTexture(MainTexId, zoneTexture);
        brushMaterial.SetFloat(ProjectionModeId, projectionMode);

        RenderTexture temp = RenderTexture.GetTemporary(zoneTexture.descriptor);
        Graphics.Blit(zoneTexture, temp);
        Graphics.Blit(temp, zoneTexture, brushMaterial);
        RenderTexture.ReleaseTemporary(temp);
    }

    static void ReleaseTexture(RenderTexture zoneTexture)
    {
        if (zoneTexture != null)
            zoneTexture.Release();
    }
}
