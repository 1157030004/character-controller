using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(InputHandler))]
public class Locomotion : MonoBehaviour
{
    InputHandler inputHandler;

    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravityValue = -9.81f;
    private CharacterController controller;
    private PlayerInput playerInput;
    private Vector3 playerVelocity;
    private bool groundedPlayer;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        inputHandler = GetComponent<InputHandler>();

    }

    // void Update()
    // {
    //     groundedPlayer = controller.isGrounded;
    //     if (groundedPlayer && playerVelocity.y < 0)
    //     {
    //         playerVelocity.y = 0f;
    //     }

    //     Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    //     controller.Move(move * Time.deltaTime * playerSpeed);

    //     if (move != Vector3.zero)
    //     {
    //         gameObject.transform.forward = move;
    //     }

    //     // Changes the height position of the player..
    //     if (Input.GetButtonDown("Jump") && groundedPlayer)
    //     {
    //         playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
    //     }

    //     playerVelocity.y += gravityValue * Time.deltaTime;
    //     controller.Move(playerVelocity * Time.deltaTime);
    // }

    public void HandleAllMovements()
    {
        HandleMovement();
        HandleAction();
    }

    private void HandleMovement()
    {
        Vector3 move = new Vector3(inputHandler.horizontalInput, 0, inputHandler.verticalInput);
        controller.Move(move * Time.deltaTime * playerSpeed);

    }

    private void HandleAction()
    {
        if (inputHandler.jumpInput && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
}