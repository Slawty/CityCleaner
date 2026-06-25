using UnityEngine;

public class AreaManager : MonoBehaviour
{
    DirtArea currentArea;

    public DirtArea CurrentArea => currentArea;

    public void EnterArea(DirtArea area)
    {
        if (area == null)
            return;

        currentArea = area;
        currentArea.RefreshProgressAndPushUi();
    }

    public void ExitArea(DirtArea area)
    {
        if (currentArea != area)
            return;

        currentArea = null;
        Managers.UI.ShowRadioactivesProgress(false);
        Managers.UI.SetRadioactivesProgressBarPercent(0f);
    }
}
