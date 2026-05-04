using UnityEngine;

public class AreaManager : MonoBehaviour
{
    DirtArea currentArea;

    public DirtArea CurrentArea => currentArea;

    public void EnterArea(DirtArea area)
    {
        if (area == null)
            return;

        if (currentArea != null && currentArea != area)
            currentArea.SetDrivingCleanUi(false);

        currentArea = area;
        currentArea.SetDrivingCleanUi(true);
        currentArea.RefreshProgressAndPushUi();
    }

    public void ExitArea(DirtArea area)
    {
        if (currentArea != area)
            return;

        currentArea.SetDrivingCleanUi(false);
        currentArea = null;
        Managers.UI.SetCleanProgressBarPercent(0f);
    }
}
