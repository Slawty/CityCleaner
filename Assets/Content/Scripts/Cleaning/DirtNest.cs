using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Local corruption anchor for a neighborhood.
/// Vulnerability is set explicitly via <see cref="SetVulnerable"/>.
/// </summary>
public class DirtNest : MonoBehaviour, IGooHitReceiver
{
    [SerializeField] GameObject shieldObject;
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

    [Header("Events")]
    public UnityEvent OnBecameVulnerable;
    public UnityEvent OnAreaFreed;
    public UnityEvent<float> OnHealthNormalizedChanged;

    float health;
    float nextPooplingSpawnTime;
    int alivePooplings;
    bool freed;
    bool isSpawning;
    readonly List<Poopling> pooplingsSpawned = new();

    public bool IsVulnerable { get; private set; }
    public bool IsFreed => freed;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => health;

    void Awake()
    {
        health = maxHealth;

        if (dirtArea == null && autoFindDirtAreaInParents)
            dirtArea = GetComponentInParent<DirtArea>();

        if (VulnerableAtStart)
            SetVulnerable();

        SyncHealthBar();
    }

    void Start()
    {
        dirtArea.OnAreaProgressChanged.AddListener(OnAreaProgressChanged);
        StartSpawning();
    }

    void OnAreaProgressChanged(float progress)
    {
        if (dirtArea.NormalizedProgress >= vulnerabilityCleanFraction)
        {
            SetVulnerable();
            dirtArea.OnAreaProgressChanged.RemoveListener(OnAreaProgressChanged);
        }
    }

    void Update()
    {
        if (!isSpawning)
            return;

        if (Time.time < nextPooplingSpawnTime)
            return;

        SpawnPoopling();
        ScheduleNextPooplingSpawn();
    }

    public void SetVulnerable()
    {
        if (IsVulnerable)
            return;

        IsVulnerable = true;
        shieldObject.SetActive(false);
        OnBecameVulnerable?.Invoke();
        Managers.UI.ShowInfoText("Poop Nest Vulnerable");
        StopSpawning();
    }

    void ScheduleNextPooplingSpawn()
    {
        float gap = Random.Range(pooplingSpawnMinInterval, pooplingSpawnMaxInterval);
        nextPooplingSpawnTime = Time.time + gap;
    }

    public void StartSpawning()
    {
        if (isSpawning)
            return;

        isSpawning = true;
        ScheduleNextPooplingSpawn();
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    void SpawnPoopling()
    {
        Vector2 offset = Random.insideUnitCircle * pooplingSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, 0f, offset.y);

        GameObject instance = Instantiate(pooplingPrefab, spawnPos, Quaternion.identity);
        var poopling = instance.GetComponent<Poopling>();
        alivePooplings++;
        pooplingsSpawned.Add(poopling);
        poopling.SetWanderCenter(transform.position);
        poopling.OnConsumed += OnPooplingDestroyed;

        if (alivePooplings >= maxAlivePooplings)
            StopSpawning();
    }

    void OnPooplingDestroyed()
    {
        alivePooplings = Mathf.Max(0, alivePooplings - 1);
        StartSpawning();
    }

    void OnDestroy()
    {
        foreach (Poopling p in pooplingsSpawned)
        {
            if (p != null)
                p.OnConsumed -= OnPooplingDestroyed;
        }

        pooplingsSpawned.Clear();
    }

    /// <summary>Continuous laser damage (expects damage per second; applies delta internally).</summary>
    public void ApplyLaserDamage(float damagePerSecond)
    {
        ApplyDamage(damagePerSecond * Time.deltaTime);
    }

    /// <summary>Water sprayer: pass the same strength GPUPainter uses per frame (cleanSpeed * Time.deltaTime).</summary>
    public void ApplyWaterDamage(float painterStrengthThisFrame)
    {
        ApplyDamage(painterStrengthThisFrame * waterDamageMultiplier);
    }

    public void OnGooHit(Vector3 hitPoint, GameObject source)
    {
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
        StopSpawning();
        OnAreaFreed?.Invoke();

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Destroy(gameObject);
        Managers.UI.ShowInfoText("Area Cleaned");
    }

    void SyncHealthBar()
    {
        float normalized = Mathf.Clamp01(health / Mathf.Max(maxHealth, 1e-5f));
        healthBar.SetNormalizedFill(normalized);
    }
}
