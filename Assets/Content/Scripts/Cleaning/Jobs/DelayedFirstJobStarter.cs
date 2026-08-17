using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DelayedFirstJobStarter : MonoBehaviour
{
    [SerializeField] Job firstJob;

    void Start()
    {
        if (firstJob == null)
            firstJob = GetFirstChildJob();

        if (firstJob == null)
        {
            Debug.LogWarning($"{nameof(DelayedFirstJobStarter)} on {name}: no {nameof(firstJob)} assigned and none found on direct child objects.", this);
            return;
        }

        StartFirstJobAsync(destroyCancellationToken).Forget();
    }

    async UniTaskVoid StartFirstJobAsync(CancellationToken cancellationToken)
    {
        if (IntroSequenceController.Instance != null)
            await IntroSequenceController.Instance.WaitUntilIntroReadyAsync(cancellationToken);

        if (Managers.Jobs == null)
        {
            Debug.LogError($"{nameof(DelayedFirstJobStarter)} on {name}: {nameof(Managers.Jobs)} is not available.", this);
            return;
        }

        JobPresentation presentation = firstJob.Presentation;
        await NpcMoveRunner.RunAsync(presentation.movesBeforeIntro);
        cancellationToken.ThrowIfCancellationRequested();

        Managers.Jobs.MarkPreIntroMovesRan(firstJob);

        if (firstJob.Speaker != null)
            Managers.Speech.SetDialogueClient(firstJob.Speaker);

        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);
        Managers.Jobs.StartJobChain(firstJob);
    }

    Job GetFirstChildJob()
    {
        for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
        {
            Job job = transform.GetChild(childIndex).GetComponentInChildren<Job>(true);
            if (job != null)
                return job;
        }

        return null;
    }
}
