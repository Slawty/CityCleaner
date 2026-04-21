using UnityEngine;
using System.Collections.Generic;

public class GrassPatch : GooHitGrowable
{
    [Header("Renderers")]
    [SerializeField] List<Renderer> renderers;

    [Header("Growth")]
    [SerializeField] float minStrength = 0f;
    [SerializeField] float maxStrength = 1f;

    readonly int dirtStrengthID = Shader.PropertyToID("_CleanStrength");
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
        mpb.SetFloat(dirtStrengthID, bumpedStrength);

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null)
                continue;

            renderers[i].SetPropertyBlock(mpb);
        }
    }
}
