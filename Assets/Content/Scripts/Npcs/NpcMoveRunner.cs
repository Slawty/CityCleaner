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

        ApplyMovePresentationStart(move);
        move.npc.MoveTo(move.destination.position, move.destination);
        await move.npc.WaitUntilArrivedAsync();
        ApplyMovePresentationArrive(move);
        jobClient?.EndTransition();
    }

    static void ApplyMovePresentationStart(NpcMoveInstruction move)
    {
        NpcExpressionController expressionController = GetExpressionController(move.npc);
        if (expressionController == null)
            return;

        if (!string.IsNullOrEmpty(move.expressionOnStart))
            expressionController.SetExpression(move.expressionOnStart);

        ApplyEffectNames(expressionController, move.enableEffectsOnStart, active: true);
    }

    static void ApplyMovePresentationArrive(NpcMoveInstruction move)
    {
        NpcExpressionController expressionController = GetExpressionController(move.npc);
        if (expressionController == null)
            return;

        if (!string.IsNullOrEmpty(move.expressionOnArrive))
            expressionController.SetExpression(move.expressionOnArrive);

        ApplyEffectNames(expressionController, move.disableEffectsOnArrive, active: false);
    }

    static NpcExpressionController GetExpressionController(NpcNavMovement npc)
    {
        if (npc == null)
            return null;

        NpcExpressionController expressionController = npc.GetComponent<NpcExpressionController>();
        if (expressionController != null)
            return expressionController;

        return npc.GetComponentInParent<NpcExpressionController>();
    }

    static void ApplyEffectNames(NpcExpressionController expressionController, string[] effectNames, bool active)
    {
        if (effectNames == null || effectNames.Length == 0)
            return;

        foreach (string effectName in effectNames)
        {
            if (string.IsNullOrEmpty(effectName))
                continue;

            expressionController.SetEffect(effectName, active);
        }
    }
}
