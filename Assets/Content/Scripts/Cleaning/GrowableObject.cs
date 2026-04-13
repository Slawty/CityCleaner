using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class GrowableObject : MonoBehaviour
{
    [Header("Growth")]
    public float minScale = 0.5f;
    public float maxScale = 1f;
    public float totalGrowTime = 30f;
    public int bumps = 5;
    public float lifeTimePerGoo = 1f;
    public int maxGooCount = 10;
    public Transform growObject;
    public List<Renderer> cleanRenderers;

    [Header("Bump Effect")]
    public float bumpScale = 1.15f;
    public float bumpDuration = 0.15f;

    float growthProgress;
    float bumpMultiplier = 1f;
    int nextPhaseIndex = 0;
    bool isGrowing = false;
    Tween bumpTween;

    int gooCount;
    float gooTimeCounter;

    void Update()
    {
        if (!isGrowing)
            return;

        // Goo decay
        if (gooCount > 0)
        {
            gooTimeCounter += Time.deltaTime;

            if (gooTimeCounter >= lifeTimePerGoo)
            {
                gooCount = Mathf.Max(0, gooCount - 1);
                gooTimeCounter = 0f;
            }
        }

        // Growth speed based on goo amount
        float gooFactor = (float)gooCount / maxGooCount;
        float growthSpeed = Time.deltaTime / totalGrowTime * gooFactor;

        growthProgress += growthSpeed;
        growthProgress = Mathf.Clamp01(growthProgress);

        // Phase bumps
        float phaseFraction = (float)nextPhaseIndex / bumps;

        if (growthProgress >= phaseFraction && nextPhaseIndex <= bumps)
        {
            TriggerBump();
            nextPhaseIndex++;
        }

        ApplyScale();

        // Stop when fully grown
        if (growthProgress >= 1f)
        {
            growthProgress = 1f;
            CleanShaders();
            isGrowing = false;
        }
    }

    public void HitByGoo()
    {
        if (growthProgress >= 1f || gooCount >= maxGooCount)
            return;

        if (!isGrowing)
            gooTimeCounter = 0f;

        isGrowing = true;
        gooCount = Mathf.Clamp(gooCount + 1, 0, maxGooCount);
        Debug.Log($"Goo hit {gameObject.name} ({gooCount}/{maxGooCount})");
    }

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

    void ApplyScale()
    {
        float scale = Mathf.Lerp(minScale, maxScale, growthProgress);
        growObject.localScale = Vector3.one * scale * bumpMultiplier;
    }

    void TriggerBump()
    {
        bumpTween?.Kill();

        bumpTween = DOTween.Sequence()
            .Append(DOTween.To(() => bumpMultiplier, x => bumpMultiplier = x, bumpScale, bumpDuration))
            .Append(DOTween.To(() => bumpMultiplier, x => bumpMultiplier = x, 1f, bumpDuration));
    }
}