using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Local corruption anchor for a neighborhood: invulnerable until <see cref="DirtArea"/> progress crosses the threshold
/// (or immediately if <see cref="VulnerableAtStart"/>), then takes damage from water, goo, and laser tools.
/// </summary>
public class DirtNest : MonoBehaviour, IGooHitReceiver
{
    public bool VulnerableAtStart;
    [Header("Area")]
    [SerializeField] DirtArea dirtArea;
    [SerializeField] bool autoFindDirtAreaInParents = true;
    [SerializeField][Range(0f, 1f)] float vulnerabilityCleanFraction = 0.9f;

    [Header("Combat")]
    [SerializeField] float maxHealth = 200f;
    [SerializeField] HealthBar healthBar;
    [Tooltip("Multiplies GPUPainter strength (cleanSpeed * deltaTime) when applying water damage.")]
    [SerializeField] float waterDamageMultiplier = 30f;
    [SerializeField] float gooDamagePerHit = 15f;

    [Header("Pooplings")]
    [SerializeField] GameObject pooplingPrefab;
    [SerializeField] float pooplingSpawnRadius = 4f;
    [SerializeField] float pooplingSpawnMinInterval = 10f;
    [SerializeField] float pooplingSpawnMaxInterval = 25f;
    [SerializeField] int maxAlivePooplings = 5;
    [Tooltip("If false, no pooplings spawn until the nest is vulnerable.")]
    [SerializeField] bool spawnPooplingsWhileInvulnerable = true;

    [Header("Events")]
    public UnityEvent OnBecameVulnerable;
    public UnityEvent OnAreaFreed;
    public UnityEvent<float> OnHealthNormalizedChanged;

    float health;
    float nextPooplingSpawnTime;
    int alivePooplings;
    bool wasVulnerable;
    bool freed;
    readonly List<Poopling> pooplingsSpawned = new();

    public bool IsVulnerable { get; private set; }
    public float AreaCleanFraction { get; private set; }
    public bool IsFreed => freed;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => health;

    void Awake()
    {
        health = maxHealth;

        if (dirtArea == null && autoFindDirtAreaInParents)
            dirtArea = GetComponentInParent<DirtArea>();

        ScheduleNextPooplingSpawn();
        RefreshAreaAndVulnerability();
        SyncHealthBar();
    }

    void Update()
    {
        if (freed)
            return;

        RefreshAreaAndVulnerability();

        if (IsVulnerable && !wasVulnerable)
            SetVulnerable();

        wasVulnerable = IsVulnerable;

        TrySpawnPooplingRoutine();
    }

    void RefreshAreaAndVulnerability()
    {
        AreaCleanFraction = dirtArea != null ? dirtArea.NormalizedProgress : 1f;
        IsVulnerable =
            VulnerableAtStart ||
            AreaCleanFraction >= vulnerabilityCleanFraction;
    }

    void SetVulnerable()
    {
        OnBecameVulnerable?.Invoke();
    }

    void TrySpawnPooplingRoutine()
    {
        if (freed)
            return;

        if (!spawnPooplingsWhileInvulnerable && !IsVulnerable)
            return;

        if (pooplingPrefab == null)
            return;

        if (alivePooplings >= maxAlivePooplings)
            return;

        if (Time.time < nextPooplingSpawnTime)
            return;

        SpawnPoopling();
        ScheduleNextPooplingSpawn();
    }

    void ScheduleNextPooplingSpawn()
    {
        float gap = Random.Range(pooplingSpawnMinInterval, pooplingSpawnMaxInterval);
        nextPooplingSpawnTime = Time.time + gap;
    }

    void SpawnPoopling()
    {
        Vector2 offset = Random.insideUnitCircle * pooplingSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, 0f, offset.y);

        GameObject instance = Instantiate(pooplingPrefab, spawnPos, Quaternion.identity);
        var poopling = instance.GetComponent<Poopling>();
        if (poopling != null)
        {
            alivePooplings++;
            pooplingsSpawned.Add(poopling);
            poopling.SetWanderCenter(transform.position);
            poopling.OnConsumed += OnPooplingConsumed;
        }
    }

    void OnPooplingConsumed()
    {
        alivePooplings = Mathf.Max(0, alivePooplings - 1);
    }

    void OnDestroy()
    {
        foreach (Poopling p in pooplingsSpawned)
        {
            if (p != null)
                p.OnConsumed -= OnPooplingConsumed;
        }

        pooplingsSpawned.Clear();
    }

    /// <summary>Continuous laser damage (expects damage per second; applies delta internally).</summary>
    public void ApplyLaserDamage(float damagePerSecond)
    {
        if (!IsVulnerable || freed)
            return;

        ApplyDamage(damagePerSecond * Time.deltaTime);
    }

    /// <summary>Water sprayer: pass the same strength GPUPainter uses per frame (cleanSpeed * Time.deltaTime).</summary>
    public void ApplyWaterDamage(float painterStrengthThisFrame)
    {
        if (!IsVulnerable || freed)
            return;

        ApplyDamage(painterStrengthThisFrame * waterDamageMultiplier);
    }

    public void OnGooHit(Vector3 hitPoint, GameObject source)
    {
        if (!IsVulnerable || freed)
            return;

        ApplyDamage(gooDamagePerHit);
    }

    void ApplyDamage(float amount)
    {
        if (amount <= 0f || freed)
            return;

        health -= amount;
        float normalized = Mathf.Clamp01(health / Mathf.Max(maxHealth, 1e-5f));
        OnHealthNormalizedChanged?.Invoke(normalized);
        SyncHealthBar();

        if (health <= 0f)
            FreeArea();
    }

    void FreeArea()
    {
        if (freed)
            return;

        freed = true;
        IsVulnerable = false;
        OnAreaFreed?.Invoke();

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    void SyncHealthBar()
    {
        if (healthBar == null)
            return;

        float normalized = Mathf.Clamp01(health / Mathf.Max(maxHealth, 1e-5f));
        healthBar.SetNormalizedFill(normalized);
    }
}
