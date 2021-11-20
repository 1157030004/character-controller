using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    InputManager inputManager;
    InputHandler inputHandler;
    PlayerLocomotion playerLocomotion;
    Locomotion locomotion;
    CameraManager cameraManager;

    private void Awake()
    {
        // inputManager = GetComponent<InputManager>();
        // playerLocomotion = GetComponent<PlayerLocomotion>();
        inputHandler = GetComponent<InputHandler>();
        locomotion = GetComponent<Locomotion>();
        cameraManager = FindObjectOfType<CameraManager>();
    }

    private void Update()
    {
        // inputManager.HandleAllInputs();
        inputHandler.HandleAllInputs();
    }

    private void FixedUpdate()
    {
        // playerLocomotion.HandleAllMovements();
        locomotion.HandleAllMovements();
    }

    // private void LateUpdate()
    // {
    //     cameraManager.HandleAllCameraMovements();
    // }
}
