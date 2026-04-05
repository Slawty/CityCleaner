using System;
using UnityEngine;
using System.Collections.Generic;

public class GrassPatch : MonoBehaviour
{
    public List<Renderer> renderers;
    int DirtStrengthID = Shader.PropertyToID("_CleanStrength");
    MaterialPropertyBlock mpb;
    GPUPaintableObject paintable;

    void Awake()
    {
        paintable = GetComponent<GPUPaintableObject>();
    }

    void OnEnable()
    {
        paintable.OnInitialize += OnInitialize;
        paintable.OnProgress += OnProgress;
        paintable.OnCleaned += OnCleaned;
    }

    void OnDisable()
    {
        paintable.OnInitialize -= OnInitialize;
        paintable.OnProgress -= OnProgress;
        paintable.OnCleaned -= OnCleaned;
    }


    void OnInitialize()
    {
        mpb = new MaterialPropertyBlock();
        SetCleanStrength(1f);
    }

    void OnProgress()
    {
        SetCleanStrength(paintable.GetCleanPercent());
    }

    void OnCleaned()
    {
        SetCleanStrength(0f);
    }

    void SetCleanStrength(float value)
    {
        mpb.SetFloat(DirtStrengthID, value);

        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].SetPropertyBlock(mpb);
        }
    }
}
