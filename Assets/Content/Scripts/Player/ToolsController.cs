using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolsController : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] private List<Tool> tools;
    public WaterSprayTool WaterSprayer;
    public GooGunTool GooGun;

    [Header("Input")]
    [SerializeField] private InputActionReference nextToolAction;
    [SerializeField] private InputActionReference prevToolAction;

    private int currentToolIndex = -1;
    public Tool CurrentTool { get; private set; }

    private void OnEnable()
    {
        nextToolAction.action.performed += OnNextTool;
        prevToolAction.action.performed += OnPrevTool;
    }

    private void OnDisable()
    {
        nextToolAction.action.performed -= OnNextTool;
        prevToolAction.action.performed -= OnPrevTool;
    }

    private void Start()
    {
        foreach (var tool in tools)
        {
            if (tool.isActiveAndEnabled)
                EquipTool(tools.IndexOf(tool));
        }
    }

    private void OnNextTool(InputAction.CallbackContext ctx)
    {
        int next = (currentToolIndex + 1) % tools.Count;
        EquipTool(next);
    }

    private void OnPrevTool(InputAction.CallbackContext ctx)
    {
        int prev = currentToolIndex - 1;
        if (prev < 0) prev = tools.Count - 1;
        EquipTool(prev);
    }

    public void EquipTool(int index)
    {
        if (index < 0 || index >= tools.Count)
            return;

        if (currentToolIndex == index)
            return;

        // Disable current
        if (currentToolIndex >= 0)
            tools[currentToolIndex].gameObject.SetActive(false);

        // Enable new
        currentToolIndex = index;
        CurrentTool = tools[currentToolIndex];
        CurrentTool.gameObject.SetActive(true);

        Debug.Log($"Equipped tool: {tools[currentToolIndex].name}");
    }

    public Tool GetCurrentTool()
    {
        if (currentToolIndex < 0) return null;
        return tools[currentToolIndex];
    }
}
