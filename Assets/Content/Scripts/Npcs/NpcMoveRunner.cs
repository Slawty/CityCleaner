using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class NpcMoveRunner
{
    public static void Run(NpcMoveInstruction[] moves)
    {
        RunAsync(moves).Forget();
    }

    public static UniTask RunAsync(NpcMoveInstruction[] moves)
    {
        if (moves == null || moves.Length == 0)
            return UniTask.CompletedTask;

        List<UniTask> moveTasks = new List<UniTask>();

        foreach (NpcMoveInstruction move in moves)
        {
            if (move == null || move.npc == null || move.destination == null)
                continue;

            moveTasks.Add(MoveInstructionAsync(move));
        }

        if (moveTasks.Count == 0)
            return UniTask.CompletedTask;

        return UniTask.WhenAll(moveTasks);
    }

    static async UniTask MoveInstructionAsync(NpcMoveInstruction move)
    {
        if (move.stopWandering && move.npc.TryGetComponent(out NpcWander wander))
            wander.StopWandering();

        JobClient jobClient = move.npc.GetComponent<JobClient>();
        if (jobClient == null)
            jobClient = move.npc.GetComponentInParent<JobClient>();

        jobClient?.BeginTransition(move.returnState);

        move.npc.MoveTo(move.destination.position, move.destination);
        await move.npc.WaitUntilArrivedAsync();
        jobClient?.EndTransition();
    }
}
