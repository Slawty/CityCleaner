using System;
using DG.Tweening;
using UnityEngine;

public class IntroCameraRigMover
{
    readonly Transform cameraRig;
    readonly Transform endPosition;
    readonly float holdDuration;
    readonly float moveDuration;
    readonly Ease moveEase;

    public IntroCameraRigMover(Transform cameraRig, Transform endPosition, float holdDuration, float moveDuration, Ease moveEase)
    {
        this.cameraRig = cameraRig;
        this.endPosition = endPosition;
        this.holdDuration = holdDuration;
        this.moveDuration = moveDuration;
        this.moveEase = moveEase;
    }

    public Sequence Play(Action onMoveComplete)
    {
        Sequence sequence = DOTween.Sequence();

        if (holdDuration > 0f)
            sequence.AppendInterval(holdDuration);

        sequence.Append(cameraRig.DOMove(endPosition.position, moveDuration).SetEase(moveEase));
        sequence.Join(cameraRig.DORotateQuaternion(endPosition.rotation, moveDuration).SetEase(moveEase));
        sequence.AppendCallback(() => onMoveComplete?.Invoke());

        return sequence;
    }
}
