using System;
using NuiN.Movement;
using NuiN.NExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementInput : MonoBehaviour, IMovementInput
{
    public event Action OnPressJump = delegate { };
    public Vector2 MoveDelta => _controls.Movement.Move.ReadValue<Vector2>();
    public Vector2 CameraDelta => _controls.Movement.Camera.ReadValue<Vector2>();
    public bool IsHoldingJump => _controls.Movement.Jump.IsPressed();
    public bool IsHoldingSprint => _controls.Movement.Sprint.IsPressed();
    
    Controls _controls;
    
    Vector2 _rotation;
    Quaternion _cameraRotation;
    
    Vector2 _moveDelta;
    Vector2 _cameraDelta;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
    }

    void OnEnable()
    {
        _controls = new Controls();

        _controls.Movement.Jump.performed += _ => OnPressJump.Invoke();
        
        _controls.Enable();
    }
    void OnDisable()
    {
        _controls.Disable();
        _controls = null;
    }
}