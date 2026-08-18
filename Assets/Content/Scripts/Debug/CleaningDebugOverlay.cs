using UnityEngine;

public class CleaningDebugOverlay : MonoBehaviour
{
    public bool Visible { get; set; }

    GUIStyle boxStyle;
    GUIStyle labelStyle;
    bool stylesReady;

    void OnGUI()
    {
        if (!Visible)
            return;

        EnsureStyles();

        const float width = 420f;
        const float height = 240f;
        Rect rect = new Rect(12f, 12f, width, height);

        GUI.Box(rect, GUIContent.none, boxStyle);

        GUILayout.BeginArea(rect);
        GUILayout.Space(6f);
        GUILayout.Label("Cleaning Debug (F9)", labelStyle);
        GUILayout.Label($"FPS: {Mathf.RoundToInt(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f))}", labelStyle);
        GUILayout.Label($"Painting: {CleaningDebugStats.IsPainting}  Ray hit: {CleaningDebugStats.RayHit}", labelStyle);
        GUILayout.Label($"CleanSpeed: {CleaningDebugStats.CleanSpeed:F1}  Strength: {CleaningDebugStats.LastCleanStrength:F4}", labelStyle);
        GUILayout.Label($"Hit paintable: {CleaningDebugStats.HitPaintableName}", labelStyle);
        GUILayout.Label($"Zone dirt painted: {CleaningDebugStats.ZoneDirtPainted}  Maps: {CleaningDebugStats.ZoneMapsPainted}", labelStyle);
        GUILayout.Label($"Paintables in brush: {CleaningDebugStats.PaintablesInBrush}", labelStyle);
        GUILayout.Label($"Primary: {CleaningDebugStats.PrimaryPaintableName}  Clean: {CleaningDebugStats.PrimaryCleanPercent:P0}", labelStyle);
        GUILayout.Label(
            $"Renderer props — mask: {CleaningDebugStats.PrimaryHasMaskOnRenderer}  zone: {CleaningDebugStats.PrimaryHasZoneOnRenderer}",
            labelStyle);
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        if (stylesReady)
            return;

        stylesReady = true;
        boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };
    }
}
