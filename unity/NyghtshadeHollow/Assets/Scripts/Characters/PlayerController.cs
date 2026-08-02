using UnityEngine;
using NyghtshadeHollow.Core;

namespace NyghtshadeHollow.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float WalkSpeed = 3f;
        public float RunSpeed = 6f;
        public float CrouchSpeed = 1.5f;
        public float Gravity = -9.81f;
        public float JumpHeight = 1.2f;

        [Header("Camera")]
        public Transform CameraRoot;
        public float MouseSensitivity = 2f;
        public float MaxLookAngle = 80f;

        [Header("Interaction")]
        public float InteractRange = 2.5f;
        public LayerMask InteractLayer;

        private CharacterController _cc;
        private Vector3 _velocity;
        private float _cameraPitch;
        private bool _isCrouching;
        private bool _isGrounded;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            HandleMovement();
            HandleCamera();
            HandleInteraction();
            HandleCrouch();
        }

        private void HandleMovement()
        {
            _isGrounded = _cc.isGrounded;
            if (_isGrounded && _velocity.y < 0) _velocity.y = -2f;

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            bool running = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;

            float speed = _isCrouching ? CrouchSpeed : running ? RunSpeed : WalkSpeed;
            Vector3 move = transform.right * x + transform.forward * z;
            _cc.Move(move * speed * Time.deltaTime);

            if (Input.GetButtonDown("Jump") && _isGrounded && !_isCrouching)
                _velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            _velocity.y += Gravity * Time.deltaTime;
            _cc.Move(_velocity * Time.deltaTime);
        }

        private void HandleCamera()
        {
            float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

            _cameraPitch = Mathf.Clamp(_cameraPitch - mouseY, -MaxLookAngle, MaxLookAngle);
            CameraRoot.localEulerAngles = Vector3.right * _cameraPitch;
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                _isCrouching = !_isCrouching;
                _cc.height = _isCrouching ? 1f : 2f;
                _cc.center = new Vector3(0, _isCrouching ? 0.5f : 1f, 0);
            }
        }

        private void HandleInteraction()
        {
            if (!Input.GetKeyDown(KeyCode.E)) return;

            Ray ray = new Ray(CameraRoot.position, CameraRoot.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, InteractRange, InteractLayer))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                interactable?.Interact(gameObject);
            }
        }
    }
}
