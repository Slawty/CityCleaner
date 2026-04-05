using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Collider))]
public class TrashContainer : MonoBehaviour
{
    public UnityAction<PickupInteractable> OnTrashCollected;
    [Header("Container Settings")]
    [SerializeField] private Transform depositTarget;
    [SerializeField] private Transform coinSpawnPoint;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float objectScale = 0.525f;

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
            OnTrashLanded(pickup);
        });
    }

    void OnTrashLanded(PickupInteractable pickup)
    {
        // if (pickup.StaticPickup != null)
        OnTrashCollected?.Invoke(pickup);

        Managers.Spawning.SpawnCoins(3, coinSpawnPoint.position, coinSpawnPoint.forward).Forget();
        Destroy(pickup.gameObject);
    }
}
