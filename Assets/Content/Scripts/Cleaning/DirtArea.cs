using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

/// <summary>
/// Root for a neighborhood: counts objectives (GPU cleanables, goo growables, splitables) and drives the HUD while the player is inside an <see cref="AreaTrigger"/>.
/// GPU paintables with <see cref="GPUPaintableObject.UseContinuousProgress"/> contribute partial clean percent on each tracking update.
/// Other objectives advance only when fully completed.
/// </summary>
public class DirtArea : MonoBehaviour
{
    [SerializeField] Volume radioactivePostProcessVolume;
    [SerializeField] AreaTrigger areaTrigger;
    [SerializeField] GameObject indicator;
    [SerializeField] GameObject visualBorder;
    [Header("GPU dirt")]
    [SerializeField] bool initializeGpuPaintablesOnAwake = true;

    [Header("Job")]
    [SerializeField, Range(0f, 1f)] float jobCompletionFraction = 1f;

    [Header("Debug")]
    [SerializeField, Range(0f, 100f)] float debugCleanPercent = 10f;

    [Header("Events")]
    public UnityEvent<float> OnAreaProgressChanged;

    List<GPUPaintableObject> paintables = new();
    List<GooHitGrowable> gooGrowables = new();
    List<SplitableObject> splitables = new();
    List<SplitableObject> radioactives = new();


    int totalTargets;
    int completedTargets;
    int totalRadioactivetargets;
    int completedRadioactivetargets;

    bool subscribed;
    bool playerInsideArea;
    bool jobTargetActive;
    bool jobCompleted;

    public float NormalizedProgress { get; private set; }
    public float JobCompletionFraction => jobCompletionFraction;
    public bool IsJobTargetActive => jobTargetActive;

    void Awake()
    {
        DiscoverTargets();
        totalTargets = paintables.Count + gooGrowables.Count + splitables.Count + radioactives.Count;
        totalRadioactivetargets = radioactives.Count;
        Debug.Log($"Area {name} Total targets: {totalTargets} Radioactive targets: {totalRadioactivetargets}");
    }

