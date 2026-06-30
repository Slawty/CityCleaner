using UnityEngine;

public class JobSequence : MonoBehaviour
{
    [SerializeField] JobStep[] steps;
    [SerializeField] bool autoStartOnLoad;
    [SerializeField] bool triggerTutorialOnComplete;

    int currentStepIndex;
    bool sequenceActive;
    bool firstStepPreIntroMovesRan;
    bool waitingForStepOutro;

    void Start()
    {
        if (autoStartOnLoad)
            RunFirstStepPreIntroMoves();
    }

    public void StartSequence()
    {
        if (sequenceActive)
            return;

        if (steps == null || steps.Length == 0)
        {
            Debug.LogError($"{nameof(JobSequence)} on {name}: at least one {nameof(JobStep)} is required.", this);
            return;
        }

        currentStepIndex = 0;
        sequenceActive = true;
        BeginCurrentStep();
    }

    void RunFirstStepPreIntroMoves()
    {
        if (steps == null || steps.Length == 0)
            return;

        NpcMoveRunner.Run(steps[0].movesBeforeIntro);
        firstStepPreIntroMovesRan = true;
    }

    void BeginCurrentStep()
    {
        JobStep step = steps[currentStepIndex];
        if (step.job == null)
        {
            Debug.LogError($"{nameof(JobSequence)} on {name}: step {currentStepIndex} is missing a job reference.", this);
            return;
        }

        bool skipPreIntroMoves = currentStepIndex == 0 && firstStepPreIntroMovesRan;
        if (!skipPreIntroMoves)
            NpcMoveRunner.Run(step.movesBeforeIntro);

        if (HasDialogues(step.introDialogues))
            Managers.Speech.ShowDialogueSequence(step.introDialogues, OnIntroFinished);
        else
            OnIntroFinished();
    }

    void OnIntroFinished()
    {
        JobStep step = steps[currentStepIndex];
        step.onJobStarted?.Invoke();
        Managers.Jobs.StartSequenceJob(step.job, step.speaker, OnJobObjectivesCompleted);
    }

    void OnJobObjectivesCompleted()
    {
        JobStep step = steps[currentStepIndex];
        step.onJobCompleted?.Invoke();

        if (step.speaker != null)
        {
            waitingForStepOutro = true;
            step.speaker.SetState(JobClientState.CompletedPendingTurnIn);
        }
        else
            OnOutroFinished();
    }

    public void OfferCurrentStepOutro(JobClient client)
    {
        if (!sequenceActive || !waitingForStepOutro || client == null)
            return;

        JobStep step = steps[currentStepIndex];
        if (step.speaker != client)
            return;

        if (HasDialogues(step.outroDialogues))
            Managers.Speech.ShowDialogueSequence(step.outroDialogues, OnOutroFinished);
        else
            OnOutroFinished();
    }

    void OnOutroFinished()
    {
        waitingForStepOutro = false;
        JobStep step = steps[currentStepIndex];
        NpcMoveRunner.Run(step.movesAfterOutro);

        if (step.payRewardOnComplete && step.speaker != null)
            step.speaker.PayReward();

        bool isLastStep = currentStepIndex >= steps.Length - 1;
        if (isLastStep && step.speaker != null)
            step.speaker.SetState(JobClientState.TurnedIn);

        currentStepIndex++;
        if (currentStepIndex < steps.Length)
        {
            JobStep nextStep = steps[currentStepIndex];
            if (nextStep.speaker != null)
                nextStep.speaker.SetState(JobClientState.Available);

            BeginCurrentStep();
            return;
        }

        sequenceActive = false;

        if (triggerTutorialOnComplete)
            Managers.Tutorial.NotifyJobSequenceCompleted();
    }

    static bool HasDialogues(string[] dialogues)
    {
        return dialogues != null && dialogues.Length > 0;
    }
}
