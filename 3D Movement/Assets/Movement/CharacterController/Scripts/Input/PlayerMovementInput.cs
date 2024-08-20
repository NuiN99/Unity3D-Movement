using NuiN.Movement;
using NuiN.NExtensions;
using UnityEngine;

public class PlayerMovementInput : MonoBehaviour, IMovementInput
{
    const string AXIS_X = "Horizontal";
    const string AXIS_Y = "Vertical";
    
    const string MOUSE_X = "Mouse X";
    const string MOUSE_Y = "Mouse Y";
    
    
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    
    [SerializeField] float lookSensitivity = 20f;
    [Range(0f, 90f)][SerializeField] float yRotationLimit = 88f;

    Vector2 _rotation;
    Quaternion _cameraRotation;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        _rotation.x = transform.eulerAngles.y;
    }

    bool IMovementInput.ShouldJump()
    {
        return Input.GetKeyDown(jumpKey);
    }

    Vector3 IMovementInput.GetDirection()
    {
        float x = Input.GetAxisRaw(AXIS_X);
        float z = Input.GetAxisRaw(AXIS_Y);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMoveDirection = forward * z + right * x;

        return desiredMoveDirection.normalized;
    }
    
    Quaternion IMovementInput.GetRotation()
    {
        _rotation.x += Input.GetAxis(MOUSE_X) * lookSensitivity;
        
        return Quaternion.AngleAxis(_rotation.x, Vector3.up);
    }

    Quaternion IMovementInput.GetCameraRotation()
    {
        _rotation.y += Input.GetAxis(MOUSE_Y) * lookSensitivity;
        _rotation.y = Mathf.Clamp(_rotation.y, -yRotationLimit, yRotationLimit);
        
        _cameraRotation = Quaternion.AngleAxis(_rotation.y, Vector3.left);
        return _cameraRotation;
    }

    bool IMovementInput.IsRunning()
    {
        return Input.GetKey(sprintKey);
    }
}