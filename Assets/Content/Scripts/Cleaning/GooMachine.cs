using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GooMachine : MonoBehaviour
{
    public float spinSpeed = 720f;
    public float washingDuration = 8f;
    [FormerlySerializedAs("pooplingTrigger")]
    public WashingMachineTrigger dirtlingTrigger;
    public PressButton startButton;
    public Transform drum;
    public GameObject cleanlingPrefab;
    public Image fillImage;
    public Transform cleanlingSpawnPoint;
    [Tooltip("World-space target the stored dirtling moves toward over the wash cycle (e.g. bottom of drum).")]
    [FormerlySerializedAs("pooplingSinkTarget")]
    public Transform dirtlingSinkTarget;
    Dirtling storedDirtling;
    bool isWashing = false;
    float washingTimer = 0f;
    float accumulatedRotation = 0f;
    Vector3 dirtlingSinkStartLocal;
    Vector3 dirtlingSinkEndLocal;
    bool dirtlingSinkActive;

    Quaternion startRotation;

    void Start()
    {
        dirtlingTrigger.OnDirtlingStored += OnDirtlingStored;
        dirtlingTrigger.OnDirtlingReleased += OnDirtlingReleased;
        startButton.OnButtonPressed += OnStartButtonPressed;
        startButton.SetState(PressButton.ButtonState.Unavailable);
    }

    void OnDestroy()
    {
        dirtlingTrigger.OnDirtlingStored -= OnDirtlingStored;
        dirtlingTrigger.OnDirtlingReleased -= OnDirtlingReleased;
        startButton.OnButtonPressed -= OnStartButtonPressed;
    }

    void Update()
    {
        if (!isWashing)
        {
            return;
        }

        float deltaRotation = spinSpeed * Time.deltaTime;

        drum.Rotate(Vector3.up * deltaRotation);

        accumulatedRotation += deltaRotation;
        washingTimer -= Time.deltaTime;

        if (dirtlingSinkActive && storedDirtling != null)
        {
            float sinkT = 1f - washingTimer / washingDuration;
            storedDirtling.transform.localPosition = Vector3.Lerp(dirtlingSinkStartLocal, dirtlingSinkEndLocal, Mathf.Clamp01(sinkT));
        }

        fillImage.fillAmount = 1 - washingTimer / washingDuration;

        if (washingTimer <= 0f)
        {
            float remainder = accumulatedRotation % 360f;

            if (remainder > 0f)
            {
                drum.Rotate(Vector3.up * (360f - remainder));
            }

            drum.rotation = startRotation;
            EndWashing();
            isWashing = false;
        }
    }

    void OnDirtlingStored(Dirtling dirtling)
    {
        storedDirtling = dirtling;
        startButton.SetState(PressButton.ButtonState.Available);
    }

    void OnDirtlingReleased()
    {
        storedDirtling = null;
    }

    void OnStartButtonPressed()
    {
        if (startButton.CurrentState == PressButton.ButtonState.Available)
        {
            StartWashing();
        }
    }

    void StartWashing()
    {
        Debug.Log("Start washing");
        startButton.SetState(PressButton.ButtonState.InUse);
        startRotation = drum.rotation;
        accumulatedRotation = 0f;
        washingTimer = washingDuration;
        isWashing = true;

        dirtlingSinkActive = storedDirtling != null
            && dirtlingSinkTarget != null
            && storedDirtling.transform.parent != null;
        if (dirtlingSinkActive)
        {
            Transform dirtParent = storedDirtling.transform.parent;
            dirtlingSinkStartLocal = storedDirtling.transform.localPosition;
            dirtlingSinkEndLocal = dirtParent.InverseTransformPoint(dirtlingSinkTarget.position);
        }
    }

    void EndWashing()
    {
        isWashing = false;
        GameObject cleanling = Instantiate(cleanlingPrefab, cleanlingSpawnPoint.position, cleanlingSpawnPoint.rotation);
        Destroy(storedDirtling.gameObject);
        storedDirtling = null;
        startButton.SetState(PressButton.ButtonState.Unavailable);
    }
}
