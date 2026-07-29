using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum JobClientState
{
    Available,
    Active,
    CompletedPendingTurnIn,
    TurnedIn
}

public class JobClient : MonoBehaviour, IInteractable
{
    const string DefaultOfferDialogue = "Please, can you help me clean up around here?";
    const string DefaultCompletionDialogue = "Thanks for completing the job!";
    const string ActiveJobDialogue = "Keep cleaning — you're not done yet!";

    [SerializeField] Job job;
    [SerializeField] string dialogue;
    [SerializeField] string completionDialogue;
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

    JobClientState state = JobClientState.Available;

    public JobClientState State => state;
    public Job Job => job;
    public AnimationSounds AnimationSounds => animationSounds;
    public Transform WaypointTransform => waypointTarget != null ? waypointTarget : transform;
    public string ReturnDestinationName => string.IsNullOrEmpty(returnDestinationName) ? name : returnDestinationName;
    public string OfferDialogue => string.IsNullOrEmpty(dialogue) ? DefaultOfferDialogue : dialogue;
    public string CompletionDialogue => string.IsNullOrEmpty(completionDialogue) ? DefaultCompletionDialogue : completionDialogue;

    public string Prompt => "Talk";

    public event Action SpokenTo;

    void Awake()
    {
        ResolveJobReference();
        ResolveAnimationSounds();
    }

    void Start()
    {
        RefreshSymbols();
    }

    public void Interact(GameObject interactor)
    {
        switch (state)
        {
            case JobClientState.TurnedIn:
                return;
            case JobClientState.CompletedPendingTurnIn:
                if (Managers.Jobs.OfferChainJobOutro(this))
                    break;

                if (job == null)
                {
                    Debug.LogError($"{nameof(JobClient)} on {name}: {nameof(job)} is not assigned.", this);
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
                    Managers.Speech.Show(OfferDialogue);
                    NotifySpokenTo();
                    break;
                }

                if (job.UsesChainFlow)
                {
                    Managers.Jobs.StartJobChain(job);
                    NotifySpokenTo();
                    break;
                }

                Managers.Jobs.OfferJob(this);
                NotifySpokenTo();
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

    public void PayReward()
    {
        Vector3 spawnPos = coinSpawnPoint != null ? coinSpawnPoint.position : transform.position + Vector3.up;
        Vector3 spawnDirection = coinSpawnPoint != null ? coinSpawnPoint.forward : transform.forward;
        Managers.Spawning.SpawnCoins(rewardCoinCount, spawnPos, spawnDirection).Forget();
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
