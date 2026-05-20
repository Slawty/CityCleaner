using UnityEngine;

public class ToolsViewBob : MonoBehaviour
{
    [SerializeField] SimplePlayerMovement movement;
    [SerializeField] MouseLook mouseLook;
    [SerializeField] float bobAmount = 0.03f;
    [SerializeField] float swayAmount = 0.015f;
    [SerializeField] float tiltAmount = 2f;
    [SerializeField] float bobFrequency = 2f;
    [SerializeField] float speedThreshold = 0.1f;
    [SerializeField] float returnSpeed = 8f;
    [SerializeField] float sprintBobMultiplier = 1.25f;

    [Header("Jump / Land")]
    [SerializeField] float jumpKick = 0.025f;
    [SerializeField] float landingKick = 0.06f;
    [SerializeField] float kickSmoothSpeed = 12f;
    [SerializeField] float airBobAmount = 0.012f;
    [SerializeField] float airSwayAmount = 0.008f;
    [SerializeField] float airTiltAmount = 1f;
    [SerializeField] float airBobFrequency = 1.5f;
    [SerializeField] float airVelocityFollow = 0.003f;

    [Header("Mouse Sway")]
    [SerializeField] float mouseSwayPosition = 0.012f;
    [SerializeField] float mouseSwayTilt = 0.35f;
    [SerializeField] float mouseSwaySmooth = 14f;
    [SerializeField] float mouseSwayReturn = 10f;

    Vector3 defaultLocalPosition;
    Quaternion defaultLocalRotation;
    CharacterController controller;
    float bobPhase;
    float airPhase;
    float verticalKick;
    float verticalKickTarget;
    bool wasGrounded;
    Vector2 mouseSwayPositionOffset;
    Vector2 mouseSwayTiltOffset;

    void Awake()
    {
        defaultLocalPosition = transform.localPosition;
        defaultLocalRotation = transform.localRotation;
    }

    void Start()
    {
        if (movement == null)
            movement = GetComponentInParent<SimplePlayerMovement>();

        if (mouseLook == null)
            mouseLook = GetComponentInParent<MouseLook>();

        controller = movement.GetComponent<CharacterController>();
        wasGrounded = controller.isGrounded;
    }

    void LateUpdate()
    {
        bool grounded = controller.isGrounded;

        if (grounded && !wasGrounded)
        {
            verticalKick -= landingKick;
            verticalKickTarget = 0f;
        }
        else if (!grounded && wasGrounded)
            verticalKickTarget = jumpKick;

        wasGrounded = grounded;

        float kickStep = kickSmoothSpeed * Time.deltaTime;
        if (grounded)
            verticalKick = Mathf.Lerp(verticalKick, 0f, kickStep);
        else
        {
            verticalKick = Mathf.Lerp(verticalKick, verticalKickTarget, kickStep);
            verticalKickTarget = Mathf.Lerp(verticalKickTarget, 0f, kickStep);
        }

        UpdateMouseSway();

        float horizontalSpeed = movement.HorizontalSpeed;
        bool walkBobActive = grounded && horizontalSpeed > speedThreshold;

        Vector3 positionOffset = new Vector3(mouseSwayPositionOffset.x, verticalKick + mouseSwayPositionOffset.y, 0f);
        Vector3 eulerOffset = new Vector3(mouseSwayTiltOffset.y, 0f, mouseSwayTiltOffset.x);

        if (walkBobActive)
        {
            float amountScale = movement.IsSprinting ? sprintBobMultiplier : 1f;
            bobPhase += horizontalSpeed * bobFrequency * Time.deltaTime;

            positionOffset.y += Mathf.Sin(bobPhase) * bobAmount * amountScale;
            positionOffset.x += Mathf.Cos(bobPhase * 0.5f) * swayAmount * amountScale;
            eulerOffset.z += Mathf.Sin(bobPhase) * tiltAmount * amountScale;
        }
        else if (!grounded)
        {
            float airMoveScale = Mathf.Clamp01(horizontalSpeed / movement.moveSpeed);
            airPhase += (airBobFrequency + airBobFrequency * airMoveScale) * Time.deltaTime;

            positionOffset.y += Mathf.Sin(airPhase) * airBobAmount;
            positionOffset.x += Mathf.Cos(airPhase * 0.5f) * airSwayAmount;
            eulerOffset.z += Mathf.Sin(airPhase) * airTiltAmount;
            positionOffset.y += movement.VerticalVelocity * airVelocityFollow;
            bobPhase = Mathf.Lerp(bobPhase, 0f, returnSpeed * Time.deltaTime);
        }
        else
        {
            bobPhase = Mathf.Lerp(bobPhase, 0f, returnSpeed * Time.deltaTime);
            airPhase = Mathf.Lerp(airPhase, 0f, returnSpeed * Time.deltaTime);
        }

        transform.localPosition = defaultLocalPosition + positionOffset;
        transform.localRotation = defaultLocalRotation * Quaternion.Euler(eulerOffset);
    }

    void UpdateMouseSway()
    {
        Vector2 lookDelta = mouseLook.LookDelta;
        bool hasLookInput = lookDelta.sqrMagnitude > 0.0001f;
        float smooth = mouseSwaySmooth * Time.deltaTime;
        float returnStep = mouseSwayReturn * Time.deltaTime;

        if (hasLookInput)
        {
            Vector2 targetPosition = new Vector2(-lookDelta.x, -lookDelta.y) * mouseSwayPosition;
            Vector2 targetTilt = new Vector2(-lookDelta.x, lookDelta.y) * mouseSwayTilt;
            mouseSwayPositionOffset = Vector2.Lerp(mouseSwayPositionOffset, targetPosition, smooth);
            mouseSwayTiltOffset = Vector2.Lerp(mouseSwayTiltOffset, targetTilt, smooth);
            return;
        }

        mouseSwayPositionOffset = Vector2.Lerp(mouseSwayPositionOffset, Vector2.zero, returnStep);
        mouseSwayTiltOffset = Vector2.Lerp(mouseSwayTiltOffset, Vector2.zero, returnStep);
    }
}
