using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class ToolsController : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] List<Tool> tools;
    [SerializeField] Vacuum vacuum;
    public WaterSprayTool WaterSprayer;
    public LaserGunTool Lasergun;
    public GooGunTool GooGun;

    [Header("Input")]
    [SerializeField] InputActionReference nextToolAction;
    [SerializeField] InputActionReference prevToolAction;
    [SerializeField] InputActionReference vacuumInteractAction;
    [SerializeField] InputActionReference vacuumReleaseAction;
    [SerializeField] InputActionReference vacuumShootAction;

    int currentToolIndex = -1;
    int savedToolIndex = -1;
    bool vacuumMode;
    bool vacuumCarryMode;

    public Tool CurrentTool { get; private set; }
    public bool IsInVacuumMode => vacuumMode;

    void OnEnable()
    {
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
        {
            if (tool.isActiveAndEnabled)
                EquipTool(tools.IndexOf(tool));

            tool.Initialize();
        }

        if (vacuum != null)
            vacuum.gameObject.SetActive(false);
    }

    void OnNextTool(InputAction.CallbackContext ctx)
    {
        if (vacuumMode)
            return;

        int next = (currentToolIndex + 1) % tools.Count;
        EquipTool(next);
    }

    void OnPrevTool(InputAction.CallbackContext ctx)
    {
        if (vacuumMode)
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

        if (ctx.interaction is not HoldInteraction)
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
        if (!vacuumMode)
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

        if (!force && currentToolIndex == index)
            return;

        if (currentToolIndex >= 0)
            tools[currentToolIndex].gameObject.SetActive(false);

        currentToolIndex = index;
        CurrentTool = tools[currentToolIndex];
        CurrentTool.gameObject.SetActive(true);

        Debug.Log($"Equipped tool: {tools[currentToolIndex].name}");
    }

    public Tool GetCurrentTool()
    {
        if (currentToolIndex < 0)
            return null;
        return tools[currentToolIndex];
    }
}
