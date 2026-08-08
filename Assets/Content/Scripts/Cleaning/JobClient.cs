using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum JobClientState
{
    Available,
    Active,
    CompletedPendingTurnIn,
    TurnedIn,
    Transitioning
}

public class JobClient : MonoBehaviour, IInteractable
{
    const string ActiveJobDialogue = "Keep cleaning — you're not done yet!";

    [SerializeField] Job job;
    [Header("Symbols")]
    [SerializeField] GameObject jobAvailableSymbol;
    [SerializeField] GameObject jobPendingSymbol;
    [SerializeField] GameObject jobCompletedSymbol;
    [Header("Reward")]
    [SerializeField] Transform coinSpawnPoint;
    [SerializeField] int rewardCoinCount = 20;
    [Header("Return")]
    [SerializeField] string returnDestinationName;
    [Header("Waypoint")]
    [SerializeField] Transform waypointTarget;
    [Header("Audio")]
    [SerializeField] AnimationSounds animationSounds;

    NpcNavMovement navMovement;

    JobClientState state = JobClientState.Available;
    JobClientState transitionReturnState = JobClientState.Available;
    Quaternion dialogueReturnRotation;
    bool isInDialogueFacing;
    bool hasPendingReturnRotation;
    bool hasSavedDialogueReturnRotation;
    CancellationTokenSource dialogueFacingCts;

    public JobClientState State => state;
    public Job Job => job;
    public AnimationSounds AnimationSounds => animationSounds;
    public Transform WaypointTransform => waypointTarget != null ? waypointTarget : transform;
    public string ReturnDestinationName => string.IsNullOrEmpty(returnDestinationName) ? name : returnDestinationName;

    public string Prompt => CanTalk ? "Talk" : "";

    public event Action SpokenTo;

    bool CanTalk => state != JobClientState.Transitioning && state != JobClientState.TurnedIn;

    void Awake()
    {
        ResolveJobReference();
        ResolveAnimationSounds();
        ResolveNavMovement();
    }

    void Start()
    {
        RefreshSymbols();
    }

    void OnDestroy()
    {
        CancelDialogueFacingFlow(releaseSavedRotation: true);
    }

    public void Interact(GameObject interactor)
    {
        switch (state)
        {
            case JobClientState.TurnedIn:
            case JobClientState.Transitioning:
                return;
        }

        BeginDialogueFacing(interactor.transform.position);
        Managers.Speech.SetDialogueClient(this);

        switch (state)
        {
            case JobClientState.CompletedPendingTurnIn:
                Managers.UI.UnregisterReturnMessage(this);

                if (Managers.Jobs.OfferChainJobOutro(this))
                    break;

                if (job == null)
                {
                    Debug.LogError($"{nameof(JobClient)} on {name}: {nameof(job)} is not assigned.", this);
                    CancelDialogueFacingSetup();
                    return;
                }

                Managers.Jobs.OfferTurnIn(this);
                break;
            case JobClientState.Active:
                Managers.Speech.Show(ActiveJobDialogue);
                NotifySpokenTo();
                break;
            default:
                if (job == null)
                {
                    Debug.LogError($"{nameof(JobClient)} on {name}: {nameof(job)} is not assigned.", this);
                    CancelDialogueFacingSetup();
                    return;
                }

                NotifySpokenTo();

                if (Managers.Jobs.TryStartPendingChainJobFromTalk(this))
                    break;

                if (job.UsesChainFlow)
                {
                    Managers.Jobs.StartJobChain(job);
                    break;
                }

                Managers.Jobs.OfferJob(this);
                break;
        }
    }

    public void InteractReleased(GameObject interactor)
    {
    }

    public void SetState(JobClientState newState)
    {
        state = newState;
        RefreshSymbols();
        Managers.Jobs?.RefreshWaypoint();
    }

    public void BeginTransition(JobClientState returnState)
    {
        PrepareForScriptedMove();
        transitionReturnState = returnState;
        state = JobClientState.Transitioning;
        RefreshSymbols();
    }

    public void EndTransition()
    {
        if (state != JobClientState.Transitioning)
            return;

        SetState(transitionReturnState);
    }

    public void PayReward()
    {
        Vector3 spawnPos = coinSpawnPoint != null ? coinSpawnPoint.position : transform.position + Vector3.up;
        Managers.Spawning.SpawnCoins(rewardCoinCount, spawnPos).Forget();
    }

