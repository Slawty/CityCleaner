using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolsController : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] List<Tool> tools;
    [SerializeField] Vacuum vacuum;
    public WaterSprayTool WaterSprayer;
    public LaserGunTool Lasergun;
    public GooGunTool GooGun;

    [Header("UI")]
    [SerializeField] ToolSlot[] toolSlots;

    [Header("Audio")]
    [SerializeField] EventReference toolSwitchEvent;

    [Header("Input")]
    [SerializeField] InputActionReference nextToolAction;
    [SerializeField] InputActionReference prevToolAction;
    [SerializeField] InputActionReference tool1Action;
    [SerializeField] InputActionReference tool2Action;
    [SerializeField] InputActionReference tool3Action;
    [SerializeField] InputActionReference vacuumInteractAction;
    [SerializeField] InputActionReference vacuumReleaseAction;
    [SerializeField] InputActionReference vacuumShootAction;

    const int LaserToolIndex = 0;
    const int PowerWasherToolIndex = 1;
    const int GooGunToolIndex = 2;

    int currentToolIndex = -1;
    int savedToolIndex = -1;
    bool vacuumMode;
    bool vacuumCarryMode;

    public Tool CurrentTool { get; private set; }
    public int CurrentToolIndex => currentToolIndex;
    public bool IsInVacuumMode => vacuumMode;

    void Awake()
    {
        ResolveToolHotkeyActions();
        ResolveToolSlots();
    }

    void ResolveToolSlots()
    {
        if (toolSlots != null && toolSlots.Length > 0)
            return;

        toolSlots = FindObjectsByType<ToolSlot>(FindObjectsSortMode.None);
    }

    void ResolveToolHotkeyActions()
    {
        if (tool1Action != null && tool2Action != null && tool3Action != null)
            return;

        InputActionAsset asset = nextToolAction.action.actionMap.asset;
        InputActionMap playerMap = asset.FindActionMap("Player");

        if (tool1Action == null)
            tool1Action = InputActionReference.Create(playerMap.FindAction("Tool 1"));
        if (tool2Action == null)
            tool2Action = InputActionReference.Create(playerMap.FindAction("Tool 2"));
        if (tool3Action == null)
            tool3Action = InputActionReference.Create(playerMap.FindAction("Tool 3"));
    }

    void OnEnable()
    {
        nextToolAction.action.Enable();
        prevToolAction.action.Enable();
        tool1Action.action.Enable();
        tool2Action.action.Enable();
        tool3Action.action.Enable();

        nextToolAction.action.performed += OnNextTool;
        prevToolAction.action.performed += OnPrevTool;
        tool1Action.action.performed += OnTool1;
        tool2Action.action.performed += OnTool2;
        tool3Action.action.performed += OnTool3;

        vacuumInteractAction.action.Enable();
        vacuumInteractAction.action.performed += OnVacuumInteractPerformed;
        vacuumInteractAction.action.canceled += OnVacuumInteractCanceled;
    }

    void OnDisable()
    {
        nextToolAction.action.performed -= OnNextTool;
        prevToolAction.action.performed -= OnPrevTool;
        tool1Action.action.performed -= OnTool1;
        tool2Action.action.performed -= OnTool2;
        tool3Action.action.performed -= OnTool3;

        nextToolAction.action.Disable();
        prevToolAction.action.Disable();
        tool1Action.action.Disable();
        tool2Action.action.Disable();
        tool3Action.action.Disable();

        vacuumInteractAction.action.performed -= OnVacuumInteractPerformed;
        vacuumInteractAction.action.canceled -= OnVacuumInteractCanceled;
        vacuumInteractAction.action.Disable();
        UnbindVacuumCarryInput();

        if (vacuumMode)
            EndVacuumMode();
    }

    void Start()
    {
        foreach (Tool tool in tools)
            tool.Initialize();

        if (vacuum != null)
            vacuum.gameObject.SetActive(false);

        int initialIndex = LaserToolIndex;
        for (int i = 0; i < tools.Count; i++)
        {
            if (tools[i].gameObject.activeSelf)
            {
                initialIndex = i;
                break;
            }
        }

        EquipTool(initialIndex, force: true);
    }

    void OnNextTool(InputAction.CallbackContext ctx) => SelectNextTool();

    void OnPrevTool(InputAction.CallbackContext ctx) => SelectPrevTool();

    void OnTool1(InputAction.CallbackContext ctx) => EquipTool(LaserToolIndex);

    void OnTool2(InputAction.CallbackContext ctx) => EquipTool(PowerWasherToolIndex);

    void OnTool3(InputAction.CallbackContext ctx) => EquipTool(GooGunToolIndex);

    public void SelectNextTool()
    {
        if (vacuumMode || tools.Count == 0)
            return;

        int next = currentToolIndex < 0 ? 0 : (currentToolIndex + 1) % tools.Count;
        EquipTool(next);
    }

    public void SelectPrevTool()
    {
        if (vacuumMode || tools.Count == 0)
            return;

        int prev = currentToolIndex - 1;
        if (prev < 0)
            prev = tools.Count - 1;
        EquipTool(prev);
    }

    void OnVacuumInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        BeginVacuumMode();
    }

    void OnVacuumInteractCanceled(InputAction.CallbackContext ctx)
    {
        if (!vacuumMode)
            return;

        if (vacuum.HasCarryTarget)
        {
            vacuum.EnterCarryMode();
            BeginVacuumCarryInput();
            return;
        }

        EndVacuumMode();
    }

    void BeginVacuumCarryInput()
    {
        if (vacuumCarryMode)
            return;

        vacuumCarryMode = true;
        vacuumReleaseAction.action.canceled += OnVacuumRelease;
        vacuumShootAction.action.performed += OnVacuumShoot;
    }

    void UnbindVacuumCarryInput()
    {
        if (!vacuumCarryMode)
            return;

        vacuumCarryMode = false;
        vacuumReleaseAction.action.canceled -= OnVacuumRelease;
        vacuumShootAction.action.performed -= OnVacuumShoot;
    }

    void OnVacuumRelease(InputAction.CallbackContext ctx)
    {
        if (!vacuumMode)
            return;

        vacuum.ReleaseCarried();
        EndVacuumMode();
    }

    void OnVacuumShoot(InputAction.CallbackContext ctx)
    {
        if (!vacuumMode || Managers.Input.InteractionBlocked())
            return;

        vacuum.ShootCarried();
        EndVacuumMode();
    }

    public void BeginVacuumMode()
    {
        if (vacuumMode || vacuum == null)
            return;

        savedToolIndex = currentToolIndex;

        if (currentToolIndex >= 0)
            tools[currentToolIndex].gameObject.SetActive(false);

        vacuum.gameObject.SetActive(true);
        vacuum.Begin();
        vacuumMode = true;
        CurrentTool = null;
        RefreshToolSlotVisuals();
    }

    public void EndVacuumMode()
    {
        if (!vacuumMode || vacuum == null)
            return;

        UnbindVacuumCarryInput();
        vacuum.End();
        vacuum.gameObject.SetActive(false);
        vacuumMode = false;

        int restoreIndex = savedToolIndex;
        savedToolIndex = -1;

        if (restoreIndex >= 0)
            EquipTool(restoreIndex, force: true);
    }

    public void EquipTool(int index, bool force = false)
    {
        if (index < 0 || index >= tools.Count)
            return;

        if (vacuumMode && !force)
            return;

        if (!force && currentToolIndex == index)
            return;

        if (currentToolIndex >= 0)
            tools[currentToolIndex].gameObject.SetActive(false);

        currentToolIndex = index;
        CurrentTool = tools[currentToolIndex];
        CurrentTool.gameObject.SetActive(true);

        if (!force)
            PlayToolSwitch();

        Debug.Log($"Equipped tool: {tools[currentToolIndex].name}");
        RefreshToolSlotVisuals();
    }

    void PlayToolSwitch()
    {
        if (toolSwitchEvent.IsNull)
            throw new System.InvalidOperationException("Tool switch FMOD event is not assigned on ToolsController.");

        RuntimeManager.PlayOneShotAttached(toolSwitchEvent, gameObject);
    }

    void RefreshToolSlotVisuals()
    {
        if (toolSlots == null)
            return;

        int activeIndex = vacuumMode ? -1 : currentToolIndex;

        foreach (ToolSlot slot in toolSlots)
            slot.SetSelected(slot.ToolIndex == activeIndex);
    }

    public Tool GetCurrentTool()
    {
        if (currentToolIndex < 0)
            return null;
        return tools[currentToolIndex];
    }

    public void StopActiveShooting()
    {
        if (currentToolIndex >= 0)
            tools[currentToolIndex].StopShooting();
    }
}
