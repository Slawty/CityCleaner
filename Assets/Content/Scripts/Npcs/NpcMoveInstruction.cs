using System;
using UnityEngine;

[Serializable]
public class NpcMoveInstruction
{
    public NpcNavMovement npc;
    public Transform destination;
    public bool stopWandering = true;
}
