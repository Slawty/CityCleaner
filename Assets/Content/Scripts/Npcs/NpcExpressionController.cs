using System;
using UnityEngine;

[Serializable]
public class NpcEffectEntry
{
    public string name;
    public ParticleSystem particleSystem;
}

public class NpcExpressionController : MonoBehaviour
{
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    [SerializeField] NpcExpressionAtlas atlas;
    [SerializeField] Renderer faceRenderer;
    [SerializeField] int materialIndex;
    [SerializeField] string texturePropertyName = "_BaseMap";
    [SerializeField] NpcEffectEntry[] effects = Array.Empty<NpcEffectEntry>();

    MaterialPropertyBlock propertyBlock;
    int texturePropertyId;
    string currentExpressionName;

    void Awake()
    {
        ResolveFaceRenderer();
        texturePropertyId = Shader.PropertyToID(texturePropertyName);

        if (atlas == null)
            throw new InvalidOperationException($"{nameof(NpcExpressionController)} on {name}: {nameof(atlas)} is not assigned.");

        if (faceRenderer == null)
            throw new InvalidOperationException($"{nameof(NpcExpressionController)} on {name}: {nameof(faceRenderer)} is not assigned.");

        propertyBlock = new MaterialPropertyBlock();
        ResetPresentation();
    }

    public void SetExpression(string expressionName)
    {
        if (string.IsNullOrEmpty(expressionName) || expressionName == currentExpressionName)
            return;

        if (!atlas.TryGetTexture(expressionName, out Texture2D texture))
            return;

        faceRenderer.GetPropertyBlock(propertyBlock, materialIndex);
        propertyBlock.SetTexture(texturePropertyId != 0 ? texturePropertyId : BaseMapId, texture);
        faceRenderer.SetPropertyBlock(propertyBlock, materialIndex);
        currentExpressionName = expressionName;
    }

    public void SetEffect(string effectName, bool active)
    {
        if (string.IsNullOrEmpty(effectName))
            return;

        ParticleSystem particleSystem = FindEffect(effectName);
        if (particleSystem == null)
            return;

        if (active)
        {
            particleSystem.gameObject.SetActive(true);
            particleSystem.Play(true);
            return;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void EnableEffect(string effectName) => SetEffect(effectName, true);

    public void DisableEffect(string effectName) => SetEffect(effectName, false);

    public void ResetPresentation()
    {
        SetExpression(atlas.DefaultExpressionName);

        foreach (NpcEffectEntry effect in effects)
        {
            if (effect == null || effect.particleSystem == null)
                continue;

            effect.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.particleSystem.gameObject.SetActive(false);
        }
    }

    ParticleSystem FindEffect(string effectName)
    {
        foreach (NpcEffectEntry effect in effects)
        {
            if (effect == null || effect.name != effectName)
                continue;

            if (effect.particleSystem == null)
            {
                Debug.LogError($"{nameof(NpcExpressionController)} on {name}: effect '{effectName}' has no particle system assigned.", this);
                return null;
            }

            return effect.particleSystem;
        }

        Debug.LogWarning($"{nameof(NpcExpressionController)} on {name}: no effect named '{effectName}'.", this);
        return null;
    }

    void ResolveFaceRenderer()
    {
        if (faceRenderer != null)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.gameObject.name.Contains("Face", StringComparison.OrdinalIgnoreCase))
            {
                faceRenderer = renderer;
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                if (material.name.Contains("Face", StringComparison.OrdinalIgnoreCase))
                {
                    faceRenderer = renderer;
                    return;
                }
            }
        }
    }
}
