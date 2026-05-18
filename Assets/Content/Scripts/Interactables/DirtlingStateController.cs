using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Dirtling))]
[RequireComponent(typeof(DirtlingWander))]
[RequireComponent(typeof(NpcNavMovement))]
[RequireComponent(typeof(NavMeshAgent))]
public class DirtlingStateController : MonoBehaviour
{
    public DirtlingState CurrentState { get; private set; }
    public bool WanderingEnabled { get; private set; } = true;

    public Dirtling Dirtling { get; private set; }
    public NpcNavMovement Movement { get; private set; }
    public NavMeshAgent NavAgent { get; private set; }

    DirtlingWander wander;

    void Awake()
    {
        Dirtling = GetComponent<Dirtling>();
        Movement = GetComponent<NpcNavMovement>();
        NavAgent = GetComponent<NavMeshAgent>();
        wander = GetComponent<DirtlingWander>();
    }

    void Start()
    {
        wander.Initialize();
        WanderingEnabled = wander.InitialWanderingEnabled;
        EnterState(DirtlingState.Wandering);
    }

    public void EnterState(DirtlingState state)
    {
        CurrentState = state;

        switch (state)
        {
            case DirtlingState.Wandering:
                wander.enabled = true;
                if (WanderingEnabled)
                    wander.BeginWandering();
                break;
            default:
                wander.enabled = false;
                wander.StopWandering();
                Movement.Stop();
                break;
        }
    }

    public void SetWanderingEnabled(bool value)
    {
        WanderingEnabled = value;

        if (!value)
        {
            wander.StopWandering();
            if (CurrentState == DirtlingState.Wandering)
                Movement.Stop();
            return;
        }

        if (CurrentState == DirtlingState.Wandering && NavAgent.enabled)
            wander.BeginWandering();
    }
}
