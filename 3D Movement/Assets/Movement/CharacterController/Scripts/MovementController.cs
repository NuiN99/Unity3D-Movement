using System;
using NuiN.NExtensions;
using UnityEngine;

namespace NuiN.Movement
{
    public class MovementController : MonoBehaviour
    {
        [NonSerialized, ShowInInspector] public bool canMove = true;
        [NonSerialized, ShowInInspector] public bool canRotate = true;

        [SerializeField] SerializedInterface<IMovement> movement;
        [SerializeField] SerializedInterface<IMovementInput> input;

        IMovement Movement => movement.Value;
        IMovementInput Input => input.Value;

        void OnEnable() => Input.OnPressJump += PressJumpHandler;
        void OnDisable() => Input.OnPressJump -= PressJumpHandler;

        void Update()
        {
            if(canMove && Input.IsHoldingJump) Movement.HoldJump();
            if(canRotate) Movement.Rotate();
            Movement.RotateCamera(Input.CameraDelta);
        }

        void PressJumpHandler()
        {
            if(canMove) Movement.PressJump();
        }

        void FixedUpdate()
        {
            if(canMove) Movement.Move(Input.MoveDelta, Input.IsHoldingSprint);
        }
    }
}



