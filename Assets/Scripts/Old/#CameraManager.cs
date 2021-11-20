using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    InputManager inputManager;
    public Transform targetTransform;
    public Transform cameraPivot;

    public float lookAngle;
    public float pivotAngle;

    public float cameraLookSpeed = 2;
    public float cameraPivotSpeed = 2;

    private void Awake()
    {
        inputManager = FindObjectOfType<InputManager>();
    }

    public void HandleAllCameraMovements()
    {
        RotateCamera();
    }
    private void RotateCamera()
    {
        lookAngle += inputManager.cameraInputX * cameraLookSpeed;
        pivotAngle -= inputManager.cameraInputY * cameraPivotSpeed;

        Vector3 rotation = Vector3.zero;
        rotation.y = lookAngle;
        Quaternion targetRotation = Quaternion.Euler(rotation);
        transform.rotation = targetRotation;

        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        targetRotation = Quaternion.Euler(rotation);
        cameraPivot.localRotation = targetRotation;
    }
}
