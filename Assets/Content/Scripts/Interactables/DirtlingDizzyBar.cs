using UnityEngine;
using UnityEngine.UI;

public class DirtlingDizzyBar : MonoBehaviour
{
    [SerializeField] HealthBar healthBar;
    [SerializeField] GameObject barRoot;
    [SerializeField] Vector3 localOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] float worldScale = 0.01f;

    void Awake()
    {
        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBar>(true);

        if (healthBar == null)
            CreateRuntimeBar();

        if (barRoot == null && healthBar != null)
            barRoot = healthBar.gameObject;
    }

    void Start()
    {
        SetDizzy(0f);
    }

    public void SetDizzy(float normalized01)
    {
        float dizzy = Mathf.Clamp01(normalized01);

        if (barRoot != null)
            barRoot.SetActive(dizzy > 0.001f);

        if (healthBar != null)
            healthBar.SetNormalizedFill(dizzy);
    }

    void CreateRuntimeBar()
    {
        barRoot = new GameObject("Dizzy Bar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = localOffset;
        barRoot.transform.localScale = Vector3.one * worldScale;

        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        barRoot.AddComponent<CanvasScaler>();
        healthBar = barRoot.AddComponent<HealthBar>();

        GameObject background = new GameObject("Background");
        background.transform.SetParent(barRoot.transform, false);
        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.sizeDelta = new Vector2(120f, 16f);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0.1f, 0.2f, 0.85f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(background.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.7f, 1f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

        ProgressBar progressBar = barRoot.AddComponent<ProgressBar>();
        progressBar.SetFillImage(fillImage);
        healthBar.SetProgressBar(progressBar);
    }
}
