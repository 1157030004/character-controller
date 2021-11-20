using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    public float verticalInput;
    public float horizontalInput;

    public bool jumpInput;
    private Vector2 input;



    private void Start()
    {
        moveAction = playerInput.actions["Movement"];
        jumpAction = playerInput.actions["Jump"];

        input = moveAction.ReadValue<Vector2>();
    }

    public void HandleAllInputs()
    {
        HandleMovementInput();
        HandleActionInput();
    }

    private void HandleMovementInput()
    {
        verticalInput = input.y;
        horizontalInput = input.x;
    }

    private void HandleActionInput()
    {
        jumpInput = jumpAction.triggered;
    }
}
