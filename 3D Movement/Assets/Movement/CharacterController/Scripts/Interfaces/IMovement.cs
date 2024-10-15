using UnityEngine;

namespace NuiN.Movement
{
    public interface IMovement
    {
        void Move(Vector2 input, bool isHoldingSprint);
        void Rotate();
        void RotateCamera(Vector2 input);
        void GroundJump();
        void AirJump();
    }
}