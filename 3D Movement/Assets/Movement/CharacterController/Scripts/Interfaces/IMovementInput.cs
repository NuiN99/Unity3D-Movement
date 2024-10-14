using System;
using UnityEngine;

namespace NuiN.Movement
{
    public interface IMovementInput
    {
        public event Action OnPressJump;
        public Vector2 MoveDelta { get; }
        public Vector2 CameraDelta { get; }
        public bool IsHoldingJump { get; }
        public bool IsHoldingSprint { get; }
    }
}