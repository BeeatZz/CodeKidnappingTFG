using UnityEngine;

namespace Game
{
    // First person movement: WASD, mouse look, shift to run, space to jump
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float lookSensitivity = 2f;
        public float jumpHeight = 1.1f;
        public float gravity = -20f;

        public bool InputEnabled { get; private set; }

        private CharacterController controller;
        private float pitch;
        private float verticalSpeed;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            SetInputEnabled(true);
        }

        // Turning input off also frees the mouse, which the terminal needs
        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }

        private void Update()
        {
            Vector3 move = Vector3.zero;

            if (InputEnabled)
            {
                Look();

                float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
                move = (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * speed;
            }

            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;

            if (InputEnabled && controller.isGrounded && Input.GetButtonDown("Jump"))
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalSpeed += gravity * Time.deltaTime;
            move.y = verticalSpeed;

            controller.Move(move * Time.deltaTime);
        }

        private void Look()
        {
            transform.Rotate(0f, Input.GetAxis("Mouse X") * lookSensitivity, 0f);

            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
