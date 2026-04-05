using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;

    private CharacterController controller;
    private Vector3 velocity;
    private bool jumpRequested;
    private bool wasGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
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
        // Only accept jump input if grounded
        if (controller.isGrounded)
        {
            jumpRequested = true;
        }
    }

    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (controller.isGrounded && jumpRequested)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequested = false;
        }

        velocity.y += gravity * Time.deltaTime;

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        float speed = sprintAction.action.IsPressed() ? sprintSpeed : moveSpeed;

        controller.Move(move * speed * Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);
    }

}
