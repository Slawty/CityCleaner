using System.Collections.Generic;
using UnityEngine;

public struct PaintableDebugEntry
{
    public string Name;
    public int CleanedPixels;
    public int PixelsToClean;
    public float CleanPercent;
    public bool HasMaskOnRenderer;
    public bool HasZoneOnRenderer;

    public static PaintableDebugEntry From(GPUPaintableObject paintable)
    {
        return new PaintableDebugEntry
        {
            Name = paintable.name,
            CleanedPixels = paintable.CleanedPixelCount,
            PixelsToClean = paintable.PixelsToCleanCount,
            CleanPercent = paintable.GetCleanPercent(),
            HasMaskOnRenderer = paintable.RendererHasMaskBound(),
            HasZoneOnRenderer = paintable.RendererHasZoneDirtBound(),
        };
    }

    public string FormatProgress()
    {
        if (PixelsToClean <= 0)
            return $"{Name}: —/— (—)";

        int percent = Mathf.RoundToInt(CleanPercent * 100f);
        return $"{Name}: {CleanedPixels}/{PixelsToClean} ({percent}%)";
    }
}

public static class CleaningDebugStats
{
    public static bool IsPainting;
    public static bool RayHit;
    public static bool ZoneDirtPainted;
    public static int ZoneMapsPainted;
    public static int PaintablesInBrush;
    public static float LastCleanStrength;
    public static float CleanSpeed;
    public static string HitPaintableName = "none";
    public static bool HasRayTargetPaintable;
    public static PaintableDebugEntry RayTargetPaintable;
    public static readonly List<PaintableDebugEntry> OverlapPaintables = new();

    public static void ClearStroke()
    {
        IsPainting = false;
        RayHit = false;
        ZoneDirtPainted = false;
        ZoneMapsPainted = 0;
        PaintablesInBrush = 0;
        LastCleanStrength = 0f;
        HitPaintableName = "none";
        HasRayTargetPaintable = false;
        OverlapPaintables.Clear();
    }
}
