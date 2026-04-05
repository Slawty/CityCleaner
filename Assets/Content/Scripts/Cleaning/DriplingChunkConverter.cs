using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

public class DriplingChunkConverter : MonoBehaviour
{
    public UnityAction<PickupInteractable> OnChunkCollected;
    public UnityAction OnAllChunksCollected;
    [Header("Settings")]
    [SerializeField] private int RequiredChunkAmount = 3;
    [SerializeField] private Transform depositTarget;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float objectScale = 0.525f;
    int chunksConsumed;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PickupInteractable pickup = other.GetComponent<PickupInteractable>();

        if (pickup == null)
            return;

        // pickup.Release();
        pickup.EnablePhysics(false);
        MoveToTargetAndDestroy(pickup);
    }

    private void MoveToTargetAndDestroy(PickupInteractable pickup)
    {
        GameObject obj = pickup.gameObject;
        // Kill existing tweens on object
        obj.transform.DOKill();

        // Move and rotate simultaneously
        Sequence seq = DOTween.Sequence();

        seq.Join(obj.transform.DOMove(depositTarget.position, moveDuration).SetEase(Ease.InOutSine));
        seq.Join(obj.transform.DORotateQuaternion(depositTarget.rotation, moveDuration).SetEase(Ease.InOutSine));
        seq.Join(obj.transform.DOScale(objectScale, moveDuration).SetEase(Ease.InOutSine));

        seq.OnComplete(() =>
        {
            OnChunkLanded(pickup);
        });
    }

    void OnChunkLanded(PickupInteractable pickup)
    {
        chunksConsumed++;

        if (chunksConsumed >= RequiredChunkAmount)
            OnAllChunksCollected?.Invoke();
        else
            OnChunkCollected?.Invoke(pickup);

        Destroy(pickup.gameObject);
    }
}
