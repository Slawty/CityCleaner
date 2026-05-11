using UnityEngine;
using UnityEngine.UI;
public class GooMachine : MonoBehaviour
{
    public float spinSpeed = 720f;
    public float washingDuration = 8f;
    public WashingMachineTrigger pooplingTrigger;
    public PressButton startButton;
    public Transform drum;
    public GameObject cleanlingPrefab;
    public Image fillImage;
    public Transform cleanlingSpawnPoint;
    [Tooltip("World-space target the stored poopling moves toward over the wash cycle (e.g. bottom of drum).")]
    public Transform pooplingSinkTarget;
    Poopling storedPoopling;
    bool isWashing = false;
    float washingTimer = 0f;
    float accumulatedRotation = 0f;
    Vector3 pooplingSinkStartLocal;
    Vector3 pooplingSinkEndLocal;
    bool pooplingSinkActive;

    Quaternion startRotation;

    void Start()
    {
        pooplingTrigger.OnPooplingStored += OnPooplingStored;
        pooplingTrigger.OnPooplingPickedUp += OnPooplingPickedUp;
        startButton.OnButtonPressed += OnStartButtonPressed;
        startButton.SetState(PressButton.ButtonState.Unavailable);
    }

    void OnDestroy()
    {
        pooplingTrigger.OnPooplingStored -= OnPooplingStored;
        pooplingTrigger.OnPooplingPickedUp -= OnPooplingPickedUp;
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

        if (pooplingSinkActive && storedPoopling != null)
        {
            float sinkT = 1f - washingTimer / washingDuration;
            storedPoopling.transform.localPosition = Vector3.Lerp(pooplingSinkStartLocal, pooplingSinkEndLocal, Mathf.Clamp01(sinkT));
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

    void OnPooplingStored(Poopling poopling)
    {
        storedPoopling = poopling;
        startButton.SetState(PressButton.ButtonState.Available);
    }

    void OnPooplingPickedUp()
    {
        storedPoopling = null;
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

        pooplingSinkActive = storedPoopling != null
            && pooplingSinkTarget != null
            && storedPoopling.transform.parent != null;
        if (pooplingSinkActive)
        {
            Transform poopParent = storedPoopling.transform.parent;
            pooplingSinkStartLocal = storedPoopling.transform.localPosition;
            pooplingSinkEndLocal = poopParent.InverseTransformPoint(pooplingSinkTarget.position);
        }
    }

    void EndWashing()
    {
        isWashing = false;
        GameObject cleanling = Instantiate(cleanlingPrefab, cleanlingSpawnPoint.position, cleanlingSpawnPoint.rotation);
        Destroy(storedPoopling.gameObject);
        storedPoopling = null;
        startButton.SetState(PressButton.ButtonState.Unavailable);
    }
}
