using UnityEngine;

public class PerObjectTiling : MonoBehaviour
{
    public Vector2 patternTiling = Vector2.one;

    Renderer r;
    MaterialPropertyBlock block;

    void OnEnable()
    {
        r = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        Apply();
    }

    void OnValidate()
    {
        if (r == null) r = GetComponent<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();
        Apply();
    }

    void Apply()
    {
        r.GetPropertyBlock(block);
        block.SetVector("_PatternTiling", patternTiling);
        r.SetPropertyBlock(block);
    }
}