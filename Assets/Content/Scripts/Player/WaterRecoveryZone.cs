using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterRecoveryZone : MonoBehaviour
{
    void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerFallRecovery recovery = other.GetComponentInParent<PlayerFallRecovery>();
        if (recovery == null)
            return;

        recovery.RecoverFromWater();
    }
}
