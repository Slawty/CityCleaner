using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class GooParticleNotifier : MonoBehaviour
{
    public UnityAction<Vector3, GameObject> OnGooHit;
    ParticleSystem ps;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
        Vector3 hitPoint = other.transform.position;
        int collisionCount = ps.GetCollisionEvents(other, collisionEvents);
        if (collisionCount > 0)
            hitPoint = collisionEvents[0].intersection;

        OnGooHit?.Invoke(hitPoint, other);

        IGooHitReceiver[] receivers = other.GetComponents<IGooHitReceiver>();
        foreach (IGooHitReceiver receiver in receivers)
        {
            receiver.OnGooHit(hitPoint, gameObject);
        }
    }
}
