using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeVP
{
    public class InputManager_ArcadeVP : MonoBehaviour
    {
        public ArcadeVehicleController arcadeVehicleController;

        [Header("Input Actions")]
        public InputActionReference moveAction; // Vector2
        public InputActionReference jumpAction; // Button
        public InputActionReference exitAction; // Button

        [HideInInspector] public float Horizontal;
        [HideInInspector] public float Vertical;
        [HideInInspector] public float Jump;

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            exitAction.action.Enable();
            exitAction.action.performed += ExitCarButtonPressed;

        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
            exitAction.action.Disable();
            exitAction.action.performed -= ExitCarButtonPressed;
        }

        private void Update()
        {
            Vector2 move = moveAction.action.ReadValue<Vector2>();

            Horizontal = move.x;
            Vertical = move.y;
            Jump = jumpAction.action.ReadValue<float>();

            arcadeVehicleController.ProvideInputs(Horizontal, Vertical, Jump);
        }

        void ExitCarButtonPressed(InputAction.CallbackContext context)
        {
            GetComponent<VehicleActivator>().ExitCar();
        }
    }
}
