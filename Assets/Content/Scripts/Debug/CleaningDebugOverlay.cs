using UnityEngine;

public class CleaningDebugOverlay : MonoBehaviour
{
    public bool Visible { get; set; }

    const float PanelWidth = 480f;
    const float PanelX = 12f;
    const float PanelY = 12f;
    const float HorizontalPadding = 12f;
    const float VerticalPadding = 10f;
    const float ExtraBottomPadding = 48f;

    GUIStyle boxStyle;
    GUIStyle labelStyle;
    bool stylesReady;

    void OnGUI()
    {
        if (!Visible)
            return;

        EnsureStyles();

        float contentWidth = PanelWidth - HorizontalPadding * 2f;
        float contentHeight = MeasureContentHeight(contentWidth);
        float panelHeight = contentHeight + VerticalPadding * 2f + ExtraBottomPadding;
        Rect rect = new Rect(PanelX, PanelY, PanelWidth, panelHeight);

        GUI.Box(rect, GUIContent.none, boxStyle);

        GUILayout.BeginArea(rect);
        GUILayout.Space(VerticalPadding);
        DrawContent(contentWidth);
        GUILayout.Space(VerticalPadding + ExtraBottomPadding);
        GUILayout.EndArea();
    }

    float MeasureContentHeight(float contentWidth)
    {
        float height = 0f;
        AddLineHeight(ref height, contentWidth, "Cleaning Debug (F9)");
        AddLineHeight(ref height, contentWidth, $"FPS: {Mathf.RoundToInt(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f))}");
        AddLineHeight(ref height, contentWidth, $"Painting: {CleaningDebugStats.IsPainting}  Ray hit: {CleaningDebugStats.RayHit}");
        AddLineHeight(ref height, contentWidth, $"CleanSpeed: {CleaningDebugStats.CleanSpeed:F1}  Strength: {CleaningDebugStats.LastCleanStrength:F4}");
        AddLineHeight(ref height, contentWidth, $"Zone dirt painted: {CleaningDebugStats.ZoneDirtPainted}  Maps: {CleaningDebugStats.ZoneMapsPainted}");
        AddLineHeight(ref height, contentWidth, $"Paintables in brush: {CleaningDebugStats.PaintablesInBrush}");

        if (CleaningDebugStats.HasRayTargetPaintable)
        {
            PaintableDebugEntry rayTarget = CleaningDebugStats.RayTargetPaintable;
            AddLineHeight(ref height, contentWidth, $"Aim: {rayTarget.FormatProgress()}");
            AddLineHeight(ref height, contentWidth, $"  mask: {rayTarget.HasMaskOnRenderer}  zone: {rayTarget.HasZoneOnRenderer}");
        }
        else
        {
            AddLineHeight(ref height, contentWidth, $"Aim: {CleaningDebugStats.HitPaintableName}");
        }

        if (CleaningDebugStats.OverlapPaintables.Count > 0)
        {
            AddLineHeight(ref height, contentWidth, $"Brush overlap ({CleaningDebugStats.OverlapPaintables.Count}):");
            foreach (PaintableDebugEntry overlapPaintable in CleaningDebugStats.OverlapPaintables)
                AddLineHeight(ref height, contentWidth, $"  {overlapPaintable.FormatProgress()}");
        }

        return height;
    }

    void DrawContent(float contentWidth)
    {
        DrawLine(contentWidth, "Cleaning Debug (F9)");
        DrawLine(contentWidth, $"FPS: {Mathf.RoundToInt(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f))}");
        DrawLine(contentWidth, $"Painting: {CleaningDebugStats.IsPainting}  Ray hit: {CleaningDebugStats.RayHit}");
        DrawLine(contentWidth, $"CleanSpeed: {CleaningDebugStats.CleanSpeed:F1}  Strength: {CleaningDebugStats.LastCleanStrength:F4}");
        DrawLine(contentWidth, $"Zone dirt painted: {CleaningDebugStats.ZoneDirtPainted}  Maps: {CleaningDebugStats.ZoneMapsPainted}");
        DrawLine(contentWidth, $"Paintables in brush: {CleaningDebugStats.PaintablesInBrush}");

        if (CleaningDebugStats.HasRayTargetPaintable)
        {
            PaintableDebugEntry rayTarget = CleaningDebugStats.RayTargetPaintable;
            DrawLine(contentWidth, $"Aim: {rayTarget.FormatProgress()}");
            DrawLine(contentWidth, $"  mask: {rayTarget.HasMaskOnRenderer}  zone: {rayTarget.HasZoneOnRenderer}");
        }
        else
        {
            DrawLine(contentWidth, $"Aim: {CleaningDebugStats.HitPaintableName}");
        }

        if (CleaningDebugStats.OverlapPaintables.Count > 0)
        {
            DrawLine(contentWidth, $"Brush overlap ({CleaningDebugStats.OverlapPaintables.Count}):");
            foreach (PaintableDebugEntry overlapPaintable in CleaningDebugStats.OverlapPaintables)
                DrawLine(contentWidth, $"  {overlapPaintable.FormatProgress()}");
        }
    }

    void AddLineHeight(ref float height, float contentWidth, string text)
    {
        height += labelStyle.CalcHeight(new GUIContent(text), contentWidth);
    }

    void DrawLine(float contentWidth, string text)
    {
        float lineHeight = labelStyle.CalcHeight(new GUIContent(text), contentWidth);
        GUILayout.Label(text, labelStyle, GUILayout.Height(lineHeight));
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
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }
}