    void ResolveJobReference()
    {
        if (job != null)
            return;

        job = GetComponent<Job>();
        if (job == null)
            job = GetComponentInChildren<Job>();
    }

    void ResolveAnimationSounds()
    {
        if (animationSounds != null)
            return;

        animationSounds = GetComponent<AnimationSounds>();
        if (animationSounds == null)
            animationSounds = GetComponentInChildren<AnimationSounds>();
    }

    void ResolveNavMovement()
    {
        if (navMovement != null)
            return;

        navMovement = GetComponent<NpcNavMovement>();
    }

    public void BeginDialogueFacing(Vector3 lookAtWorldPoint)
    {
        if (navMovement == null)
            throw new InvalidOperationException($"{nameof(JobClient)} on {name}: {nameof(NpcNavMovement)} is not assigned.");

        navMovement.Stop();

        if (!hasPendingReturnRotation)
            dialogueReturnRotation = transform.rotation;

        hasSavedDialogueReturnRotation = true;
        isInDialogueFacing = true;

        CancellationToken cancellationToken = StartDialogueFacingFlow();
        FaceTowardPlayerAsync(lookAtWorldPoint, cancellationToken).Forget();
    }

    public void PrepareForScriptedMove()
    {
        CancelDialogueFacingFlow(releaseSavedRotation: true);
        isInDialogueFacing = false;
    }

    public bool TryGetDialogueReturnRotation(out Quaternion rotation)
    {
        if (!hasSavedDialogueReturnRotation)
        {
            rotation = default;
            return false;
        }

        rotation = dialogueReturnRotation;
        return true;
    }

    public void ReleaseDialogueReturnRotation()
    {
        hasSavedDialogueReturnRotation = false;
        hasPendingReturnRotation = false;
    }

    public void EndDialogueFacing(bool restoreRotation = true)
    {
        if (!isInDialogueFacing)
            return;

        isInDialogueFacing = false;

        if (!restoreRotation)
            return;

        hasPendingReturnRotation = true;
        CancellationToken cancellationToken = StartDialogueFacingFlow();
        RestoreDialogueRotationAsync(cancellationToken).Forget();
    }

    CancellationToken StartDialogueFacingFlow()
    {
        CancelDialogueFacingFlow(releaseSavedRotation: false);
        dialogueFacingCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        return dialogueFacingCts.Token;
    }

    void CancelDialogueFacingFlow(bool releaseSavedRotation)
    {
        if (dialogueFacingCts != null)
        {
            dialogueFacingCts.Cancel();
            dialogueFacingCts.Dispose();
            dialogueFacingCts = null;
        }

        navMovement?.CancelFacing();

        if (releaseSavedRotation)
            ReleaseDialogueReturnRotation();
    }

    async UniTaskVoid FaceTowardPlayerAsync(Vector3 lookAtWorldPoint, CancellationToken cancellationToken)
    {
        try
        {
            await navMovement.FacePointAsync(lookAtWorldPoint, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async UniTaskVoid RestoreDialogueRotationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await navMovement.FaceRotationAsync(dialogueReturnRotation, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        ReleaseDialogueReturnRotation();
    }

    void CancelDialogueFacingSetup()
    {
        EndDialogueFacing();
        Managers.Speech.SetDialogueClient(null);
    }

    void RefreshSymbols()
    {
        bool showAvailable = state == JobClientState.Available;
        bool showPending = state == JobClientState.Active;
        bool showCompleted = state == JobClientState.CompletedPendingTurnIn;

        SetSymbolActive(jobAvailableSymbol, showAvailable);

        if (jobPendingSymbol != null && jobCompletedSymbol != null && jobPendingSymbol == jobCompletedSymbol)
        {
            SetSymbolActive(jobPendingSymbol, showPending || showCompleted);
            return;
        }

        SetSymbolActive(jobPendingSymbol, showPending);
        SetSymbolActive(jobCompletedSymbol, showCompleted);
    }

    static void SetSymbolActive(GameObject symbol, bool active)
    {
        if (symbol != null)
            symbol.SetActive(active);
    }

    void NotifySpokenTo()
    {
        SpokenTo?.Invoke();
    }
}
