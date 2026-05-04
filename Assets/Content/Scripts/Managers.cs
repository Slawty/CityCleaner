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

    private void Awake()
    {
        Instance = this;
    }

    public static Camera MainCam => Instance.mainCam;
    public static UIManager UI => Instance.uIManager;
    public static SpawnManager Spawning => Instance.spawnManager;
    public static Player Player => Instance.player;
    public static ToolsController Tools => Instance.toolsController;
    public static AreaManager Areas => Instance.areaManager;
    public static InventoryManager Inventory => Instance.inventoryManager;
    public static InputManager Input => Instance.inputManager;
}
