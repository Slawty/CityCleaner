using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Dripling : MonoBehaviour, IVacuumable
{
    public enum ConsumableType { Water, Goo };
    public ConsumableType Type;
    public UnityAction OnConsumed;
    public string prompt;
    public string Prompt => prompt;
    [SerializeField] float totalAmmo = 100f;
    [SerializeField] float shrinkDuration = 3f;
    [SerializeField] float minScale = 0.2f;
    [SerializeField] float startScale = 1f;
    [SerializeField] DriplingChunkConverter chunkConverter;
    [SerializeField] GameObject goolingPrefab;
    NpcNavMovement mover;
    NpcWander wander;
    bool isRefilling;
    float elapsed;

    void Awake()
    {
        mover = GetComponent<NpcNavMovement>();
        wander = GetComponent<NpcWander>();
    }

    void Start()
    {
        transform.localScale = Vector3.one * startScale;
        if (chunkConverter != null)
            chunkConverter.OnAllChunksCollected += OnAllChunksCollected;

        if (wander != null)
            return;

        mover.Follow(Managers.Player.transform);
    }

    void OnDestroy()
    {
        OnConsumed?.Invoke();
        if (chunkConverter != null)
            chunkConverter.OnAllChunksCollected -= OnAllChunksCollected;
    }

    void Update()
    {
        if (!isRefilling)
            return;

        float delta = Time.deltaTime;
        elapsed += delta;

        float ammoPerSecond = totalAmmo / shrinkDuration;
        if (Type == ConsumableType.Water)
            Managers.Tools.WaterSprayer.FillWaterAmount(ammoPerSecond * delta);
        else if (Type == ConsumableType.Goo)
            Managers.Tools.GooGun.FillAmmoAmount(ammoPerSecond * delta);

        float t = Mathf.Clamp01(elapsed / shrinkDuration);
        float scale = Mathf.Lerp(startScale, minScale, t);
        transform.localScale = Vector3.one * scale;

        if (wander != null)
            wander.SetWanderingEnabled(false);

        if (elapsed >= shrinkDuration)
            Destroy(gameObject);
    }

    public bool CanVacuum => true;

    public void VacuumStart()
    {
        isRefilling = true;
    }

    public void VacuumEnd()
    {
        isRefilling = false;

        if (wander != null)
            wander.SetWanderingEnabled(true);
    }

    void OnAllChunksCollected()
    {
        Dripling gooling = Instantiate(goolingPrefab, transform.position, transform.rotation).GetComponent<Dripling>();
        Destroy(gameObject);
    }
}
