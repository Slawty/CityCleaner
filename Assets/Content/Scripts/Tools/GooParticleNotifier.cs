using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class GooParticleNotifier : MonoBehaviour
{
    public UnityAction<Vector3, GameObject> OnGooHit;
    ParticleSystem ps;
    ParticleSystem.Particle[] particles;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void OnParticleCollision(GameObject other)
    {
        int numEvents = ps.GetCollisionEvents(other, collisionEvents);

        if (numEvents == 0)
            return;

        Vector3 hitPosition = collisionEvents[0].intersection;
        OnGooHit?.Invoke(hitPosition, other);
    }
}
