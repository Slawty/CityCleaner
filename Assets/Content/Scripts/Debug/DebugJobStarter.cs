using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(1000)]
public class DebugJobStarter : MonoBehaviour
{
    [SerializeField] bool useDebugJobStart;
    [SerializeField] Job startAtJob;

    void Start()
    {
        if (!useDebugJobStart)
            return;

        if (startAtJob == null)
        {
            Debug.LogWarning($"{nameof(DebugJobStarter)} on {name}: {nameof(startAtJob)} is not assigned.", this);
            return;
        }

        if (Managers.Jobs == null)
        {
            Debug.LogError($"{nameof(DebugJobStarter)} on {name}: {nameof(Managers.Jobs)} is not available.", this);
            return;
        }

        List<Job> orderedJobs = CollectOrderedJobs();
        int startIndex = orderedJobs.IndexOf(startAtJob);
        if (startIndex < 0)
        {
            Debug.LogError($"{nameof(DebugJobStarter)} on {name}: {startAtJob.name} is not a direct child job under this Jobs root.", this);
            return;
        }

        for (int jobIndex = 0; jobIndex < startIndex; jobIndex++)
            DebugCompleteJob(orderedJobs[jobIndex]);

        Job previousJob = startIndex > 0 ? orderedJobs[startIndex - 1] : null;
        Managers.Jobs.DebugStartAtJob(startAtJob, previousJob);
    }

    List<Job> CollectOrderedJobs()
    {
        List<Job> orderedJobs = new List<Job>();

        for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
        {
            Job job = transform.GetChild(childIndex).GetComponentInChildren<Job>(true);
            if (job != null)
                orderedJobs.Add(job);
        }

        return orderedJobs;
    }

    static void DebugCompleteJob(Job job)
    {
        job.CompleteRemaining();
        job.MarkCompleted();
        job.RenameAsDone();
        job.OnTurnedIn();
        job.Presentation.onJobCompleted?.Invoke();
        BeamNpcsAfterOutro(job.Presentation);

        JobClient client = ResolveClient(job);
        if (client != null)
            client.SetState(JobClientState.TurnedIn);
    }

    static JobClient ResolveClient(Job job)
    {
        if (job.Speaker != null)
            return job.Speaker;

        return job.GetComponentInParent<JobClient>();
    }

    static void BeamNpcsAfterOutro(JobPresentation presentation)
    {
        if (presentation.movesAfterOutro == null)
            return;

        foreach (NpcMoveInstruction move in presentation.movesAfterOutro)
        {
            if (move == null || move.npc == null || move.destination == null)
                continue;

            if (move.stopWandering && move.npc.TryGetComponent(out NpcWander wander))
                wander.StopWandering();

            move.npc.Stop();

            if (move.npc.TryGetComponent(out NavMeshAgent agent) && agent.isOnNavMesh)
                agent.Warp(move.destination.position);
            else
                move.npc.transform.position = move.destination.position;

            move.npc.transform.rotation = move.destination.rotation;
        }
    }
}
