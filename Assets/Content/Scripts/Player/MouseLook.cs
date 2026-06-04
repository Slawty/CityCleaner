using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 0.1f; // lower because deltaTime is removed
    public Transform playerBody;
    public Transform cameraTransform;
    public InputActionReference lookAction;

    public float maxDelta = 100f; // clamp extreme spikes

    float xRotation;
    bool pointerMode;
    int discardLookFrames;

    public Vector2 LookDelta { get; private set; }

    void OnEnable()
    {
        lookAction.action.Enable();
        ResetLook();
    }

    void OnDisable()
    {
        lookAction.action.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (pointerMode)
        {
            LookDelta = Vector2.zero;
            return;
        }

        if (discardLookFrames > 0)
        {
            discardLookFrames--;
            lookAction.action.ReadValue<Vector2>();
            LookDelta = Vector2.zero;
            return;
        }

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        // Clamp extreme spikes (debug freezes, editor stalls, etc.)
        lookInput.x = Mathf.Clamp(lookInput.x, -maxDelta, maxDelta);
        lookInput.y = Mathf.Clamp(lookInput.y, -maxDelta, maxDelta);

        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;
        LookDelta = new Vector2(mouseX, mouseY);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void ResetLook(float pitch = 0f)
    {
        xRotation = pitch;
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void SetPointerMode(bool pointerVisible)
    {
        if (pointerMode == pointerVisible)
            return;

        pointerMode = pointerVisible;
        LookDelta = Vector2.zero;

        if (pointerVisible)
        {
            lookAction.action.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lookAction.action.Enable();
        discardLookFrames = 2;
    }
}
