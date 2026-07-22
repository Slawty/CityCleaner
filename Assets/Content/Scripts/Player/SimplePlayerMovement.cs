using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float ladderClimbSpeed = 3f;

    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;

    private CharacterController controller;
    private Vector3 velocity;
    private bool jumpRequested;
    private Ladder currentLadder;
    private bool wasGrounded;

    public float HorizontalSpeed { get; private set; }
    public float VerticalVelocity => velocity.y;
    public bool IsSprinting { get; private set; }
    public bool IsClimbing => currentLadder != null;

    public event Action OnJumpStarted;
    public event Action<float> OnLanded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        wasGrounded = controller.isGrounded;
    }

    void OnEnable()
    {
        moveAction.action.Enable();
        sprintAction.action.Enable();

        jumpAction.action.Enable();
        jumpAction.action.performed += OnJumpPerformed;
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        sprintAction.action.Disable();

        jumpAction.action.performed -= OnJumpPerformed;
        jumpAction.action.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (controller.isGrounded)
            jumpRequested = true;
    }

    void Update()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        if (currentLadder != null)
        {
            MoveOnLadder(input);
            wasGrounded = controller.isGrounded;
            return;
        }

        bool groundedBeforeMove = controller.isGrounded;

        if (groundedBeforeMove && velocity.y < 0)
            velocity.y = -2f;

        if (groundedBeforeMove && jumpRequested)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequested = false;
            OnJumpStarted?.Invoke();
        }

        velocity.y += gravity * Time.deltaTime;
        float verticalVelocityForLand = velocity.y;

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        IsSprinting = sprintAction.action.IsPressed();
        float speed = IsSprinting ? sprintSpeed : moveSpeed;
        HorizontalSpeed = move.magnitude * speed;

        controller.Move(move * speed * Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded && !wasGrounded)
            OnLanded?.Invoke(verticalVelocityForLand);

        wasGrounded = controller.isGrounded;
    }

    void MoveOnLadder(Vector2 input)
    {
        jumpRequested = false;
        velocity = Vector3.zero;
        IsSprinting = false;

        Vector3 sideMove = transform.right * input.x * moveSpeed;
        Vector3 climbMove = Vector3.up * input.y * ladderClimbSpeed;

        HorizontalSpeed = Mathf.Abs(input.x) * moveSpeed;
        controller.Move((sideMove + climbMove) * Time.deltaTime);
    }

    public void EnterLadder(Ladder ladder)
    {
        currentLadder = ladder;
        velocity = Vector3.zero;
        jumpRequested = false;
    }

    public void ExitLadder(Ladder ladder)
    {
        if (currentLadder != ladder)
            return;

        currentLadder = null;
    }
}
