using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class NpcToolThrow : MonoBehaviour
{
    [SerializeField] NpcNavMovement npc;
    [SerializeField] Transform dropPosition;
    [SerializeField] ToolPickup pickupPrefab;
    [SerializeField] Vector3 throwOriginOffset = new(0f, 1.2f, 0.5f);
    [SerializeField] float turnPauseSeconds = 0.35f;
    [SerializeField] float throwArcHeight = 1.5f;
    [SerializeField] float throwDuration = 0.6f;

    bool hasThrown;

    public void ThrowTool()
    {
        if (hasThrown)
            return;

        ThrowToolAsync().Forget();
    }

    async UniTaskVoid ThrowToolAsync()
    {
        if (npc == null)
            throw new InvalidOperationException($"{nameof(NpcToolThrow)} on {name}: {nameof(npc)} is not assigned.");

        if (dropPosition == null)
            throw new InvalidOperationException($"{nameof(NpcToolThrow)} on {name}: {nameof(dropPosition)} is not assigned.");

        if (pickupPrefab == null)
            throw new InvalidOperationException($"{nameof(NpcToolThrow)} on {name}: {nameof(pickupPrefab)} is not assigned.");

        Transform npcTransform = npc.transform;
        JobClient jobClient = npc.GetComponent<JobClient>();
        if (jobClient == null)
            jobClient = npc.GetComponentInParent<JobClient>();

        Quaternion returnRotation = npcTransform.rotation;
        if (jobClient != null && jobClient.TryGetDialogueReturnRotation(out Quaternion savedRotation))
            returnRotation = savedRotation;

        try
        {
            await npc.FacePointAsync(dropPosition.position, destroyCancellationToken);

            if (turnPauseSeconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(turnPauseSeconds), cancellationToken: destroyCancellationToken);

            Vector3 spawnPosition = npcTransform.position + npcTransform.TransformDirection(throwOriginOffset);
            ToolPickup pickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
            pickup.ThrowTo(dropPosition.position, throwArcHeight, throwDuration);
            hasThrown = true;

            if (turnPauseSeconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(turnPauseSeconds * 0.5f), cancellationToken: destroyCancellationToken);

            await npc.FaceRotationAsync(returnRotation, destroyCancellationToken);
            jobClient?.ReleaseDialogueReturnRotation();
        }
        catch (OperationCanceledException)
        {
        }
    }
}
