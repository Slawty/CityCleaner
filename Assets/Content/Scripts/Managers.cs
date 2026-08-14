using UnityEngine;

public class Managers : MonoBehaviour
{
    private static Managers Instance;

    [SerializeField] Camera mainCam;
    [SerializeField] UIManager uIManager;
    [SerializeField] SpawnManager spawnManager;
    [SerializeField] Player player;
    [SerializeField] ToolsController toolsController;
    [SerializeField] AreaManager areaManager;
    [SerializeField] InventoryManager inventoryManager;
    [SerializeField] InputManager inputManager;
    [SerializeField] SpeechPanelController speechPanelController;
    [SerializeField] JobManager jobManager;
    [SerializeField] MaterialManager materialManager;
    [SerializeField] TutorialManager tutorialManager;
    [SerializeField] UpgradeProgressManager upgradeProgressManager;
    [SerializeField] UpgradeMenuController upgradeMenuController;
    [SerializeField] SettingsMenuController settingsMenuController;

    private void Awake()
    {
        Instance = this;
    }

    public static bool IsInitialized => Instance != null;

    public static Camera MainCam => Instance.mainCam;
    public static UIManager UI => Instance.uIManager;
    public static SpawnManager Spawning => Instance.spawnManager;
    public static Player Player => Instance.player;
    public static ToolsController Tools => Instance.toolsController;
    public static AreaManager Areas => Instance.areaManager;
    public static InventoryManager Inventory => Instance.inventoryManager;
    public static InputManager Input => Instance.inputManager;
    public static SpeechPanelController Speech => Instance.speechPanelController;
    public static JobManager Jobs => Instance.jobManager;
    public static MaterialManager Materials => Instance.materialManager;
    public static TutorialManager Tutorial => Instance.tutorialManager;
    public static UpgradeProgressManager Upgrades => Instance.upgradeProgressManager;
    public static UpgradeMenuController UpgradeMenu => Instance.upgradeMenuController;
    public static SettingsMenuController SettingsMenu => Instance.settingsMenuController;
}
