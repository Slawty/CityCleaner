using System.Collections.Generic;
using UnityEngine;

public class DirtMaterialPreview : MonoBehaviour
{
    [SerializeField] List<Material> materials = new();
    [SerializeField] bool previewDirty = true;

    public IReadOnlyList<Material> Materials => materials;
    public bool PreviewDirty
    {
        get => previewDirty;
        set => previewDirty = value;
    }
}
