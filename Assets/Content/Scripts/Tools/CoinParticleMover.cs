using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CoinParticleMover : MonoBehaviour
{
    struct SuctionState
    {
        public bool Committed;
    }

    public static UnityAction OnCoinCollected;

    public RessourceType Type;
    public ParticleSystem ps;
    public Transform vacuumPoint;
    [SerializeField] EventReference coinCollectEvent;
    [SerializeField] bool scaleSize = true;
    [SerializeField] float suctionStrength = 12f;
    [SerializeField] float minSuctionStrengthMultiplier = 0.25f;
    [SerializeField] float suctionFalloffDistance = 3f;
    [SerializeField] float commitHorizontalDistance = 0.5f;
    [SerializeField] float collectDistance = 0.3f;
    [SerializeField] float hopForce = 4f;
    [SerializeField] float hopFrequency = 8f;

    readonly Dictionary<uint, SuctionState> activeSuctions = new();
    readonly List<ParticleSystem.Particle> triggerParticles = new();
    readonly List<uint> finishedSeeds = new();
    readonly HashSet<uint> seenSeeds = new();
    ParticleSystem.Particle[] particles;
    float initialSize;

    void Awake()
    {
        if (ps == null)
            ps = GetComponent<ParticleSystem>();

        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void Start()
    {
        initialSize = ps.main.startSize.constant;
    }

    void OnParticleTrigger()
    {
        int count = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, triggerParticles);

        for (int index = 0; index < count; index++)
        {
            uint seed = triggerParticles[index].randomSeed;
            if (!activeSuctions.ContainsKey(seed))
                activeSuctions[seed] = new SuctionState();
        }
    }

    void LateUpdate()
    {
        if (activeSuctions.Count == 0)
            return;

        bool suctionActive = Managers.Tools.IsCoinSuctionActive;
        int count = ps.GetParticles(particles);
        bool changed = false;

        finishedSeeds.Clear();
        seenSeeds.Clear();

        for (int index = 0; index < count; index++)
        {
            ParticleSystem.Particle particle = particles[index];
            if (!activeSuctions.TryGetValue(particle.randomSeed, out SuctionState state))
                continue;

            seenSeeds.Add(particle.randomSeed);

            if (!suctionActive && !state.Committed)
            {
                particle.velocity = Vector3.zero;
                finishedSeeds.Add(particle.randomSeed);
                particles[index] = particle;
                changed = true;
                continue;
            }

            Vector3 offset = vacuumPoint.position - particle.position;
            float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
            float distance = offset.magnitude;
            bool liftPhase = horizontalDistance <= commitHorizontalDistance;

            if (liftPhase)
                state.Committed = true;

            if (liftPhase)
            {
                float strength = GetSuctionStrength(distance);
                Vector3 direction = distance > 0.0001f ? offset / distance : Vector3.zero;
                float hopPhase = particle.randomSeed * 0.001f;
                Vector3 hop = Vector3.up * hopForce * Mathf.Abs(Mathf.Sin(Time.time * hopFrequency + hopPhase));
                particle.velocity = direction * strength + hop;

                if (scaleSize)
                {
                    float scaleT = commitHorizontalDistance > 0.0001f
                        ? Mathf.Clamp01(distance / commitHorizontalDistance)
                        : 0f;
                    particle.startSize = Mathf.Lerp(initialSize * 0.15f, initialSize, scaleT);
                }
            }
            else
            {
                Vector3 flatDirection = new Vector3(offset.x, 0f, offset.z);
                float strength = GetSuctionStrength(horizontalDistance);
                particle.velocity = flatDirection.sqrMagnitude > 0.0001f
                    ? flatDirection.normalized * strength
                    : Vector3.zero;

                if (scaleSize)
                    particle.startSize = initialSize;
            }

            if (distance < collectDistance)
            {
                AddValue(1);
                particle.remainingLifetime = 0f;
                finishedSeeds.Add(particle.randomSeed);
            }
            else
                activeSuctions[particle.randomSeed] = state;

            particles[index] = particle;
            changed = true;
        }

        foreach (uint seed in activeSuctions.Keys)
        {
            if (!seenSeeds.Contains(seed))
                finishedSeeds.Add(seed);
        }

        foreach (uint seed in finishedSeeds)
            activeSuctions.Remove(seed);

        if (changed)
            ps.SetParticles(particles, count);
    }

    float GetSuctionStrength(float distance)
    {
        float falloffDistance = Mathf.Max(suctionFalloffDistance, collectDistance);
        float closeness = 1f - Mathf.Clamp01(distance / falloffDistance);
        return Mathf.Lerp(suctionStrength * minSuctionStrengthMultiplier, suctionStrength, closeness);
    }

    void AddValue(int amount)
    {
        if (Type == RessourceType.Coin)
        {
            Managers.Inventory.IncreaseCoins(amount);
            OnCoinCollected?.Invoke();
            PlayCoinCollectSound();
        }
        else if (Type == RessourceType.Poop)
            Managers.Inventory.IncreasePoop(amount);
        else if (Type == RessourceType.Dirt)
            Managers.Inventory.IncreaseDirt(amount);
    }

    void PlayCoinCollectSound()
    {
        if (coinCollectEvent.IsNull)
            throw new System.InvalidOperationException("Coin collect FMOD event is not assigned on CoinParticleMover.");

        RuntimeManager.PlayOneShotAttached(coinCollectEvent, vacuumPoint.gameObject);
    }
}
