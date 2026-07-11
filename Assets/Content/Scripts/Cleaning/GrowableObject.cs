using UnityEngine;
using System.Collections.Generic;

public class GrowableObject : GooHitGrowable
{
    [Header("Growth")]
    public float minScale = 0.5f;
    public float maxScale = 1f;
    public Transform growObject;
    public List<Renderer> cleanRenderers;

    void Awake() => InitializeGrowable();

    void CleanShaders()
    {
        foreach (Renderer r in cleanRenderers)
        {
            if (r == null)
                continue;

            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_DirtAmount"))
                {
                    mat.SetFloat("_DirtAmount", 0f);
                }
            }
        }
    }

    protected override void OnFullyGrown()
    {
        CleanShaders();
    }

    protected override void OnDebugResetGrowth()
    {
        DirtyShaders();
    }

    void DirtyShaders()
    {
        foreach (Renderer renderer in cleanRenderers)
        {
            if (renderer == null)
                continue;

            foreach (Material material in renderer.materials)
            {
                if (material.HasProperty("_DirtAmount"))
                    material.SetFloat("_DirtAmount", 1f);
            }
        }
    }

    protected override void ApplyGrowth(float progress, float hitMultiplier)
    {
        if (growObject == null)
            return;

        float scale = Mathf.Lerp(minScale, maxScale, progress);
        growObject.localScale = Vector3.one * scale * hitMultiplier;
    }
}