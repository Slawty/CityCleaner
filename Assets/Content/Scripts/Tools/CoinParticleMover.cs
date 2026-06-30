using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CoinParticleMover : MonoBehaviour
{
    public static UnityAction OnCoinCollected;

    public RessourceType Type;
    public ParticleSystem ps;
    public Transform vacuumPoint;
    public bool scaleSize = true;
    public float suctionStrength = 12f;
    public float hopForce = 4f;
    public float collectDistance = 0.3f;
    List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
    float initialSize;

    void Start()
    {
        initialSize = ps.main.startSize.constant;
    }

    void OnParticleTrigger()
    {
        int count = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);

        // Debug.Log($"OnParticleTrigger. Count: {count}");
        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = inside[i];

            Vector3 dir = vacuumPoint.position - p.position;
            float dist = dir.magnitude;

            dir.Normalize();

            if (scaleSize)
            {
                // scale coin while approaching vacuum
                p.startSize = Mathf.Lerp(initialSize * 0.15f, initialSize, dist / 2f);
            }

            // suction force toward vacuum
            Vector3 suction = dir * suctionStrength;

            // hopping motion
            // Vector3 hop = Vector3.up * hopForce * Mathf.Sin(Time.time * 8f);

            p.velocity = suction;

            // collect coin
            if (dist < collectDistance)
            {
                AddValue(1);
                p.remainingLifetime = 0f;
            }

            inside[i] = p;
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
    }

    void AddValue(int amount)
    {
        if (Type == RessourceType.Coin)
        {
            Managers.Inventory.IncreaseCoins(amount);
            OnCoinCollected?.Invoke();
        }
        else if (Type == RessourceType.Poop)
            Managers.Inventory.IncreasePoop(amount);
    }
}