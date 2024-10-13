using NuiN.NExtensions;
using UnityEngine;

namespace NuiN.Movement
{
    public class GroundMovement : MonoBehaviour, IMovement
    {
        [Header("Dependencies")]
        [SerializeField] Rigidbody rb;
        [SerializeField] CapsuleCollider capsuleCollider;
        [SerializeField] Transform stepCheck;

        [Header("Move Speed Settings")]
        [SerializeField] float maxSpeed = 10;
        [SerializeField] float acceleration = 5;
        [SerializeField] float sprintingMaxSpeedMult = 2f;
        [SerializeField] float airControlFactor = 0.25f;
        [SerializeField] float groundDrag = 15f;
        [SerializeField] float airDrag = 0.002f;
        
        [Header("Jump Settings")] 
        [SerializeField] float jumpForce = 6f;
        [SerializeField] int maxAirJumps = 1;
        [SerializeField] float gravity = 0.15f;
        [SerializeField] float gravityStartVelocityUp = 3f;
        [SerializeField] Timer disableGroundCheckAfterJumpTimer;

        [Header("Environment Settings")]
        [SerializeField] LayerMask groundMask;
        [SerializeField] float groundCheckDist = 0.1f;
        [SerializeField] float groundCheckRadiusMult = 0.9f;
        
        [ShowInInspector] bool _isGrounded;
        [ShowInInspector] bool _isJumping;
        [ShowInInspector] bool _isSprinting;
        [ShowInInspector] bool _isInputtingJump;
        [ShowInInspector] int _curAirJumps;

        Vector3 _direction;

        Vector3 BottomOfCapsule => transform.TransformPoint(capsuleCollider.center - new Vector3(0, (capsuleCollider.height * 0.5f) - capsuleCollider.radius , 0));
        
        void FixedUpdate()
        {
            if (!_isGrounded && rb.velocity.y <= gravityStartVelocityUp)
            {
                rb.velocity += Vector3.down * gravity;
            }
        }

        void IMovement.Move(IMovementInput input)
        {
            bool wasInputtingJump = _isInputtingJump;
            _isInputtingJump = input.InputtingJump();
            if (!wasInputtingJump && _isInputtingJump && _isJumping && disableGroundCheckAfterJumpTimer.IsComplete)
            {
                JumpAir();
            }
            
            _direction = input.GetDirection();
            _isGrounded = 
                disableGroundCheckAfterJumpTimer.IsComplete && 
                Physics.OverlapSphere(BottomOfCapsule.Add(y: -groundCheckDist), capsuleCollider.radius * groundCheckRadiusMult, groundMask).Length > 0;

            if (_isGrounded)
            {
                _isJumping = false;
            }
            
            bool isInputtingDirection = _direction.magnitude > 0;
            if (!isInputtingDirection)
            {
                if (_isGrounded && !_isJumping) rb.drag = groundDrag;
                else rb.drag = airDrag;
                return;
            }

            rb.drag = airDrag;
            
            _isSprinting = input.InputtingSprint();
            float speed = _isSprinting ? maxSpeed * sprintingMaxSpeedMult : maxSpeed;
            
            // Calculate velocity along the input direction
            Vector3 inputVelocity = _direction * speed;
            
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

            // Separate the current velocity into two components:
            // 1. In the direction of the input
            Vector3 velocityInInputDir = Vector3.Project(horizontalVelocity, _direction);

            // 2. Perpendicular to the input (this is to preserve any existing momentum)
            Vector3 velocityPerpendicular = horizontalVelocity - velocityInInputDir;

            // Calculate the force we need to move toward the input velocity
            Vector3 desiredVelocity = inputVelocity + velocityPerpendicular;

            // Ensure we don't exceed max speed
            if (desiredVelocity.magnitude > speed)
            {
                desiredVelocity = desiredVelocity.normalized * speed;
            }

            // Apply force in the direction of the input, allowing for acceleration
            Vector3 force = (desiredVelocity - horizontalVelocity) * (acceleration * Time.fixedDeltaTime);
            
            // Apply less force when in the air
            if (!_isGrounded)
            {
                force *= airControlFactor;
            }

            rb.AddForce(force, ForceMode.VelocityChange);
        }

        void IMovement.Rotate(IMovementInput input)
        {
            Quaternion rotation = input.GetRotation();
            transform.rotation = rotation;
        }

        void IMovement.Jump()
        {
            if (!_isGrounded) return;
            
            _isJumping = true;
            disableGroundCheckAfterJumpTimer.Restart();
                
            _curAirJumps = 0;
            rb.velocity = rb.velocity.With(y: jumpForce);
        }

        void JumpAir()
        {
            if (_curAirJumps >= maxAirJumps) return;
            _curAirJumps++;

            // only sets y velocity when y velocity is less than potential jump force. Otherwise it would set y vel to a lower value when going faster
            if (rb.velocity.y <= jumpForce)
            {
                rb.velocity = rb.velocity.With(y: jumpForce);
            }
            else
            {
                rb.velocity += Vector3.up * jumpForce;
            }
        }
    }
}