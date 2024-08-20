using NuiN.Movement;
using NuiN.NExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementInput : MonoBehaviour, IMovementInput
{
    [SerializeField] float lookSensitivity = 20f;
    [Range(0f, 90f)][SerializeField] float yRotationLimit = 88f;
    [SerializeField] Transform camTransform;

    Controls _controls;
    
    Vector2 _rotation;
    Quaternion _cameraRotation;
    
    Vector2 _moveDelta;
    Vector2 _cameraDelta;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        _rotation.x = transform.eulerAngles.y;
    }

    void OnEnable()
    {
        _controls = new Controls();

        _controls.Movement.Move.performed += SetMoveDelta;
        _controls.Movement.Move.canceled += ResetMoveDelta;
        
        _controls.Movement.Camera.performed += SetCameraDelta;
        _controls.Movement.Camera.canceled += ResetCameraDelta;
        
        _controls.Enable();
    }
    void OnDisable()
    {
        _controls = null;
    }
    
    void SetMoveDelta(InputAction.CallbackContext ctx) => _moveDelta = ctx.ReadValue<Vector2>();
    void ResetMoveDelta(InputAction.CallbackContext ctx) => _moveDelta = Vector2.zero;
    void SetCameraDelta(InputAction.CallbackContext ctx) => _cameraDelta = ctx.ReadValue<Vector2>();
    void ResetCameraDelta(InputAction.CallbackContext ctx) => _cameraDelta = Vector2.zero;

    bool IMovementInput.InputtingJump() => _controls.Movement.Jump.IsPressed();
    bool IMovementInput.InputtingSprint() => _controls.Movement.Sprint.IsPressed();

    Vector3 IMovementInput.GetDirection()
    {
        Vector3 moveDirection = (transform.forward * _moveDelta.y) + (transform.right * _moveDelta.x).With(y:0);
        return moveDirection.normalized;
    }
    
    Quaternion IMovementInput.GetRotation()
    {
        _rotation.x += _cameraDelta.x * lookSensitivity;
        
        return Quaternion.AngleAxis(_rotation.x, Vector3.up);
    }

    Quaternion IMovementInput.GetCameraRotation()
    {
        _rotation.y += _cameraDelta.y * lookSensitivity;
        _rotation.y = Mathf.Clamp(_rotation.y, -yRotationLimit, yRotationLimit);
        
        _cameraRotation = Quaternion.AngleAxis(_rotation.y, Vector3.left);
        return _cameraRotation;
    }
}