using System;
using UnityEngine;

[Serializable]
public class NpcMoveInstruction
{
    public NpcNavMovement npc;
    public Transform destination;
    public bool stopWandering = true;
    public JobClientState returnState = JobClientState.Available;

    [Header("Presentation")]
    public string expressionOnStart;
    public string expressionOnArrive;
    public string[] enableEffectsOnStart = Array.Empty<string>();
    public string[] disableEffectsOnArrive = Array.Empty<string>();
}
