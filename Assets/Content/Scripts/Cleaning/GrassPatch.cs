using UnityEngine;
using System.Collections.Generic;

public class GrassPatch : GooHitGrowable
{
    [Header("Renderers")]
    [SerializeField] List<Renderer> growableRenderers;
    [SerializeField] List<Renderer> cleanRenderers;

    [Header("Growth")]
    [SerializeField] float minStrength = 0f;
    [SerializeField] float maxStrength = 1f;

    [Header("Dirt shader")]
    [SerializeField] float cleanStrengthWhileGrowing = 0f;
    [SerializeField] float cleanStrengthWhenFullyGrown = 1f;

    readonly int growStrengthID = Shader.PropertyToID("_GrowStrength");
    readonly int dirtStrengthID = Shader.PropertyToID("_DirtAmount");
    MaterialPropertyBlock mpb;

    void Awake()
    {
        EnsureMpb();
        InitializeGrowable();
    }

    void EnsureMpb()
    {
        mpb ??= new MaterialPropertyBlock();
    }

    protected override void ApplyGrowth(float progress, float hitMultiplier)
    {
        EnsureMpb();

        float baseStrength = Mathf.Lerp(minStrength, maxStrength, progress);
        float bumpedStrength = Mathf.Clamp01(baseStrength * hitMultiplier);
        mpb.SetFloat(growStrengthID, bumpedStrength);

        if (growableRenderers != null)
        {
            for (int i = 0; i < growableRenderers.Count; i++)
            {
                if (growableRenderers[i] == null)
                    continue;

                growableRenderers[i].SetPropertyBlock(mpb);
            }
        }

        mpb.SetFloat(dirtStrengthID, 1 - progress);
        if (cleanRenderers == null)
            return;

        foreach (Renderer renderer in cleanRenderers)
        {
            if (renderer == null)
                continue;

            renderer.SetPropertyBlock(mpb);
        }
    }

    protected override void OnFullyGrown()
    {
        EnsureMpb();

        if (cleanRenderers == null)
            return;

        foreach (Renderer renderer in cleanRenderers)
        {
            if (renderer == null)
                continue;

            mpb.SetFloat(dirtStrengthID, 0);
            renderer.SetPropertyBlock(mpb);
        }
    }
}
