using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    AnimatorManager animatorManager;
    Animator animator;
    [SerializeField] private float jumpHeight = 3.0f;
    [SerializeField] private float gravityValue = -9.81f;

    [Header("Falling Speeds")]
    [SerializeField] private float inAirTimer;
    [SerializeField] private float leapingVelocity;
    [SerializeField] private float rayCastHeightOffset = 0.5f;
    [SerializeField] private float rayCastDistance = 0.5f;
    public LayerMask groundLayer;

    [Header("Movement Flags")]
    public bool isSprinting;
    public bool isInteracting;
    public bool isGrounded;
    public bool isJumping;


    [Header("Movement Speeds")]
    [SerializeField] private float runningSpeed = 5f;
    [SerializeField] private float walkingSpeed = 1.5f;
    [SerializeField] private float sprintingSpeed = 7f;
    [SerializeField] private float rotationSpeed = 10f;

    [SerializeField] private float moveAmount;
    private CharacterController controller;
    private PlayerInput playerInput;
    private Vector3 playerVelocity;
    private Vector3 move;
    // private bool groudPlayer;
    private Transform cameraTransform;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction b_Input;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        animatorManager = GetComponent<AnimatorManager>();
        cameraTransform = Camera.main.transform;

        moveAction = playerInput.actions["move"];
        jumpAction = playerInput.actions["Jump"];
        b_Input = playerInput.actions["B"];
    }

    private void OnEnable()
    {
        b_Input.performed += _ => HandleSprintingInput(true);
        b_Input.canceled += _ => HandleSprintingInput(false);
    }

    private void OnDisable()
    {
        b_Input.performed -= _ => HandleSprintingInput(false);
    }

    private void LateUpdate()
    {
        HandleAllMovements();
        isInteracting = animator.GetBool("isInteracting");
        isJumping = animator.GetBool("isJumping");
        animator.SetBool("isGrounded", isGrounded);
    }

    public void HandleAllMovements()
    {
        HandleFallingAndLanding();

        if (isInteracting)
            return;
        HandleMovement();
        HandleJumping();
        HandleRotation();
    }

    private void HandleMovement()
    {
        if (isJumping)
            return;

        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        move = new Vector3(input.x, 0, input.y);
        move = move.x * cameraTransform.right.normalized + move.z * cameraTransform.forward.normalized;
        move.y = 0f;

        Debug.Log("Move AMount " + moveAmount);
        if (isSprinting)
        {
            controller.Move(move * sprintingSpeed * Time.deltaTime);
        }
        else
        {
            if (moveAmount >= 0.5f)
            {
                controller.Move(move * runningSpeed * Time.deltaTime);
            }
            else
            {
                controller.Move(move * walkingSpeed * Time.deltaTime);
            }
        }


        controller.Move(move * Time.deltaTime * runningSpeed);

        moveAmount = Mathf.Clamp01(Mathf.Abs(input.x) + Mathf.Abs(input.y));
        animatorManager.UpdateAnimatorValues(0, moveAmount, isSprinting);
    }

    private void HandleJumping()
    {
        // Changes the height position of the player..
        if (jumpAction.triggered && isGrounded)
        {
            animatorManager.animator.SetBool("isJumping", true);
            animatorManager.PlayerTargetAnimation("Jump", true);
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        // playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (isJumping)
            return;

        //! FPS Rotation style
        // Quaternion targetRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        // transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (move == Vector3.zero)
            move = transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(move);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleSprintingInput(bool ctx)
    {
        if (ctx && moveAmount > 0.5f && isGrounded)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

    }

    private void HandleFallingAndLanding()
    {
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        rayCastOrigin.y += rayCastHeightOffset;



        if (!isGrounded & !isJumping)
        {
            if (!isInteracting)
            {
                Debug.Log("Fall");
                animatorManager.PlayerTargetAnimation("Fall", true);
            }
            inAirTimer += Time.deltaTime;
            controller.Move(transform.forward * leapingVelocity * Time.deltaTime);
            playerVelocity.y += gravityValue * inAirTimer * Time.deltaTime;
        }
        if (Physics.SphereCast(rayCastOrigin, rayCastDistance, -transform.up, out hit, rayCastDistance, groundLayer))
        {
            if (!isGrounded && isInteracting)
            {
                Debug.Log("Land");
                animatorManager.PlayerTargetAnimation("Land", true);
            }
            inAirTimer = 0;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 rayCastOrigin = transform.position;
        rayCastOrigin.y += rayCastHeightOffset;
        Gizmos.DrawWireSphere(rayCastOrigin, rayCastDistance);
    }

    //  private void OnCollisionEnter(Collision other)
    // {
    //     if (other.gameObject.tag == "Ground")
    //     {
    //         Debug.Log("hit");
    //         if (!isGrounded)
    //         {
    //             animatorManager.PlayerTargetAnimation("Land", true);
    //         }
    //         inAirTimer = 0;
    //         isGrounded = true;
    //     }
    // }
}