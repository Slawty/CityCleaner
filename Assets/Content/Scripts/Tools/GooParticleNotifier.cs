using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

public class GooParticleNotifier : MonoBehaviour
{
    public UnityAction<Vector3, GameObject> OnGooHit;

    [Header("Audio")]
    [SerializeField] EventReference gooPopEvent;

    ParticleSystem ps;
    GooGunTool gooGun;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    public float EmissionRateOverTime => ps.emission.rateOverTime.constant;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    public void Bind(GooGunTool tool)
    {
        gooGun = tool;
    }

    void OnParticleCollision(GameObject other)
    {
        Vector3 hitPoint = other.transform.position;
        int collisionCount = ps.GetCollisionEvents(other, collisionEvents);
        if (collisionCount > 0)
            hitPoint = collisionEvents[0].intersection;

        if (collisionCount > 0 && gooGun != null)
        {
            DirtNest nest = other.GetComponent<DirtNest>();
            if (nest == null)
                nest = other.GetComponentInParent<DirtNest>();

            if (nest != null)
                nest.ApplyGooDamage(gooGun.GooDamagePerParticle * collisionCount);
        }

        PlayGooPop(hitPoint);

        OnGooHit?.Invoke(hitPoint, other);

        IGooHitReceiver[] receivers = other.GetComponents<IGooHitReceiver>();
        foreach (IGooHitReceiver receiver in receivers)
            receiver.OnGooHit(hitPoint, gameObject);

        DirtlingGoo gooOnCollider = other.GetComponent<DirtlingGoo>();
        if (gooOnCollider == null)
        {
            DirtlingGoo gooInParents = other.GetComponentInParent<DirtlingGoo>();
            if (gooInParents != null)
                gooInParents.OnGooHit(hitPoint, gameObject);
        }
    }

    void PlayGooPop(Vector3 hitPoint)
    {
        if (gooPopEvent.IsNull)
            throw new System.InvalidOperationException("Goo pop FMOD event is not assigned on GooParticleNotifier.");

        RuntimeManager.PlayOneShot(gooPopEvent, hitPoint);
    }
}
