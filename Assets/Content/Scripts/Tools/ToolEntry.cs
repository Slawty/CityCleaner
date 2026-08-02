using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerToolType
{
    Laser,
    PowerWasher,
    GooGun
}

[Serializable]
public struct ToolEntry
{
    public PlayerToolType type;
    public Tool tool;
    public InputActionReference equipAction;
}
