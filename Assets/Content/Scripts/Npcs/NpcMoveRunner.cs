using UnityEngine;

public static class NpcMoveRunner
{
    public static void Run(NpcMoveInstruction[] moves)
    {
        if (moves == null)
            return;

        foreach (NpcMoveInstruction move in moves)
        {
            if (move == null || move.npc == null || move.destination == null)
                continue;

            if (move.stopWandering && move.npc.TryGetComponent(out NpcWander wander))
                wander.StopWandering();

            move.npc.MoveTo(move.destination.position);
        }
    }
}
