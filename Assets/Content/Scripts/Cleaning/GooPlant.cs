using UnityEngine;
using UnityEngine.Serialization;

public class GooPlant : MonoBehaviour
{
    [Header("References")]
    [FormerlySerializedAs("pooplingTrigger")]
    [SerializeField] WashingMachineTrigger dirtlingTrigger;
    [SerializeField] Transform spinTarget;
    [SerializeField] Transform goolingSpawnPoint;
    [SerializeField] GameObject goolingPrefab;

    [Header("Conversion")]
    [SerializeField] float conversionDuration = 3f;
    [SerializeField] float spinSpeed = 540f;
    [SerializeField] float startSpinDistanceThreshold = 0.05f;

    Dirtling storedDirtling;
    bool isConverting;
    float conversionTimer;

    void Start()
    {
        dirtlingTrigger.OnDirtlingStored += OnDirtlingStored;
        dirtlingTrigger.OnDirtlingReleased += OnDirtlingReleased;
        dirtlingTrigger.EnableCollider(true);
    }

    void OnDestroy()
    {
        if (dirtlingTrigger == null)
            return;

        dirtlingTrigger.OnDirtlingStored -= OnDirtlingStored;
        dirtlingTrigger.OnDirtlingReleased -= OnDirtlingReleased;
    }

    void Update()
    {
        if (!isConverting || storedDirtling == null)
            return;

        if (!HasReachedSpinTarget())
            return;

        storedDirtling.transform.Rotate(Vector3.up * (spinSpeed * Time.deltaTime), Space.World);
        conversionTimer -= Time.deltaTime;

        if (conversionTimer <= 0f)
            FinishConversion();
    }

    void OnDirtlingStored(Dirtling dirtling)
    {
        if (isConverting)
            return;

        storedDirtling = dirtling;
        storedDirtling.SetBodyColliderEnabled(false);
        conversionTimer = conversionDuration;
        isConverting = true;
    }

    void OnDirtlingReleased()
    {
        isConverting = false;
        storedDirtling = null;
    }

    bool HasReachedSpinTarget()
    {
        if (spinTarget == null)
            return true;

        return Vector3.SqrMagnitude(storedDirtling.transform.position - spinTarget.position)
            <= startSpinDistanceThreshold * startSpinDistanceThreshold;
    }

    void FinishConversion()
    {
        if (storedDirtling == null)
        {
            isConverting = false;
            return;
        }

        Instantiate(goolingPrefab, goolingSpawnPoint.position, goolingSpawnPoint.rotation);
        Destroy(storedDirtling.gameObject);
        storedDirtling = null;
        isConverting = false;
        dirtlingTrigger.EnableCollider(true);
    }
}
