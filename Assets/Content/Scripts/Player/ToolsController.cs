using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolsController : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] ToolEntry[] toolEntries;
    [SerializeField] Vacuum vacuum;

    [Header("UI")]
    [SerializeField] ToolSlot[] toolSlots;

    [Header("Audio")]
    [SerializeField] EventReference toolSwitchEvent;

    [Header("Input")]
    [SerializeField] InputActionReference nextToolAction;
    [SerializeField] InputActionReference prevToolAction;
    [SerializeField] InputActionReference vacuumInteractAction;
    [SerializeField] InputActionReference vacuumReleaseAction;
    [SerializeField] InputActionReference vacuumShootAction;

    readonly Dictionary<PlayerToolType, Tool> toolsByType = new();
    readonly Dictionary<PlayerToolType, bool> toolUnlocked = new();
    readonly List<EquipHotkeyBinding> equipHotkeyBindings = new();

    PlayerToolType? currentToolType;
    PlayerToolType? savedToolType;
    bool vacuumMode;
    bool vacuumCarryMode;

    public Tool CurrentTool { get; private set; }
    public PlayerToolType? CurrentToolType => currentToolType;
    public bool IsInVacuumMode => vacuumMode;
    public bool IsCoinSuctionActive => vacuumMode && !vacuumCarryMode;

    public WaterSprayTool WaterSprayer => GetTool<WaterSprayTool>(PlayerToolType.PowerWasher);
    public LaserGunTool Lasergun => GetTool<LaserGunTool>(PlayerToolType.Laser);
    public GooGunTool GooGun => GetTool<GooGunTool>(PlayerToolType.GooGun);

    void Awake()
    {
        BuildToolRegistry();
        ResolveToolSlots();
    }

    void BuildToolRegistry()
    {
        toolsByType.Clear();

        if (toolEntries == null)
            throw new System.InvalidOperationException($"{nameof(ToolsController)} on {name}: {nameof(toolEntries)} is not assigned.");

        foreach (ToolEntry entry in toolEntries)
        {
            if (entry.tool == null)
                throw new System.InvalidOperationException($"{nameof(ToolsController)} on {name}: {entry.type} is missing a tool reference.");

            if (toolsByType.ContainsKey(entry.type))
                throw new System.InvalidOperationException($"{nameof(ToolsController)} on {name}: duplicate entry for {entry.type}.");

            toolsByType.Add(entry.type, entry.tool);
        }
    }

    void ResolveToolSlots()
    {
        if (toolSlots != null && toolSlots.Length > 0)
            return;

        toolSlots = FindObjectsByType<ToolSlot>(FindObjectsSortMode.None);
    }

    void BindEquipHotkeys()
    {
        UnbindEquipHotkeys();

        foreach (ToolEntry entry in toolEntries)
        {
            if (entry.equipAction == null)
                continue;

            PlayerToolType toolType = entry.type;
            Action<InputAction.CallbackContext> handler = _ => EquipTool(toolType);
            InputAction action = entry.equipAction.action;

            action.Enable();
            action.performed += handler;
            equipHotkeyBindings.Add(new EquipHotkeyBinding(action, handler));
        }
    }

    void UnbindEquipHotkeys()
    {
        foreach (EquipHotkeyBinding binding in equipHotkeyBindings)
        {
            binding.Action.performed -= binding.Handler;
            binding.Action.Disable();
        }

        equipHotkeyBindings.Clear();
    }

    void OnEnable()
    {
        nextToolAction.action.Enable();
        prevToolAction.action.Enable();
        BindEquipHotkeys();

        nextToolAction.action.performed += OnNextTool;
        prevToolAction.action.performed += OnPrevTool;

        vacuumInteractAction.action.Enable();
        vacuumInteractAction.action.performed += OnVacuumInteractPerformed;
        vacuumInteractAction.action.canceled += OnVacuumInteractCanceled;
    }

    void OnDisable()
    {
        nextToolAction.action.performed -= OnNextTool;
        prevToolAction.action.performed -= OnPrevTool;
        UnbindEquipHotkeys();

        nextToolAction.action.Disable();
        prevToolAction.action.Disable();

        vacuumInteractAction.action.performed -= OnVacuumInteractPerformed;
        vacuumInteractAction.action.canceled -= OnVacuumInteractCanceled;
        vacuumInteractAction.action.Disable();
        UnbindVacuumCarryInput();

        if (vacuumMode)
            EndVacuumMode();
    }

    void Start()
    {
        bool startWithNoTools = IntroSequenceController.Instance != null && IntroSequenceController.Instance.UseIntro;

        foreach (ToolEntry entry in toolEntries)
        {
            toolUnlocked[entry.type] = !startWithNoTools;
            entry.tool.Initialize();
            entry.tool.gameObject.SetActive(false);
        }

        currentToolType = null;
        CurrentTool = null;

        if (!startWithNoTools)
            EquipTool(toolEntries[0].type, force: true);

        RefreshToolSlotUnlockStates();

        if (vacuum != null)
            vacuum.gameObject.SetActive(false);
    }

    public T GetTool<T>(PlayerToolType type) where T : Tool
    {
        if (!toolsByType.TryGetValue(type, out Tool tool))
            throw new System.InvalidOperationException($"{nameof(ToolsController)}.{nameof(GetTool)}: {type} is not registered.");

        if (tool is not T typedTool)
            throw new System.InvalidOperationException($"{nameof(ToolsController)}.{nameof(GetTool)}: {type} is not a {typeof(T).Name}.");

        return typedTool;
    }

    public bool IsToolUnlocked(PlayerToolType type)
    {
        return toolUnlocked.TryGetValue(type, out bool unlocked) && unlocked;
    }

    public void UnlockTool(PlayerToolType type)
    {
        if (!toolsByType.ContainsKey(type))
            throw new System.InvalidOperationException($"{nameof(ToolsController)}.{nameof(UnlockTool)}: {type} is not registered.");

        if (IsToolUnlocked(type))
        {
            EquipTool(type);
            return;
        }

        toolUnlocked[type] = true;
        RefreshToolSlotUnlockStates();
        EquipTool(type);
    }

    void OnNextTool(InputAction.CallbackContext ctx) => SelectNextTool();

    void OnPrevTool(InputAction.CallbackContext ctx) => SelectPrevTool();

    public void SelectNextTool()
    {
        if (vacuumMode || toolEntries.Length == 0 || !currentToolType.HasValue)
            return;

        PlayerToolType? next = GetNextUnlockedTool(currentToolType.Value, 1);
        if (next.HasValue)
            EquipTool(next.Value);
    }

    public void SelectPrevTool()
    {
        if (vacuumMode || toolEntries.Length == 0 || !currentToolType.HasValue)
            return;

        PlayerToolType? prev = GetNextUnlockedTool(currentToolType.Value, -1);
        if (prev.HasValue)
            EquipTool(prev.Value);
    }

    PlayerToolType? GetNextUnlockedTool(PlayerToolType fromType, int direction)
    {
        int fromIndex = GetEntryIndex(fromType);
        if (fromIndex < 0)
            return null;

        for (int step = 0; step < toolEntries.Length; step++)
        {
            fromIndex = (fromIndex + direction + toolEntries.Length) % toolEntries.Length;
            PlayerToolType candidate = toolEntries[fromIndex].type;
            if (IsToolUnlocked(candidate))
                return candidate;
        }

        return fromType;
    }

    int GetEntryIndex(PlayerToolType type)
    {
        for (int index = 0; index < toolEntries.Length; index++)
        {
            if (toolEntries[index].type == type)
                return index;
        }

        return -1;
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

        savedToolType = currentToolType;

        if (currentToolType.HasValue)
            toolsByType[currentToolType.Value].gameObject.SetActive(false);

        vacuum.gameObject.SetActive(true);
        vacuum.Begin();
        vacuumMode = true;
        CurrentTool = null;
        currentToolType = null;
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

        PlayerToolType? restoreType = savedToolType;
        savedToolType = null;

        if (restoreType.HasValue)
            EquipTool(restoreType.Value, force: true);
    }

    public void EquipTool(PlayerToolType type, bool force = false)
    {
        if (!toolsByType.TryGetValue(type, out Tool tool))
            return;

        if (!force && !IsToolUnlocked(type))
            return;

        if (vacuumMode && !force)
            return;

        if (!force && currentToolType == type)
            return;

        if (currentToolType.HasValue)
            toolsByType[currentToolType.Value].gameObject.SetActive(false);

        currentToolType = type;
        CurrentTool = tool;
        tool.gameObject.SetActive(true);

        if (!force)
            PlayToolSwitch();

        Debug.Log($"Equipped tool: {tool.name}");
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

        foreach (ToolSlot slot in toolSlots)
        {
            if (!IsToolUnlocked(slot.ToolType))
                continue;

            slot.SetSelected(currentToolType.HasValue && slot.ToolType == currentToolType.Value);
        }
    }

    void RefreshToolSlotUnlockStates()
    {
        if (toolSlots == null)
            return;

        foreach (ToolSlot slot in toolSlots)
            slot.SetUnlocked(IsToolUnlocked(slot.ToolType));
    }

    public Tool GetCurrentTool()
    {
        return CurrentTool;
    }

    public void StopActiveShooting()
    {
        CurrentTool?.StopShooting();
    }

    sealed class EquipHotkeyBinding
    {
        public InputAction Action { get; }
        public Action<InputAction.CallbackContext> Handler { get; }

        public EquipHotkeyBinding(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            Action = action;
            Handler = handler;
        }
    }
}
