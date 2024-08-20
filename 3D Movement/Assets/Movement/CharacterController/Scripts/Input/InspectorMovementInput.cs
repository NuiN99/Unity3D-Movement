using NuiN.Movement;
using UnityEngine;

public class InspectorMovementInput : MonoBehaviour, IMovementInput
{
    [SerializeField] Vector3 direction = new Vector3(0, 0, 1);
    [SerializeField] Quaternion rotation = Quaternion.Euler(0,0,0);
    [SerializeField] bool isRunning = false;
    [SerializeField] bool jump = false;
    
    Vector3 IMovementInput.GetDirection()
    {
        return direction;
    }

    Quaternion IMovementInput.GetRotation()
    {
        return rotation;
    }

    public Quaternion GetCameraRotation()
    {
        return default;
    }

    bool IMovementInput.ShouldJump()
    {
        return jump;
    }

    bool IMovementInput.IsRunning()
    {
        return isRunning;
    }
}
