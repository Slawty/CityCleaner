using FMODUnity;
using UnityEngine;

public class PlayerSounds : MonoBehaviour
{
    [SerializeField] SimplePlayerMovement movement;
    [SerializeField] EventReference footstepEvent;
    [SerializeField] EventReference jumpStartEvent;
    [SerializeField] EventReference jumpLandEvent;
    [SerializeField] float strideDistance = 1.6f;
    [SerializeField] float minMoveSpeed = 0.1f;
    [SerializeField] float minLandSpeed = -2f;

    CharacterController controller;
    float distanceSinceStep;

    void Awake()
    {
        if (movement == null)
            movement = GetComponent<SimplePlayerMovement>();

        controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        movement.OnJumpStarted += PlayJumpStart;
        movement.OnLanded += PlayJumpLand;
    }

    void OnDisable()
    {
        movement.OnJumpStarted -= PlayJumpStart;
        movement.OnLanded -= PlayJumpLand;
    }

    void Update()
    {
        if (movement.IsClimbing || !controller.isGrounded)
        {
            distanceSinceStep = 0f;
            return;
        }

        if (movement.HorizontalSpeed < minMoveSpeed)
            return;

        distanceSinceStep += movement.HorizontalSpeed * Time.deltaTime;
        if (distanceSinceStep < strideDistance)
            return;

        distanceSinceStep = 0f;
        PlayFootstep();
    }

    void PlayFootstep()
    {
        if (footstepEvent.IsNull)
            throw new System.InvalidOperationException("Footstep FMOD event is not assigned on PlayerSounds.");

        RuntimeManager.PlayOneShotAttached(footstepEvent, gameObject);
    }

    void PlayJumpStart()
    {
        if (jumpStartEvent.IsNull)
            throw new System.InvalidOperationException("Jump start FMOD event is not assigned on PlayerSounds.");

        distanceSinceStep = 0f;
        RuntimeManager.PlayOneShotAttached(jumpStartEvent, gameObject);
    }

    void PlayJumpLand(float verticalVelocity)
    {
        if (verticalVelocity > minLandSpeed)
            return;

        if (jumpLandEvent.IsNull)
            throw new System.InvalidOperationException("Jump land FMOD event is not assigned on PlayerSounds.");

        distanceSinceStep = 0f;
        RuntimeManager.PlayOneShotAttached(jumpLandEvent, gameObject);
    }
}
