using UnityEngine;

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
    public static string PrimaryPaintableName = "none";
    public static float PrimaryCleanPercent;
    public static bool PrimaryHasMaskOnRenderer;
    public static bool PrimaryHasZoneOnRenderer;

    public static void ClearStroke()
    {
        IsPainting = false;
        RayHit = false;
        ZoneDirtPainted = false;
        ZoneMapsPainted = 0;
        PaintablesInBrush = 0;
        LastCleanStrength = 0f;
        HitPaintableName = "none";
        PrimaryPaintableName = "none";
        PrimaryCleanPercent = 0f;
        PrimaryHasMaskOnRenderer = false;
        PrimaryHasZoneOnRenderer = false;
    }
}
