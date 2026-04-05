using UnityEngine;
using UnityEngine.Events;

public class Dripling : MonoBehaviour
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

    bool isRefilling;
    float elapsed;

    void Start()
    {
        transform.localScale = Vector3.one * startScale;
        if (chunkConverter != null)
            chunkConverter.OnAllChunksCollected += OnAllChunksCollected;
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

        // Give water evenly over the duration
        float ammoPerSecond = totalAmmo / shrinkDuration;
        if (Type == ConsumableType.Water)
            Managers.Tools.WaterSprayer.FillWaterAmount(ammoPerSecond * delta);
        else if (Type == ConsumableType.Goo)
            Managers.Tools.GooGun.FillAmmoAmount(ammoPerSecond * delta);

        // Scale based on normalized time
        float t = Mathf.Clamp01(elapsed / shrinkDuration);
        float scale = Mathf.Lerp(startScale, minScale, t);
        transform.localScale = Vector3.one * scale;

        if (elapsed >= shrinkDuration)
        {
            Destroy(gameObject);
        }
    }

    public void Interact(GameObject interactor)
    {
        isRefilling = true;
    }

    public void InteractCanceled(GameObject interactor)
    {
        isRefilling = false;
    }

    void OnAllChunksCollected()
    {
        Debug.Log("All chunks collected");
        Dripling gooling = Instantiate(goolingPrefab, transform.position, transform.rotation).GetComponent<Dripling>();
        Destroy(gameObject);
    }
}