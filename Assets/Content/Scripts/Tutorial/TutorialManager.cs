using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    const string WaterRefillInfo =
        "You have run out of water! Find a Cleanling and activate the Vacuum to refill.";

    const string VacuumCoinInfo = "Use RMB to activate the Vacuum";

    const string PowerWasherUpgradeInfo =
        "Go to the work station and upgrade your power washer";

    bool waterRefillTutorialShown;
    bool waitingForWaterRefill;

    bool vacuumCoinTutorialShown;
    bool waitingForFirstCoinCollection;

    bool powerWasherUpgradeTutorialShown;
    bool waitingForPowerWasherUpgrade;

    PowerWasherUpgradeStation powerWasherUpgradeStation;

    public bool IsWaitingForPowerWasherUpgrade => waitingForPowerWasherUpgrade;

    void Start()
    {
        Managers.Tools.WaterSprayer.OnAmmoDepleted += HandleWaterDepleted;
        Managers.Tools.WaterSprayer.OnAmmoRestored += HandleWaterRestored;
        Managers.Spawning.OnCoinSpawned += HandleCoinSpawned;
        CoinParticleMover.OnCoinCollected += HandleCoinCollected;
    }

    void OnDestroy()
    {
        if (Managers.Tools?.WaterSprayer != null)
        {
            Managers.Tools.WaterSprayer.OnAmmoDepleted -= HandleWaterDepleted;
            Managers.Tools.WaterSprayer.OnAmmoRestored -= HandleWaterRestored;
        }

        if (Managers.Spawning != null)
            Managers.Spawning.OnCoinSpawned -= HandleCoinSpawned;

        CoinParticleMover.OnCoinCollected -= HandleCoinCollected;
    }

    public void RegisterPowerWasherUpgradeStation(PowerWasherUpgradeStation station)
    {
        powerWasherUpgradeStation = station;

        if (waitingForPowerWasherUpgrade)
            station.SetAvailable(true);
    }

    public void NotifyJobSequenceCompleted()
    {
        if (powerWasherUpgradeTutorialShown)
            return;

        powerWasherUpgradeTutorialShown = true;
        waitingForPowerWasherUpgrade = true;
        Managers.UI.ShowTutorialInfoText(PowerWasherUpgradeInfo, 0f);
        powerWasherUpgradeStation?.SetAvailable(true);
    }

    public void NotifyPowerWasherUpgraded()
    {
        if (!waitingForPowerWasherUpgrade)
            return;

        waitingForPowerWasherUpgrade = false;
        Managers.UI.HideTutorialInfoText();
        powerWasherUpgradeStation?.SetAvailable(false);
    }

    void HandleCoinSpawned()
    {
        if (vacuumCoinTutorialShown)
            return;

        vacuumCoinTutorialShown = true;
        waitingForFirstCoinCollection = true;
        Managers.UI.ShowTutorialInfoText(VacuumCoinInfo, 0f);
    }

    void HandleCoinCollected()
    {
        if (!waitingForFirstCoinCollection)
            return;

        waitingForFirstCoinCollection = false;
        Managers.UI.HideTutorialInfoText();
    }

    void HandleWaterDepleted()
    {
        if (waterRefillTutorialShown)
            return;

        waterRefillTutorialShown = true;
        waitingForWaterRefill = true;
        Managers.UI.ShowTutorialInfoText(WaterRefillInfo, 0f);
    }

    void HandleWaterRestored()
    {
        if (!waitingForWaterRefill)
            return;

        waitingForWaterRefill = false;
        Managers.UI.HideTutorialInfoText();
    }
}
