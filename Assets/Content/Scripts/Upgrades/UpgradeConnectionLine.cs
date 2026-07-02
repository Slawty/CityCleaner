using UnityEngine;
using UnityEngine.UI;

public class UpgradeConnectionLine : MonoBehaviour
{
    [SerializeField] Image lineImage;

    RectTransform rectTransform;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        if (lineImage == null)
            lineImage = GetComponent<Image>();
    }

    public void Bind(RectTransform from, RectTransform to)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        Vector3 fromWorld = from.position;
        Vector3 toWorld = to.position;
        Vector3 delta = toWorld - fromWorld;
        float length = delta.magnitude;

        rectTransform.position = (fromWorld + toWorld) * 0.5f;
        rectTransform.sizeDelta = new Vector2(length, rectTransform.sizeDelta.y);
        rectTransform.rotation = length > 0.001f
            ? Quaternion.FromToRotation(Vector3.right, delta.normalized)
            : Quaternion.identity;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
