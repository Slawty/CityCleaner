using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Root for a neighborhood: counts discrete objectives (GPU cleanables, goo growables, splitables) and drives the HUD while the player is inside an <see cref="AreaTrigger"/>.
/// Progress advances only when a whole objective completes — no continuous rescans.
/// Assumes each completion signal fires at most once per objective.
/// </summary>
public class DirtArea : MonoBehaviour
{
    [Header("GPU dirt")]
    [SerializeField] bool initializeGpuPaintablesOnAwake = true;

    [Header("Events")]
    public UnityEvent<float> OnAreaProgressChanged;

    List<GPUPaintableObject> paintables = new();
    List<GooHitGrowable> gooGrowables = new();
    List<SplitableObject> splitables = new();

    int totalTargets;
    int completedTargets;

    bool drivingCleanUi;
    bool subscribed;

    public float NormalizedProgress { get; private set; }

    public bool DrivingCleanUi => drivingCleanUi;

    void Awake()
    {
        DiscoverTargets();
        CountTargets();

        if (initializeGpuPaintablesOnAwake)
        {
            foreach (GPUPaintableObject p in paintables)
            {
                if (p != null && !p.IsInitialized)
                    p.Initialize(128);
            }
        }
    }

    void Start()
    {
        Subscribe();
        SyncAlreadyCompletedAtStart();
        PushProgress(forcePush: true);
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void DiscoverTargets()
    {
        paintables.Clear();
        gooGrowables.Clear();
        splitables.Clear();

        paintables.AddRange(GetComponentsInChildren<GPUPaintableObject>(true));
        gooGrowables.AddRange(GetComponentsInChildren<GooHitGrowable>(true));
        splitables.AddRange(GetComponentsInChildren<SplitableObject>(true));
    }

    void CountTargets()
    {
        totalTargets = 0;

        foreach (GPUPaintableObject p in paintables)
        {
            if (p != null)
                totalTargets++;
        }

        foreach (GooHitGrowable g in gooGrowables)
        {
            if (g != null)
                totalTargets++;
        }

        foreach (SplitableObject s in splitables)
        {
            if (s != null)
                totalTargets++;
        }

        Debug.Log($"Area {name} Total targets: {totalTargets}");
    }

    void Subscribe()
    {
        if (subscribed)
            return;

        subscribed = true;

        foreach (GPUPaintableObject p in paintables)
        {
            if (p != null)
                p.OnCleaned += OnPaintableCleaned;
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
    }

    void Unsubscribe()
    {
        if (!subscribed)
            return;

        subscribed = false;

        foreach (GPUPaintableObject p in paintables)
        {
            if (p != null)
                p.OnCleaned -= OnPaintableCleaned;
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
    }

    void SyncAlreadyCompletedAtStart()
    {
        foreach (GPUPaintableObject p in paintables)
        {
            if (p != null && p.isClean)
                completedTargets++;
        }

        foreach (GooHitGrowable g in gooGrowables)
        {
            if (g != null && g.IsFullyGrown)
                completedTargets++;
        }
    }

    void OnPaintableCleaned()
    {
        completedTargets++;
        PushProgress(forcePush: false);
    }

    void OnGooFullyGrown()
    {
        completedTargets++;
        PushProgress(forcePush: false);
    }

    void OnSplitDestroyed()
    {
        completedTargets++;
        PushProgress(forcePush: false);
    }

    public void SetDrivingCleanUi(bool value)
    {
        drivingCleanUi = value;
    }

    public void RefreshProgressAndPushUi()
    {
        PushProgress(forcePush: true);
    }

    void PushProgress(bool forcePush)
    {
        NormalizedProgress = totalTargets > 0 ? (float)completedTargets / totalTargets : 1f;

        OnAreaProgressChanged?.Invoke(NormalizedProgress);

        if (drivingCleanUi || forcePush)
            Managers.UI.SetZoneCleanProgressBarPercent(NormalizedProgress * 100f);
    }
}
