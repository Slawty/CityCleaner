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
    [SerializeField] JobSequence jobSequence;
    [SerializeField] string dialogue;
    [SerializeField] string completionDialogue;
    [Header("Symbols")]
    [SerializeField] GameObject jobAvailableSymbol;
    [SerializeField] GameObject jobPendingSymbol;
    [SerializeField] GameObject jobCompletedSymbol;
    [Header("Reward")]
    [SerializeField] Transform coinSpawnPoint;
    [SerializeField] int rewardCoinCount = 20;

    JobClientState state = JobClientState.Available;

    public JobClientState State => state;
    public Job Job => job;
    public JobSequence Sequence => jobSequence;
    public string OfferDialogue => string.IsNullOrEmpty(dialogue) ? DefaultOfferDialogue : dialogue;
    public string CompletionDialogue => string.IsNullOrEmpty(completionDialogue) ? DefaultCompletionDialogue : completionDialogue;

    public string Prompt => "Talk";

    void Awake()
    {
        ResolveJobReference();
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
                if (jobSequence != null)
                {
                    jobSequence.OfferCurrentStepOutro(this);
                    break;
                }

                if (job == null)
                {
                    Debug.LogError($"{nameof(JobClient)} on {name}: {nameof(job)} is not assigned.", this);
                    return;
                }

                Managers.Jobs.OfferTurnIn(this);
                break;
            case JobClientState.Active:
                Managers.Speech.Show(ActiveJobDialogue);
                break;
            default:
                if (jobSequence != null)
                {
                    jobSequence.StartSequence();
                    break;
                }

                if (job == null)
                {
                    Debug.LogError($"{nameof(JobClient)} on {name}: assign {nameof(job)} or {nameof(jobSequence)}.", this);
                    return;
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
}
