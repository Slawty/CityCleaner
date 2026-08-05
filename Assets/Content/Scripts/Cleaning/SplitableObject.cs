using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using FMODUnity;

[RequireComponent(typeof(MeshRenderer))]
public class SplitableObject : MonoBehaviour
{
    static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

    const float EmissionStrengthEpsilon = 0.0001f;
    const string DefaultDestroyEventPath = "event:/Tools/Laser/Laser_Pop";
    const string DefaultWobbleHitEventPath = "event:/Tools/Laser/Laser_Hit";

    public float Health = 100;
    public UnityEvent OnDestroyed;
    public bool IsRadioactive;
    public Transform ForceDirection;
    public string Prompt => "Mine Chunk";

    [Header("Hit Wobble")]
    [SerializeField] float hitScaleStrength = 0.1f;
    [SerializeField] float hitScaleFrequency = 40f;
    [SerializeField] AnimationCurve scaleCurve;

    [Header("Heat")]
    [SerializeField] float maxEmissionStrength = 1f;
    [SerializeField] AnimationCurve emissionCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 0.18f),
        new Keyframe(1f, 1f));
    [SerializeField] float healthRegenPercentPerSecond = 0.1f;

    [Header("Audio")]
    [SerializeField] EventReference destroyEvent;
    [SerializeField] EventReference wobbleHitEvent;

    [Header("Destroy VFX")]
    [SerializeField] float destroyVfxScale = 1f;

    [SerializeField] MeshRenderer meshRenderer;

    MaterialPropertyBlock propertyBlock;
    Vector3 baseScale;
    float maxHealth;
    float hitTimer;
    float lastAppliedEmissionStrength = -1f;
    float coinChance = 0.25f;
    int lastLaserHitFrame = -1;
    bool isDestroyed;

    MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

    void Awake()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        baseScale = transform.localScale;
        maxHealth = Health;
        TryUpdateEmissionFromHealth();
    }

    void Update()
    {
        if (isDestroyed || Health >= maxHealth)
            return;

        if (Time.frameCount == lastLaserHitFrame)
            return;

        float previousHealth = Health;
        Health = Mathf.Min(maxHealth, Health + maxHealth * healthRegenPercentPerSecond * Time.deltaTime);
        transform.localScale = baseScale;

        if (!Mathf.Approximately(Health, previousHealth))
            TryUpdateEmissionFromHealth();
    }

    void TryUpdateEmissionFromHealth()
    {
        float heat01 = 1f - (Health / maxHealth);
        float shapedHeat = emissionCurve.Evaluate(Mathf.Clamp01(heat01));
        float strength = shapedHeat * maxEmissionStrength;

        if (Mathf.Abs(strength - lastAppliedEmissionStrength) <= EmissionStrengthEpsilon)
            return;

        lastAppliedEmissionStrength = strength;
        meshRenderer.GetPropertyBlock(PropertyBlock);
        PropertyBlock.SetFloat(EmissionStrengthId, strength);
        meshRenderer.SetPropertyBlock(PropertyBlock);
    }

    public void UpdateLaserHit(float damagePerSecond)
    {
        if (Health <= 0f || isDestroyed)
            return;

        lastLaserHitFrame = Time.frameCount;
        Health -= damagePerSecond * Time.deltaTime;
        TryUpdateEmissionFromHealth();

        float heat01 = 1f - (Health / maxHealth);
        float wobbleIntensity = Mathf.Clamp01(heat01);

        if (wobbleIntensity > 0f)
        {
            int previousWobbleCycle = Mathf.FloorToInt(hitTimer);
            hitTimer += Time.deltaTime * hitScaleFrequency * wobbleIntensity;

            if (Mathf.FloorToInt(hitTimer) > previousWobbleCycle)
                PlayWobbleHitSound();

            float curveTime = hitTimer % 1f;
            float scaleOffset = scaleCurve.Evaluate(curveTime) * hitScaleStrength * wobbleIntensity;
            transform.localScale = baseScale * (1f + scaleOffset);
        }
        else
            transform.localScale = baseScale;

        if (Health <= 0f)
            DestroyAndReward();
    }

    public void DebugDestroyNow()
    {
        if (isDestroyed)
            return;

        Health = 0f;
        DestroyAndReward();
    }

    void DestroyAndReward()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;
        Bounds destroyBounds = meshRenderer.bounds;
        Vector3 destroyCenter = destroyBounds.center;

        PlayDestroySound(destroyCenter);
        Managers.Spawning.SpawnSplitableDestroyVfx(destroyBounds, destroyVfxScale);

        int amount = Random.Range(2, 5);
        Vector3 playerDir = (Managers.Player.transform.position - destroyCenter).normalized;
        playerDir.y = 1f;
        float velocity = Random.Range(1f, 1.5f);
        playerDir *= velocity;
        Managers.Spawning.SpawnTempChunks(amount, destroyCenter, playerDir, spawnDelay: 0f).Forget();

        if (Random.value <= coinChance)
            Managers.Spawning.SpawnCoins(1, destroyCenter).Forget();

        meshRenderer.SetPropertyBlock(null);
        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }

    void PlayDestroySound(Vector3 position)
    {
        if (!destroyEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(destroyEvent, position);
            return;
        }

        RuntimeManager.PlayOneShot(DefaultDestroyEventPath, position);
    }

    void PlayWobbleHitSound()
    {
        if (!wobbleHitEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(wobbleHitEvent, transform.position);
            return;
        }

        RuntimeManager.PlayOneShot(DefaultWobbleHitEventPath, transform.position);
    }

    async UniTask EnableColliderAsync()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), cancellationToken: destroyCancellationToken);
        GetComponent<Collider>().enabled = true;
    }
}
