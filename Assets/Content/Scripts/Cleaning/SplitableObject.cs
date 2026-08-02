using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class SplitableObject : MonoBehaviour
{
    public float Health = 100;
    public UnityEvent OnDestroyed;
    public bool IsRadioactive;
    [SerializeField] private float hitScaleStrength = 0.1f;
    [SerializeField] private float hitScaleFrequency = 40f;
    [SerializeField] float maxFlashPerSeond = 0.1f;

    [SerializeField] AnimationCurve scaleCurve;
    public Transform ForceDirection;
    public string Prompt => "Mine Chunk";
    private Vector3 baseScale;
    private float hitTimer;
    private float coinChance = 0.25f;
    float flashTimeCounter;
    Material flashMaterial;
    bool isDestroyed;


    void Start()
    {
        baseScale = transform.localScale;
        flashMaterial = GetComponent<MeshRenderer>().material;
    }

    // public void Interact(GameObject interactor)
    // {
    //     Split();
    // }

    void Split()
    {
        var rb = GetComponent<Rigidbody>();
        transform.localScale = transform.localScale * 0.75f;
        rb.isKinematic = false;
        rb.AddForce(ForceDirection.forward * 50f, ForceMode.Impulse);

        // Random spin
        float spin = Random.Range(3f, 8f);
        Vector3 randomAxis = Random.onUnitSphere;
        rb.AddTorque(randomAxis * spin, ForceMode.Impulse);
        gameObject.AddComponent<PickupInteractable>();
        OnDestroyed?.Invoke();
        Destroy(this);
        // GetComponent<Collider>().enabled = false;
        // EnableColliderAsync().Forget();
    }

    public void UpdateLaserHit(float damagePerSecond)
    {
        if (Health <= 0f || isDestroyed)
            return;

        Health -= damagePerSecond * Time.deltaTime;
        // Debug.Log("Health: " + Health);

        hitTimer += Time.deltaTime * hitScaleFrequency;
        float curveTime = hitTimer % 1f;

        float scaleOffset = scaleCurve.Evaluate(curveTime) * hitScaleStrength;
        transform.localScale = baseScale * (1f + scaleOffset);
        // flashTimeCounter += Time.deltaTime;
        // if (flashTimeCounter > maxFlashPerSeond)
        // {
        //     flashMaterial.SetFloat("_FlashStartTime", Time.time);
        //     flashTimeCounter = 0f;
        // }

        if (Health <= 0)
        {
            DestroyAndReward();
        }
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
        int amount = Random.Range(2, 5);
        Vector3 playerDir = (Managers.Player.transform.position - transform.position).normalized;
        playerDir.y = 1f;
        float velocity = Random.Range(1f, 1.5f);
        playerDir *= velocity;
        Managers.Spawning.SpawnTempChunks(amount, transform.position, playerDir, spawnDelay: 0f).Forget();

        if (Random.value <= coinChance)
            Managers.Spawning.SpawnCoins(1, transform.position).Forget();

        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }

    async UniTask EnableColliderAsync()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f));
        GetComponent<Collider>().enabled = true;
    }
}
