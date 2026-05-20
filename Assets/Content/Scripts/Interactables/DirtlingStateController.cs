using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(Dirtling))]
[RequireComponent(typeof(DirtlingWander))]
[RequireComponent(typeof(DirtlingFlee))]
[RequireComponent(typeof(DirtlingVacuumCapture))]
[RequireComponent(typeof(DirtlingDizzyBar))]
[RequireComponent(typeof(DirtlingPhysicsBall))]
[RequireComponent(typeof(DirtlingGoo))]
[RequireComponent(typeof(NpcNavMovement))]
[RequireComponent(typeof(NavMeshAgent))]
public class DirtlingStateController : MonoBehaviour
{
    [Header("Laser")]
    [FormerlySerializedAs("waterDizzyPerSecond")]
    [SerializeField] float laserDizzyPerSecond = 0.35f;

    public DirtlingState CurrentState { get; private set; }
    public bool WanderingEnabled { get; private set; } = true;
    public float Dizzy { get; private set; }
    public bool IsDizzy => Dizzy >= 1f;

    public Dirtling Dirtling { get; private set; }
    public NpcNavMovement Movement { get; private set; }
    public NavMeshAgent NavAgent { get; private set; }

    DirtlingWander wander;
    DirtlingFlee flee;
    DirtlingDizzyBar dizzyBar;
    DirtlingPhysicsBall physicsBall;
    DirtlingGoo goo;

    void Awake()
    {
        Dirtling = GetComponent<Dirtling>();
        Movement = GetComponent<NpcNavMovement>();
        NavAgent = GetComponent<NavMeshAgent>();
        wander = GetComponent<DirtlingWander>();
        flee = GetComponent<DirtlingFlee>();
        dizzyBar = GetComponent<DirtlingDizzyBar>();
        physicsBall = GetComponent<DirtlingPhysicsBall>();
        goo = GetComponent<DirtlingGoo>();
    }

    void Start()
    {
        wander.Initialize();
        WanderingEnabled = wander.InitialWanderingEnabled;
        dizzyBar.SetDizzy(Dizzy);
        EnterState(DirtlingState.Wandering);
    }

    public void ApplyWater(Vector3 pushDirection, float forcePerSecond)
    {
        if (CurrentState == DirtlingState.Processed || CurrentState == DirtlingState.Vacuumed)
            return;

        if (CurrentState != DirtlingState.PhysicsBall)
            EnterState(DirtlingState.PhysicsBall);

        physicsBall.ApplyPush(pushDirection, forcePerSecond * Time.deltaTime);
    }

    public void OnGooApplied()
    {
        if (CurrentState == DirtlingState.Fleeing)
            OnFleeEnded();
    }

    public void ApplyLaser(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (CurrentState == DirtlingState.Processed || CurrentState == DirtlingState.Vacuumed)
            return;

        if (CurrentState == DirtlingState.PhysicsBall)
            return;

        float dizzyRate = laserDizzyPerSecond * goo.WaterDizzyMultiplier;

        if (IsDizzy)
        {
            ApplyLaserDizzyOnly(deltaTime, dizzyRate);
            return;
        }

        Dizzy = Mathf.Min(1f, Dizzy + dizzyRate * deltaTime);
        dizzyBar.SetDizzy(Dizzy);

        if (IsDizzy)
        {
            EnterState(DirtlingState.Dizzy);
            return;
        }

        if (goo.BlocksFlee)
            return;

        if (CurrentState == DirtlingState.Fleeing)
            flee.NotifyWaterHit();
        else
            EnterState(DirtlingState.Fleeing);
    }

    void ApplyLaserDizzyOnly(float deltaTime, float dizzyRate)
    {
        Dizzy = Mathf.Min(1f, Dizzy + dizzyRate * deltaTime);
        dizzyBar.SetDizzy(Dizzy);

        if (CurrentState != DirtlingState.Dizzy)
            EnterState(DirtlingState.Dizzy);
    }

    public void OnFleeEnded()
    {
        if (IsDizzy)
            EnterState(DirtlingState.Dizzy);
        else
            EnterState(DirtlingState.Wandering);
    }

    public void OnPhysicsBallRecovered()
    {
        OnFleeEnded();
    }

    public void EnterState(DirtlingState state)
    {
        CurrentState = state;

        flee.enabled = state == DirtlingState.Fleeing;

        switch (state)
        {
            case DirtlingState.Wandering:
                wander.enabled = true;
                if (WanderingEnabled)
                    wander.BeginWandering();
                break;
            case DirtlingState.Fleeing:
                wander.enabled = false;
                wander.StopWandering();
                Movement.Stop();
                break;
            case DirtlingState.Dizzy:
                wander.enabled = false;
                wander.StopWandering();
                Movement.Stop();
                break;
            case DirtlingState.PhysicsBall:
                wander.enabled = false;
                wander.StopWandering();
                physicsBall.BeginBall();
                break;
            case DirtlingState.Vacuumed:
            case DirtlingState.Processed:
                wander.enabled = false;
                wander.StopWandering();
                Movement.Stop();
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
