using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class JobStep
{
    public Job job;
    public JobClient speaker;
    [TextArea(2, 6)] public string[] introDialogues;
    [TextArea(2, 6)] public string[] outroDialogues;
    public NpcMoveInstruction[] movesBeforeIntro;
    public NpcMoveInstruction[] movesAfterOutro;
    public bool payRewardOnComplete;
    public UnityEvent onJobStarted;
    public UnityEvent onJobCompleted;
}
