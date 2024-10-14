using Cinemachine.Utility;
using NuiN.NExtensions;
using UnityEngine;

namespace NuiN.Movement
{
    public class GroundMovement : MonoBehaviour, IMovement
    {
        [Header("Dependencies")]
        [SerializeField] Rigidbody rb;
        [SerializeField] CapsuleCollider capsuleCollider;
        [SerializeField] Transform cameraTarget;

        [Header("Move Speed Settings")]
        [SerializeField] float maxSpeed = 10;
        [SerializeField] float acceleration = 5;
        [SerializeField] float sprintingMaxSpeedMult = 2f;
        [SerializeField] float airControlFactor = 0.25f;
        [SerializeField] float groundDrag = 15f;
        [SerializeField] float airDrag = 0.002f;

        [Header("Rotation Settings")] 
        [SerializeField] float turnSpeed;
        [SerializeField] Timer resetRotationAfterLeaveGroundTimer;
        
        [Header("Jump Settings")] 
        [SerializeField] float jumpForce = 6f;
        [SerializeField] int maxAirJumps = 1;
        [SerializeField] float postJumpGravity = 0.15f;
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

        Vector3 _direction = Vector3.zero;
        Vector3 _groundNormal = Vector3.zero;
        Vector3 _lastGroundNormal = Vector3.zero;

        void FixedUpdate()
        {
            if (!_isGrounded && rb.velocity.y <= gravityStartVelocityUp)
            {
                rb.velocity += Vector3.down * postJumpGravity;
            }
            
            if(!_isGrounded)
            {
                rb.AddForce(Physics.gravity, ForceMode.Acceleration);
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
            
            _direction = GetGroundAlignedDirection(input.GetDirection(), _groundNormal);
            
            _isGrounded = disableGroundCheckAfterJumpTimer.IsComplete && IsOnGround();

            if (_isGrounded)
            {
                resetRotationAfterLeaveGroundTimer.Restart();
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
            
            // Separate the current velocity into two components:
            // 1. In the direction of the input
            Vector3 velocityInInputDir = Vector3.Project(rb.velocity, _direction);

            // 2. Perpendicular to the input (this is to preserve any existing momentum)
            Vector3 velocityPerpendicular = rb.velocity - velocityInInputDir;

            // Calculate the force we need to move toward the input velocity
            Vector3 desiredVelocity = inputVelocity + velocityPerpendicular;

            // Ensure we don't exceed max speed
            if (desiredVelocity.magnitude > speed)
            {
                desiredVelocity = desiredVelocity.normalized * speed;
            }

            // Apply force in the direction of the input, allowing for acceleration
            Vector3 force = (desiredVelocity - rb.velocity) * (acceleration * Time.fixedDeltaTime);
            
            // Apply less force when in the air
            if (!_isGrounded)
            {
                force *= airControlFactor;
            }

            rb.AddForce(force, ForceMode.VelocityChange);
        }

        void IMovement.Rotate(IMovementInput input)
        {
            if (!_isGrounded && resetRotationAfterLeaveGroundTimer.IsComplete)
            {
                Quaternion normalRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, normalRotation, turnSpeed);
            }
            
            if (_direction.sqrMagnitude <= 0)
            {
                return;
            }

            // Ignore the vertical component of the movement direction
            Vector3 flatMoveDir = new Vector3(_direction.x, 0, _direction.z).normalized;

            // Align to the ground normal first
            Quaternion groundAlignmentRotation = Quaternion.FromToRotation(Vector3.up, _groundNormal);

            // Calculate the target Y-axis rotation based on movement direction
            Quaternion targetYRotation = Quaternion.identity;
            if (flatMoveDir.sqrMagnitude > 0)
            {
                targetYRotation = Quaternion.LookRotation(flatMoveDir, Vector3.up);
            }
            
            // Apply the ground alignment to the player's rotation
            Quaternion combinedRotation = groundAlignmentRotation * targetYRotation;

            // Smoothly interpolate towards the new rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, combinedRotation, turnSpeed);
        }

        void IMovement.Jump()
        {
            if (!_isGrounded) return;
            
            _isJumping = true;
            disableGroundCheckAfterJumpTimer.Restart();
                
            _curAirJumps = 0;
            
            rb.velocity = rb.velocity.With(y: jumpForce);
        }

        bool IsOnGround()
        {
            Vector3 groundCheckPos = transform.TransformPoint(capsuleCollider.center - new Vector3(0, ((capsuleCollider.height * 0.5f) + groundCheckDist) - capsuleCollider.radius , 0));
            
            Collider[] colliders = Physics.OverlapSphere(groundCheckPos, capsuleCollider.radius * groundCheckRadiusMult, groundMask);

            Vector3 avgNormal = Vector3.zero;
            /*Vector3 avgVel = Vector3.zero;*/
            
            int count = 0;
            foreach (Collider col in colliders)
            {
                if (Physics.ComputePenetration(capsuleCollider, groundCheckPos, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 normal, out float dist))
                {
                    count++;
                    avgNormal += normal;

                    /*if (col.TryGetComponent(out Rigidbody hitRB))
                    {
                        avgVel += hitRB.velocity;
                    }*/
                }
            }
            avgNormal /= count;
            /*avgVel /= count;*/

            /*if (!avgVel.IsNaN())
            {
                rb.AddForce(avgVel * 0.054f, ForceMode.VelocityChange);
            }*/

            _lastGroundNormal = _groundNormal;
            _groundNormal = avgNormal.normalized;
            
            return colliders.Length > 0;
        }
        
        Vector3 GetGroundAlignedDirection(Vector3 movementDirection, Vector3 groundNormal)
        {
            // Ensure the normal is valid
            if (groundNormal.sqrMagnitude <= float.MinValue || !_isGrounded)
            {
                return movementDirection;
            }

            // Calculate the rotation to align movement with the ground
            Quaternion rotationToGround = Quaternion.FromToRotation(Vector3.up, groundNormal);

            // Rotate the movement direction to match the ground normal
            Vector3 rotatedMovementDirection = rotationToGround * movementDirection;

            // Remove any upward component to prevent additional upward force when moving uphill
            rotatedMovementDirection = Vector3.ProjectOnPlane(rotatedMovementDirection, groundNormal);

            return rotatedMovementDirection.normalized;
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