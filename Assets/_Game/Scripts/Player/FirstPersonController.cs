using UnityEngine;

namespace BlackHorizon.Player
{
    /// <summary>
    /// First-person movement and mouse-look controller.
    /// Handles walk / run / crouch / jump / gravity / ladders and smooths
    /// headbob + FOV transitions through the attached CameraRig.
    /// Uses the legacy Input Manager (stable on Built-in pipeline).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerCamera))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float runSpeed = 7.0f;
        [SerializeField] private float crouchSpeed = 2.4f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float airControl = 2f;
        [SerializeField] private float jumpForce = 5.5f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float stepOffset = 0.3f;

        [Header("Crouch")]
        [SerializeField] private float crouchHeight = 0.8f;
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchTransitionSpeed = 12f;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float lookSmoothing = 10f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private float cameraHeight = 1.65f;

        [Header("Audio")]
        [SerializeField] private FootstepPlayer footstepPlayer;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController _controller;
        private PlayerCamera _cameraRig;
        private Transform _transform;

        private Vector3 _velocity;
        private float _targetPitch;
        private float _targetYaw;
        private float _currentPitch;
        private float _currentYaw;

        private float _verticalVelocity;
        private float _currentHeight;
        private bool _isGrounded;
        private bool _isCrouching;
        private bool _isRunning;

        private MoveState _state = MoveState.Walk;
        private float _moveAmount;

        public enum MoveState { Idle, Walk, Run, Crouch }

        public bool IsGrounded => _isGrounded;
        public bool IsCrouching => _isCrouching;
        public bool IsRunning => _isRunning && !_isCrouching;
        public MoveState CurrentState => _state;
        public Vector3 HorizontalVelocity => new Vector3(_velocity.x, 0f, _velocity.z);
        public bool HasMoveInput { get; private set; }
        public Transform CameraTransform => cameraTransform;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _cameraRig = GetComponent<PlayerCamera>();
            _transform = transform;
            _currentHeight = standHeight;
        }

        private void Start()
        {
            if (cameraTransform == null) cameraTransform = Camera.main ? Camera.main.transform : _transform;
            _controller.height = standHeight;
            _currentPitch = _targetPitch = cameraTransform.localEulerAngles.x;
            _currentYaw = _targetYaw = _transform.eulerAngles.y;
            _verticalVelocity = 0f;
        }

        public void MoveCamera(float lookX, float lookY)
        {
            lookX *= mouseSensitivity * 0.05f;
            lookY *= mouseSensitivity * 0.05f;

            _targetYaw += lookX;
            _targetPitch -= lookY;
            _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);

            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, lookSmoothing * Time.deltaTime);
            _currentPitch = Mathf.LerpAngle(_currentPitch, _targetPitch, lookSmoothing * Time.deltaTime);

            _transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        }

        private void Update()
        {
            HandleCrouchInput();
            HandleHeightTransition();
            HandleMovementAndGravity();
            ApplyCameraRigContributions();
        }

        private void HandleCrouchInput()
        {
            _isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        }

        private void HandleHeightTransition()
        {
            float targetHeight = _isCrouching ? crouchHeight : standHeight;
            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            _controller.height = _currentHeight;

            Vector3 center = _controller.center;
            center.y = _currentHeight * 0.5f;
            _controller.center = center;
        }

        private void HandleMovementAndGravity()
        {
            _isGrounded = _controller.isGrounded;

            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");
            HasMoveInput = Mathf.Abs(inputX) > 0.05f || Mathf.Abs(inputZ) > 0.05f;

            bool runRequested = Input.GetKey(KeyCode.LeftShift);
            _isRunning = runRequested && !_isCrouching && inputZ > 0f;

            float speed = _isCrouching ? crouchSpeed : (_isRunning ? runSpeed : walkSpeed);

            Vector3 moveDir = (_transform.right * inputX + _transform.forward * inputZ).normalized;
            Vector3 targetVel = moveDir * speed;

            if (_isGrounded)
            {
                _velocity.x = Mathf.Lerp(_velocity.x, targetVel.x, acceleration * Time.deltaTime);
                _velocity.z = Mathf.Lerp(_velocity.z, targetVel.z, acceleration * Time.deltaTime);
            }
            else
            {
                _velocity.x = Mathf.Lerp(_velocity.x, targetVel.x, airControl * Time.deltaTime);
                _velocity.z = Mathf.Lerp(_velocity.z, targetVel.z, airControl * Time.deltaTime);
            }

            if (_isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;

            if (_isGrounded && Input.GetButtonDown("Jump"))
            {
                _verticalVelocity = jumpForce;
                _isGrounded = false;
            }

            _verticalVelocity += gravity * Time.deltaTime;

            _velocity.y = _verticalVelocity;
            _controller.Move(_velocity * Time.deltaTime);

            UpdateMoveStateAndFootsteps();
        }

        private void ApplyCameraRigContributions()
        {
            bool moving = _state != MoveState.Idle;
            _cameraRig.ApplyMotion(headBobAmount: moving && _isGrounded ? 1f : 0f, moveSpeed: _state == MoveState.Run ? 1f : 0.5f);
        }

        private void UpdateMoveStateAndFootsteps()
        {
            _state = !HasMoveInput ? MoveState.Idle
                : _isCrouching ? MoveState.Crouch
                : _isRunning ? MoveState.Run
                : MoveState.Walk;

            _moveAmount = _velocity.magnitude;

            if (footstepPlayer != null && _state != MoveState.Idle && _isGrounded)
            {
                footstepPlayer.UpdateFootsteps(_moveAmount, _state == MoveState.Run);
            }
        }

        /// <summary>Teleport to a world position, keeping looking direction.</summary>
        public void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            _transform.position = position;
            _controller.enabled = true;
        }

        public void SetMouseSensitivity(float value)
        {
            mouseSensitivity = value;
        }

        public void SetFootstepPlayer(FootstepPlayer player)
        {
            footstepPlayer = player;
        }
    }
}
