using System.Collections.Generic;
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
        [SerializeField] float maxWalkableWallAngle;
        
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
        [SerializeField] float forwardAlignCheckDist = 0.5f;
        
        [ShowInInspector] bool _isGrounded;
        [ShowInInspector] bool _isJumping;
        [ShowInInspector] bool _isSprinting;
        [ShowInInspector] bool _isInputtingJump;
        [ShowInInspector] int _curAirJumps;

        Vector3 _direction = Vector3.zero;
        Vector3 _groundNormal = Vector3.zero;

        void FixedUpdate()
        {
            ApplyGravity();
        }

        void ApplyGravity()
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
            
            Vector3 inputVelocity = _direction * speed;
            
            Vector3 velocityInInputDir = Vector3.Project(rb.velocity, _direction);
            
            Vector3 velocityPerpendicular = rb.velocity - velocityInInputDir;

            Vector3 desiredVelocity = inputVelocity + velocityPerpendicular;

            if (desiredVelocity.magnitude > speed)
            {
                desiredVelocity = desiredVelocity.normalized * speed;
            }

            Vector3 force = (desiredVelocity - rb.velocity) * (acceleration * Time.fixedDeltaTime);
            
            if (!_isGrounded)
            {
                force *= airControlFactor;
            }

            rb.AddForce(force, ForceMode.VelocityChange);
        }

        void IMovement.Rotate(IMovementInput input)
        {
            if (!_isGrounded)
            {
                if (_direction != Vector3.zero)
                {
                    Quaternion normalRotation = Quaternion.LookRotation(_direction.With(y:0));
                    transform.rotation = Quaternion.Slerp(transform.rotation, normalRotation, turnSpeed);
                }
                return;
            }

            Vector3 validDirection = _direction == Vector3.zero ? Vector3.ProjectOnPlane(rb.velocity, _groundNormal) : _direction;
            if (validDirection == Vector3.zero) 
                return;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(validDirection, _groundNormal), turnSpeed);
        }

        void IMovement.Jump()
        {
            if (!_isGrounded) return;

            _isGrounded = false;
            _isJumping = true;
            disableGroundCheckAfterJumpTimer.Restart();
                
            _curAirJumps = 0;

            // wall normal jump
            /*Vector3 wallNormal = _groundNormal;
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.velocity = horizontalVelocity + (jumpForce * wallNormal.normalized);*/
            
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }
        
        void JumpAir()
        {
            if (_curAirJumps >= maxAirJumps) return;
            _curAirJumps++;
            
            if (rb.velocity.y <= jumpForce)
            {
                rb.velocity = rb.velocity.With(y: jumpForce);
            }
            else
            {
                rb.velocity += Vector3.up * jumpForce;
            }
        }

        
        bool IsOnGround()
        {
            Vector3 groundCheckPos = transform.TransformPoint(capsuleCollider.center - new Vector3(0, ((capsuleCollider.height * 0.5f) + groundCheckDist) - capsuleCollider.radius , 0));

            if (Physics.Raycast(groundCheckPos, rb.velocity.normalized, out RaycastHit hit, forwardAlignCheckDist, groundMask))
            {
                _groundNormal = hit.normal;
                return true;
            }
            
            Collider[] colliders = Physics.OverlapSphere(groundCheckPos, capsuleCollider.radius * groundCheckRadiusMult, groundMask);

            Vector3 avgNormal = Vector3.zero;
            
            int count = 0;
            foreach (Collider col in colliders)
            {
                if (Physics.ComputePenetration(capsuleCollider, groundCheckPos, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 normal, out float dist))
                {
                    count++;
                    avgNormal += normal;
                }
            }
            avgNormal /= count;
            
            _groundNormal = avgNormal.normalized;
            
            return colliders.Length > 0;
        }
        
        Vector3 GetGroundAlignedDirection(Vector3 movementDirection, Vector3 groundNormal)
        {
            if (groundNormal.sqrMagnitude <= float.MinValue || !_isGrounded)
            {
                return movementDirection;
            }

            Quaternion rotationToGround = Quaternion.FromToRotation(Vector3.up, groundNormal);

            Vector3 rotatedMovementDirection = rotationToGround * movementDirection;

            rotatedMovementDirection = Vector3.ProjectOnPlane(rotatedMovementDirection, groundNormal);

            return rotatedMovementDirection.normalized;
        }
    }
}