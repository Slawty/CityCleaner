using UnityEngine;
using UnityEngine.UI;

public class WashingMachine : MonoBehaviour
{
    public float spinSpeed = 720f;
    public float washingDuration = 8f;
    public WashingMachineDoor door;
    public WashingMachineTrigger pooplingTrigger;
    public PressButton startButton;
    public Transform drum;
    public GameObject cleanlingPrefab;
    public Image fillImage;
    public Transform cleanlingSpawnPoint;
    Poopling storedPoopling;
    bool isWashing = false;
    float washingTimer = 0f;
    float accumulatedRotation = 0f;

    Quaternion startRotation;

    void Start()
    {
        door.OnDoorOpened += OnDoorOpened;
        door.OnDoorClosed += OnDoorClosed;
        pooplingTrigger.OnPooplingStored += OnPooplingStored;
        pooplingTrigger.OnPooplingPickedUp += OnPooplingPickedUp;
        startButton.OnButtonPressed += OnStartButtonPressed;
        startButton.SetState(PressButton.ButtonState.Unavailable);
    }

    void OnDestroy()
    {
        door.OnDoorOpened -= OnDoorOpened;
        door.OnDoorClosed -= OnDoorClosed;
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

    void OnDoorOpened()
    {
        pooplingTrigger.EnableCollider(true);
    }

    void OnDoorClosed()
    {
        pooplingTrigger.EnableCollider(false);

        if (storedPoopling != null)
        {
            startButton.SetState(PressButton.ButtonState.Available);
        }
    }

    void OnPooplingStored(Poopling poopling)
    {
        storedPoopling = poopling;
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
    }

    void EndWashing()
    {
        isWashing = false;
        GameObject cleanling = Instantiate(cleanlingPrefab, cleanlingSpawnPoint.position, cleanlingSpawnPoint.rotation);
        Destroy(storedPoopling.gameObject);
        storedPoopling = null;
        door.OpenDoor();
        startButton.SetState(PressButton.ButtonState.Unavailable);
    }
}