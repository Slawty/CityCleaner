using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class JobPresentation
{
    [TextArea(2, 6)] public string[] introDialogues;
    [TextArea(2, 6)] public string[] outroDialogues;
    public NpcMoveInstruction[] movesBeforeIntro;
    public NpcMoveInstruction[] movesAfterOutro;
    public bool payRewardOnComplete;
    public UnityEvent onJobStarted;
    public UnityEvent onJobCompleted;
}
