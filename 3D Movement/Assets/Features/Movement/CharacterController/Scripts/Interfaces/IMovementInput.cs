using UnityEngine;

namespace NuiN.Movement
{
    public interface IMovementInput
    {
        Vector3 GetDirection();
        Quaternion GetRotation();
        bool ShouldJump();
        bool IsRunning();
    }
}