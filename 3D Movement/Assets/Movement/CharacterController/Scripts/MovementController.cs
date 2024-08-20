using System;
using NuiN.NExtensions;
using UnityEngine;

namespace NuiN.Movement
{
    [RequireComponent(typeof(IMovement), typeof(IMovementInput))]
    public class MovementController : MonoBehaviour
    {
        public bool IsRunning => _input.InputtingSprint();

        [NonSerialized, ShowInInspector] public bool canMove = true;
        [NonSerialized, ShowInInspector] public bool canRotate = true;
        
        IMovement _movement;
        IMovementInput _input;

        void Awake()
        {
            _movement = GetComponent<IMovement>();
            if(_movement == null) Debug.LogError($"Missing Movement component on {gameObject}", gameObject);
            
            _input = GetComponent<IMovementInput>();
            if (_input == null) Debug.LogError($"Missing MovementInput on {gameObject.name}", gameObject);
        }
        
        void Update()
        {
            if(_input.InputtingJump()) _movement.Jump();
            if(canRotate) _movement.Rotate(_input);
        }

        void FixedUpdate()
        {
            if(canMove) _movement.Move(_input);
        }
    }
}



