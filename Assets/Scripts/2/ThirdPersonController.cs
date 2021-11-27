using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        #region Variables

        #region Player
        [Header("Player")]
        [Tooltip("Walk speed of the character in m/s")]
        public float WalkSpeed = 2.3f;
        [Tooltip("Run speed of the character in m/s")]
        public float RunSpeed = 5.3f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 7.335f;
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;
        [Tooltip("Time required to pass before entering climb state")]
        public float ClimbTimeout = 0.15f;
        #endregion

        #region Player Grounded
        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;
        #endregion

        #region Player Climbing
        [Header("Player Climbing")]
        [Tooltip("If the character collides with climbable object")]
        public bool isClimbing;
        bool isLearping;
        bool inPosition;
        float posT;
        Vector3 startPos;
        Vector3 targetPos;
        Quaternion startRot;
        Quaternion targetRot;
        public float positionOffset;
        public float offsetFromWall = 0.3f;
        public float speed_multiplier = 0.2f;
        public float climbSpeed = 3.0f;
        public float climbRotateSpeed = 5.0f;
        public float inAngleDistance = 1f;
        public IKSnapshot baseIKsnapshot;
        public FreeClimbAnimatorHook a_hook;
        Transform helper;
        #endregion

        #region Cinemachine
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;
        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;
        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;
        #endregion

        #region cinemachine
        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        int horizontal;
        int vertical;
        #endregion

        #region timeout delta
        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _climbTimeoutDelta;
        #endregion

        #region animation IDs
        // animation IDs
        private int _animIDHorizontal;
        private int _animIDVertical;
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        #endregion

        #region classes
        public Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        #endregion

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        #endregion
        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            AssignAnimationIDs();

            helper = new GameObject().transform;
            helper.name = "climb helper";
            // a_hook.Init(this, helper);
            // ClimbingCheck();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _climbTimeoutDelta = ClimbTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
            // Tick(_climbTimeoutDelta);
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDHorizontal = Animator.StringToHash("Horizontal");
            _animIDVertical = Animator.StringToHash("Vertical");
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                _cinemachineTargetYaw += _input.look.x * Time.deltaTime;
                _cinemachineTargetPitch += _input.look.y * Time.deltaTime;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        public void UpdateAnimatorValues(float horizontalMovement, float verticalMovement, bool isSprinting)
        {
            //! Animation Snapping
            float snappedHorizontal;
            float snappedVertical;


            #region Snapped Horizontal
            if (horizontalMovement > 0 && horizontalMovement < 1.55f)
            {
                snappedHorizontal = 0.5f;
            }
            else if (horizontalMovement > 5.3f)
            {
                snappedHorizontal = 1f;
            }
            else if (horizontalMovement < 0 && horizontalMovement > -0.5f)
            {
                snappedHorizontal = -0.5f;
            }
            else if (horizontalMovement < -0.5f)
            {
                snappedHorizontal = -1f;
            }
            else
            {
                snappedHorizontal = 0;
            }
            #endregion
            #region Snapped Vertical
            if (verticalMovement > 0 && verticalMovement < 2.3f)
            {
                Debug.Log("verticalMovement 1: " + verticalMovement);

                snappedVertical = 0.5f;
            }
            else if (verticalMovement > 5.3f)
            {
                snappedVertical = 1f;
                Debug.Log("verticalMovement 2: " + verticalMovement);
            }
            else if (verticalMovement < 0 && verticalMovement > -0.5f)
            {
                snappedVertical = -0.5f;
                Debug.Log("verticalMovement 3: " + verticalMovement);
            }
            else if (verticalMovement < -0.5f)
            {
                snappedVertical = -1f;
                Debug.Log("verticalMovement 4: " + verticalMovement);
            }
            else
            {
                snappedVertical = 0;
                Debug.Log("verticalMovement 5: " + verticalMovement);
            }
            #endregion

            if (isSprinting && _speed > 5.0f)
            {
                snappedHorizontal = horizontalMovement;
                snappedVertical = 2;
            }
            else
            {
                // Dash
            }

            _animator.SetFloat(_animIDHorizontal, snappedHorizontal, 0.1f, Time.deltaTime);
            _animator.SetFloat(_animIDVertical, snappedVertical, 0.1f, Time.deltaTime);
        }
        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            // float targetSpeed = _input.sprint ? SprintSpeed : WalkSpeed;
            float targetSpeed = _input.sprint ? SprintSpeed : _animationBlend >= 5f ? RunSpeed : WalkSpeed;

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;


            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;


            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_input.move == Vector2.zero) _animationBlend = 0f;

            UpdateAnimatorValues(0, _animationBlend, _input.sprint);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        #region Climbing


        private void ClimbingCheck()
        {
            Vector3 origin = transform.position;
            origin.y += 1.4f;
            Vector3 dir = transform.forward;
            RaycastHit hit;
            if (Physics.Raycast(origin, dir, out hit, 5.0f))
            {
                helper.position = PosWithOffset(origin, hit.point);
                InitClimbing(hit);
            }
        }

        private void InitClimbing(RaycastHit hit)
        {
            isClimbing = true;
            helper.transform.rotation = Quaternion.LookRotation(-hit.normal);
            startPos = transform.position;
            targetPos = hit.point + (hit.normal * offsetFromWall);
            posT = 0;
            inPosition = false;
            _animator.CrossFade("Climb", 2f);
        }

        public void Tick(float delta)
        {
            if (!inPosition)
            {
                GetInPosition();
                return;
            }


            if (!isLearping)
            {
                Vector3 inputDirection = new Vector3(_input.move.x, _input.move.y, 0.0f).normalized;
                float m = Mathf.Abs(inputDirection.x) + Mathf.Abs(inputDirection.y);

                Vector3 horizontal = helper.right * inputDirection.x;
                Vector3 vertical = helper.up * inputDirection.y;
                Vector3 moveDirection = horizontal + vertical;

                bool canMove = CanMove(moveDirection);
                if (!canMove || moveDirection == Vector3.zero)
                    return;

                posT = 0;
                isLearping = true;
                startPos = transform.position;
                // Vector3 tp = helper.position + transform.position;
                targetPos = helper.position;
                // a_hook.CreatePositions(targetPos);
            }
            else
            {
                posT += delta * climbSpeed;
                if (posT > 1)
                {
                    posT = 1;
                    isLearping = false;
                }

                Vector3 climbPosition = Vector3.Lerp(startPos, targetPos, posT);
                transform.position = climbPosition;
                transform.rotation = Quaternion.Slerp(transform.rotation, helper.rotation, delta * climbRotateSpeed);
            }
        }

        bool CanMove(Vector3 moveDir)
        {
            Vector3 origin = transform.position;
            float distance = positionOffset;
            Vector3 dir = moveDir;

            Debug.DrawRay(origin, dir * distance, Color.red);
            RaycastHit hit;
            if (Physics.Raycast(origin, dir, out hit, distance))
            {
                return false;
            }

            origin += moveDir * distance;
            dir = helper.forward;
            float distance2 = inAngleDistance;

            // Climb a wall with extreme different normal direction
            Debug.DrawRay(origin, dir * distance2, Color.blue);
            if (Physics.Raycast(origin, dir, out hit, distance))
            {
                helper.position = PosWithOffset(origin, hit.point);
                helper.rotation = Quaternion.LookRotation(-hit.normal);

                return true;
            }

            origin += dir * distance2;
            dir = -Vector3.up;
            Debug.DrawRay(origin, dir, Color.yellow);

            if (Physics.Raycast(origin, dir, out hit, distance2))
            {
                float angle = Vector3.Angle(helper.up, hit.normal);
                if (angle < 40)
                {
                    helper.position = PosWithOffset(origin, hit.point);
                    helper.rotation = Quaternion.LookRotation(-hit.normal);
                    return true;
                }
            }

            return false;
        }

        void GetInPosition()
        {
            posT += _climbTimeoutDelta;

            if (posT > 1)
            {
                posT = 1;
                inPosition = true;

                // Enable the IK
                // a_hook.CreatePositions(targetPos);

            }

            Vector3 targetPosition = Vector3.Lerp(startPos, targetPos, posT);
            transform.position = targetPosition;
            transform.rotation = Quaternion.Slerp(transform.rotation, helper.rotation, _climbTimeoutDelta * climbRotateSpeed);
        }

        Vector3 PosWithOffset(Vector3 origin, Vector3 target)
        {
            Vector3 dir = origin - target;
            dir.Normalize();
            Vector3 offset = dir * offsetFromWall;
            return target + offset;
        }
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
        #endregion
        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }


    }

    [System.Serializable]
    public class IKSnapshot
    {
        public Vector3 rh, lh, rf, lf;
    }
}