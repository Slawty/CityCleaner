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
        mpb = new MaterialPropertyBlock();
        InitializeGrowable();
    }

    protected override void ApplyGrowth(float progress, float hitMultiplier)
    {
        float baseStrength = Mathf.Lerp(minStrength, maxStrength, progress);
        float bumpedStrength = Mathf.Clamp01(baseStrength * hitMultiplier);
        mpb.SetFloat(growStrengthID, bumpedStrength);

        for (int i = 0; i < growableRenderers.Count; i++)
        {
            if (growableRenderers[i] == null)
                continue;

            growableRenderers[i].SetPropertyBlock(mpb);
        }


        mpb.SetFloat(dirtStrengthID, 1 - progress);
        foreach (var renderer in cleanRenderers)
        {
            renderer.SetPropertyBlock(mpb);
            // renderer.material.SetFloat(dirtStrengthID, 0f);
        }
    }

    protected override void OnFullyGrown()
    {
        Debug.Log("OnFullyGrown");
        foreach (var renderer in cleanRenderers)
        {
            mpb.SetFloat(dirtStrengthID, 0);
            renderer.SetPropertyBlock(mpb);
            // renderer.material.SetFloat(dirtStrengthID, 0f);
        }
    }
}
