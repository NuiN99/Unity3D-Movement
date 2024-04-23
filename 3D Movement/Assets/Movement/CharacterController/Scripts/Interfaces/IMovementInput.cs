using UnityEngine;

namespace NuiN.Movement
{
    public interface IMovementInput
    {
        Vector3 GetDirection();
        Quaternion GetRotation();
        Quaternion GetHeadRotation();
        bool ShouldJump();
        bool IsRunning();
    }
}