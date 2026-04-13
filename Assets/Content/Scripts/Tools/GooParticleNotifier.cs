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
        if (other.TryGetComponent(out GrowableObject growableObject))
        {
            Debug.Log($"Goo hit {growableObject.name}");
            growableObject.HitByGoo();
        }
    }
}
