using UnityEngine;

public class GooPlant : MonoBehaviour
{
    [Header("References")]
    [SerializeField] WashingMachineTrigger pooplingTrigger;
    [SerializeField] Transform spinTarget;
    [SerializeField] Transform goolingSpawnPoint;
    [SerializeField] GameObject goolingPrefab;

    [Header("Conversion")]
    [SerializeField] float conversionDuration = 3f;
    [SerializeField] float spinSpeed = 540f;
    [SerializeField] float startSpinDistanceThreshold = 0.05f;

    Poopling storedPoopling;
    bool isConverting;
    float conversionTimer;

    void Start()
    {
        pooplingTrigger.OnPooplingStored += OnPooplingStored;
        pooplingTrigger.OnPooplingPickedUp += OnPooplingPickedUp;
        pooplingTrigger.EnableCollider(true);
    }

    void OnDestroy()
    {
        if (pooplingTrigger == null)
            return;

        pooplingTrigger.OnPooplingStored -= OnPooplingStored;
        pooplingTrigger.OnPooplingPickedUp -= OnPooplingPickedUp;
    }

    void Update()
    {
        if (!isConverting || storedPoopling == null)
            return;

        if (!HasReachedSpinTarget())
            return;

        storedPoopling.transform.Rotate(Vector3.up * (spinSpeed * Time.deltaTime), Space.World);
        conversionTimer -= Time.deltaTime;

        if (conversionTimer <= 0f)
            FinishConversion();
    }

    void OnPooplingStored(Poopling poopling)
    {
        if (isConverting)
            return;

        storedPoopling = poopling;
        // Keep the poopling locked in place while conversion runs.
        storedPoopling.PickupInteractable.EnableCollider(false);
        conversionTimer = conversionDuration;
        isConverting = true;
    }

    void OnPooplingPickedUp()
    {
        isConverting = false;
        storedPoopling = null;
    }

    bool HasReachedSpinTarget()
    {
        if (spinTarget == null)
            return true;

        return Vector3.SqrMagnitude(storedPoopling.transform.position - spinTarget.position)
            <= startSpinDistanceThreshold * startSpinDistanceThreshold;
    }

    void FinishConversion()
    {
        if (storedPoopling == null)
        {
            isConverting = false;
            return;
        }

        Instantiate(goolingPrefab, goolingSpawnPoint.position, goolingSpawnPoint.rotation);
        Destroy(storedPoopling.gameObject);
        storedPoopling = null;
        isConverting = false;
        pooplingTrigger.EnableCollider(true);
    }
}
