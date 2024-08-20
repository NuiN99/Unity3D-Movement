using Cinemachine;
using NuiN.Movement;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform camFollowTarget;
    
    IMovementInput _input;

    void Awake()
    {
        _input = GetComponent<IMovementInput>();
    }

    void Update()
    {
        camFollowTarget.localRotation = _input.GetCameraRotation();
    }
}