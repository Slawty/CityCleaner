using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Local corruption anchor for a neighborhood.
/// Vulnerability is set explicitly via <see cref="SetVulnerable"/>.
/// </summary>
public class DirtNest : MonoBehaviour
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

    [Header("Dirtlings")]
    [FormerlySerializedAs("pooplingPrefab")]
    [SerializeField] GameObject dirtlingPrefab;
    [FormerlySerializedAs("pooplingSpawnRadius")]
    [SerializeField] float dirtlingSpawnRadius = 4f;
    [FormerlySerializedAs("pooplingSpawnMinInterval")]
    [SerializeField] float dirtlingSpawnMinInterval = 10f;
    [FormerlySerializedAs("pooplingSpawnMaxInterval")]
    [SerializeField] float dirtlingSpawnMaxInterval = 25f;
    [FormerlySerializedAs("maxAlivePooplings")]
    [SerializeField] int maxAliveDirtlings = 5;

    [Header("Events")]
    public UnityEvent OnBecameVulnerable;
    public UnityEvent OnAreaFreed;
    public UnityEvent<float> OnHealthNormalizedChanged;

    float health;
    float nextDirtlingSpawnTime;
    int aliveDirtlings;
    bool freed;
    bool isSpawning;
    readonly List<Dirtling> dirtlingsSpawned = new();

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

        if (Time.time < nextDirtlingSpawnTime)
            return;

        SpawnDirtling();
        ScheduleNextDirtlingSpawn();
    }

    public void SetVulnerable()
    {
        if (IsVulnerable)
            return;

        IsVulnerable = true;
        shieldObject.SetActive(false);
        OnBecameVulnerable?.Invoke();
        healthBar.gameObject.SetActive(true);
        Managers.UI.ShowInfoText("Poop Nest Vulnerable");
        StopSpawning();
    }

    void ScheduleNextDirtlingSpawn()
    {
        float gap = Random.Range(dirtlingSpawnMinInterval, dirtlingSpawnMaxInterval);
        nextDirtlingSpawnTime = Time.time + gap;
    }

    public void StartSpawning()
    {
        if (isSpawning)
            return;

        isSpawning = true;
        ScheduleNextDirtlingSpawn();
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    void SpawnDirtling()
    {
        Vector2 offset = Random.insideUnitCircle * dirtlingSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, 0f, offset.y);

        GameObject instance = Instantiate(dirtlingPrefab, spawnPos, Quaternion.identity);
        var dirtling = instance.GetComponent<Dirtling>();
        aliveDirtlings++;
        dirtlingsSpawned.Add(dirtling);
        dirtling.SetWanderCenter(transform.position);
        dirtling.OnConsumed += OnDirtlingDestroyed;

        if (aliveDirtlings >= maxAliveDirtlings)
            StopSpawning();
    }

    void OnDirtlingDestroyed()
    {
        aliveDirtlings = Mathf.Max(0, aliveDirtlings - 1);
        StartSpawning();
    }

    void OnDestroy()
    {
        foreach (Dirtling dirtling in dirtlingsSpawned)
        {
            if (dirtling != null)
                dirtling.OnConsumed -= OnDirtlingDestroyed;
        }

        dirtlingsSpawned.Clear();
    }

    /// <summary>Continuous tool damage (laser, water); expects damage per second.</summary>
    public void ApplyDamageOverTime(float damagePerSecond)
    {
        ApplyDamage(damagePerSecond * Time.deltaTime);
    }

    /// <summary>Instant damage from goo particle collisions (already scaled by hit count).</summary>
    public void ApplyGooDamage(float amount)
    {
        ApplyDamage(amount);
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