    void Start()
    {
        if (initializeGpuPaintablesOnAwake)
        {
            foreach (GPUPaintableObject paintable in paintables)
            {
                if (paintable != null && !paintable.IsInitialized)
                    paintable.Initialize(128);
            }
        }

        Subscribe();
        SyncAlreadyCompletedAtStart();
        PushProgress();
        if(radioactivePostProcessVolume != null)
        radioactivePostProcessVolume.gameObject.SetActive(true);
        RefreshJobIndicator();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void OnPlayerEnteredArea()
    {
        playerInsideArea = true;
        RefreshProgressAndPushUi();
        RefreshRadioactivesHud();

        if (visualBorder != null && !jobCompleted)
            visualBorder.SetActive(true);

        RefreshJobIndicator();
    }

    void OnPlayerExitedArea()
    {
        playerInsideArea = false;
        Managers.UI.ShowRadioactivesProgress(false);

        if (visualBorder != null)
            visualBorder.SetActive(false);

        RefreshJobIndicator();
    }

    public void SetJobTargetActive(bool active)
    {
        jobTargetActive = active;
        if (active)
            jobCompleted = false;

        RefreshJobIndicator();
        areaTrigger.gameObject.SetActive(true);
    }

    public void SetJobCompleted()
    {
        jobTargetActive = false;
        jobCompleted = true;

        if (indicator != null)
            indicator.SetActive(false);

        if (visualBorder != null)
            visualBorder.SetActive(false);
    }

    public void RefreshJobIndicator()
    {
        if (indicator == null || jobCompleted)
            return;

        bool showIndicator = jobTargetActive && !playerInsideArea && NormalizedProgress < 1f;
        indicator.SetActive(showIndicator);
    }

    void DiscoverTargets()
    {
        paintables.Clear();
        gooGrowables.Clear();
        splitables.Clear();
        radioactives.Clear();

        foreach (GPUPaintableObject paintable in GetComponentsInChildren<GPUPaintableObject>())
        {
            if (paintable != null && paintable.CountsTowardAreaProgress)
                paintables.Add(paintable);
        }

        gooGrowables.AddRange(GetComponentsInChildren<GooHitGrowable>());

        var splittables = GetComponentsInChildren<SplitableObject>();
        foreach (SplitableObject s in splittables)
        {
            if (s.IsRadioactive)
                radioactives.Add(s);
            else
                splitables.Add(s);
        }
    }

    void Subscribe()
    {
        if (subscribed)
            return;

        subscribed = true;

        foreach (GPUPaintableObject paintable in paintables)
        {
            if (paintable == null)
                continue;

            if (paintable.UseContinuousProgress)
                paintable.OnProgress += OnPaintableProgressChanged;
            else
                paintable.OnCleaned += OnPaintableCleaned;
        }

        foreach (GooHitGrowable g in gooGrowables)
        {
            if (g != null)
                g.OnFullyGrownCompleted += OnGooFullyGrown;
        }

        foreach (SplitableObject s in splitables)
        {
            if (s != null)
                s.OnDestroyed.AddListener(OnSplitDestroyed);
        }

        foreach (SplitableObject s in radioactives)
        {
            if (s != null)
                s.OnDestroyed.AddListener(OnRadioactiveDestroyed);
        }

        areaTrigger.OnPlayerEnter += OnPlayerEnteredArea;
        areaTrigger.OnPlayerExit += OnPlayerExitedArea;
    }

    void Unsubscribe()
    {
        if (!subscribed)
            return;

        subscribed = false;

        foreach (GPUPaintableObject paintable in paintables)
        {
            if (paintable == null)
                continue;

            paintable.OnProgress -= OnPaintableProgressChanged;
            paintable.OnCleaned -= OnPaintableCleaned;
        }

        foreach (GooHitGrowable g in gooGrowables)
        {
            if (g != null)
                g.OnFullyGrownCompleted -= OnGooFullyGrown;
        }

        foreach (SplitableObject s in splitables)
        {
            if (s != null)
                s.OnDestroyed.RemoveListener(OnSplitDestroyed);
        }

        foreach (SplitableObject s in radioactives)
        {
            if (s != null)
                s.OnDestroyed.RemoveListener(OnRadioactiveDestroyed);
        }

        areaTrigger.OnPlayerEnter -= OnPlayerEnteredArea;
        areaTrigger.OnPlayerExit -= OnPlayerExitedArea;
    }

    void SyncAlreadyCompletedAtStart()
    {
        foreach (GooHitGrowable gooGrowable in gooGrowables)
        {
            if (gooGrowable != null && gooGrowable.IsFullyGrown)
                completedTargets++;
        }
    }

    void OnPaintableProgressChanged()
    {
        PushProgress();
    }

    void OnPaintableCleaned()
    {
        PushProgress();
    }

    void OnGooFullyGrown()
    {
        completedTargets++;
        PushProgress();
    }

    void OnSplitDestroyed()
    {
        completedTargets++;
        PushProgress();
    }

    void OnRadioactiveDestroyed()
    {
        completedTargets++;
        completedRadioactivetargets++;
        Debug.Log($"Radioactive destroyed: {completedRadioactivetargets} / {totalRadioactivetargets}");
        PushProgress();

        if (completedRadioactivetargets == totalRadioactivetargets)
        {
            if(radioactivePostProcessVolume != null)
            radioactivePostProcessVolume.gameObject.SetActive(false);
            Managers.UI.ShowInfoText("Air Cleared");
            Managers.UI.ShowRadioactivesProgress(false);
        }
    }

    public void RefreshProgressAndPushUi()
    {
        PushProgress();
    }

    public void CollectIncompletePaintables(List<GPUPaintableObject> results)
    {
        foreach (GPUPaintableObject paintable in paintables)
        {
            if (paintable != null && !paintable.isClean)
                results.Add(paintable);
        }
    }

    public void CompleteAllRemainingTargets()
    {
        foreach (GPUPaintableObject paintableObject in paintables)
        {
            if (paintableObject == null || paintableObject.isClean)
                continue;

            if (!paintableObject.IsInitialized)
                paintableObject.Initialize(128);

            paintableObject.SetClean();
        }

        foreach (GooHitGrowable gooGrowable in gooGrowables)
        {
            if (gooGrowable != null && !gooGrowable.IsFullyGrown)
                gooGrowable.DebugSetFullyGrown();
        }

        foreach (SplitableObject splitableObject in splitables)
        {
            if (splitableObject != null)
                splitableObject.DebugDestroyNow();
        }

        foreach (SplitableObject radioactiveObject in radioactives)
        {
            if (radioactiveObject != null)
                radioactiveObject.DebugDestroyNow();
        }
    }

    public void DebugCleanFixedPercent()
    {
        List<System.Action> completionActions = CollectIncompleteTargetActions();
        int incompleteTargetsCount = completionActions.Count;
        if (incompleteTargetsCount == 0)
            return;

        float cleanFraction = debugCleanPercent / 100f;
        int targetsToClean = Mathf.CeilToInt(totalTargets * cleanFraction);
        targetsToClean = Mathf.Clamp(targetsToClean, 0, incompleteTargetsCount);

        for (int index = completionActions.Count - 1; index > 0; index--)
        {
            int randomIndex = Random.Range(0, index + 1);
            (completionActions[index], completionActions[randomIndex]) = (completionActions[randomIndex], completionActions[index]);
        }

        for (int index = 0; index < targetsToClean; index++)
            completionActions[index].Invoke();
    }

    List<System.Action> CollectIncompleteTargetActions()
    {
        List<System.Action> completionActions = new();

        foreach (GPUPaintableObject paintableObject in paintables)
        {
            if (paintableObject != null && !paintableObject.isClean)
            {
                if (!paintableObject.IsInitialized)
                    paintableObject.Initialize(128);

                completionActions.Add(paintableObject.SetClean);
            }
        }

        foreach (GooHitGrowable gooGrowable in gooGrowables)
        {
            if (gooGrowable != null && !gooGrowable.IsFullyGrown)
                completionActions.Add(gooGrowable.DebugSetFullyGrown);
        }

        foreach (SplitableObject splitableObject in splitables)
        {
            if (splitableObject != null)
                completionActions.Add(splitableObject.DebugDestroyNow);
        }

        foreach (SplitableObject radioactiveObject in radioactives)
        {
            if (radioactiveObject != null)
                completionActions.Add(radioactiveObject.DebugDestroyNow);
        }

        return completionActions;
    }

    void RefreshRadioactivesHud()
    {
        bool showRadioactives = totalRadioactivetargets > 0
            && completedRadioactivetargets < totalRadioactivetargets;
        Managers.UI.ShowRadioactivesProgress(showRadioactives);
    }

    void PushProgress()
    {
        float progressSum = completedTargets;

        foreach (GPUPaintableObject paintable in paintables)
        {
            if (paintable != null)
                progressSum += paintable.GetProgressContribution();
        }

        NormalizedProgress = totalTargets > 0 ? progressSum / totalTargets : 1f;

        OnAreaProgressChanged?.Invoke(NormalizedProgress);

        if (playerInsideArea && totalRadioactivetargets > 0)
            Managers.UI.SetRadioactivesProgressBarPercent((1 - completedRadioactivetargets / (float)totalRadioactivetargets) * 100f);

        RefreshRadioactivesHud();
        RefreshJobIndicator();
    }
}
