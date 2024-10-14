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
        [SerializeField] Transform cameraTransform;

        [Header("Camera Settings")] 
        [SerializeField] float cameraSensitivity = 20f;
        [SerializeField] float yRotationLimit = 70f;
        
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
        [SerializeField] LayerMask alignMask;
        [SerializeField] float groundCheckDist = 0.1f;
        [SerializeField] float groundCheckRadiusMult = 0.9f;
        [SerializeField] float forwardAlignCheckDist = 0.5f;
        
        [ShowInInspector] bool _isGrounded;
        [ShowInInspector] bool _isJumping;
        [ShowInInspector] int _curAirJumps;
        [ShowInInspector] bool _onInvalidSlope;

        Vector3 _direction = Vector3.zero;
        Vector3 _groundNormal = Vector3.zero;
        Vector2 _cameraRotation;

        void Start()
        {
            _cameraRotation.x = transform.eulerAngles.y;
        }

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
            else if(!_isGrounded)
            {
                rb.AddForce(Physics.gravity, ForceMode.Acceleration);
            }
        }
        
        void IMovement.RotateCamera(Vector2 delta)
        {
            _cameraRotation.x += delta.x * cameraSensitivity;
    
            _cameraRotation.y += delta.y * cameraSensitivity;
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y, -yRotationLimit, yRotationLimit);

            Quaternion horizontalRotation = Quaternion.AngleAxis(_cameraRotation.x, Vector3.up);
            Quaternion verticalRotation = Quaternion.AngleAxis(_cameraRotation.y, Vector3.left);

            cameraTarget.position = transform.position;
            cameraTarget.rotation = horizontalRotation * verticalRotation;
        }

        void IMovement.Move(Vector2 delta, bool isHoldingSprint)
        {
            _isGrounded = disableGroundCheckAfterJumpTimer.IsComplete && IsOnGround();
            
            Vector3 moveDirection = (cameraTransform.forward * delta.y) + (cameraTransform.right * delta.x).With(y: 0).normalized;
            _direction = GetGroundAlignedDirection(moveDirection, _groundNormal);

            if (_isGrounded)
            {
                _isJumping = false;
            }
            
            bool isInputtingDirection = _direction.magnitude > 0;
            if (!isInputtingDirection)
            {
                if (_isGrounded && !_isJumping && !_onInvalidSlope) rb.drag = groundDrag;
                else rb.drag = airDrag;
                return;
            }

            rb.drag = airDrag;
            
            float speed = isHoldingSprint ? maxSpeed * sprintingMaxSpeedMult : maxSpeed;
            
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

        void IMovement.Rotate()
        {
            float speed = turnSpeed * Time.deltaTime;
            
            if (!_isGrounded || _groundNormal == Vector3.up)
            {
                if (_direction != Vector3.zero)
                {
                    Quaternion normalRotation = Quaternion.LookRotation(_direction.With(y:0));
                    transform.rotation = Quaternion.Slerp(transform.rotation, normalRotation, speed);
                }
                return;
            }

            Vector3 validDirection = _direction == Vector3.zero ? Vector3.ProjectOnPlane(rb.velocity, _groundNormal) : _direction;
            if (validDirection == Vector3.zero) 
                return;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(validDirection, _groundNormal), speed);
        }

        void IMovement.HoldJump()
        {
            if (!_isGrounded) return;

            _isGrounded = false;
            _isJumping = true;
            disableGroundCheckAfterJumpTimer.Restart();
                
            _curAirJumps = 0;
            
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }

        void IMovement.PressJump()
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
            Debug.Log(_groundNormal);
            
            Vector3 groundCheckPos = transform.TransformPoint(capsuleCollider.center - new Vector3(0, ((capsuleCollider.height * 0.5f) + groundCheckDist) - capsuleCollider.radius , 0));
            
            Collider[] colliders = Physics.OverlapSphere(groundCheckPos, capsuleCollider.radius * groundCheckRadiusMult, groundMask);

            Vector3 avgNormal = Vector3.zero;
            
            int count = 0;
            foreach (Collider col in colliders)
            {
                if(!alignMask.ContainsLayer(col)) continue;
                
                if (Physics.ComputePenetration(capsuleCollider, groundCheckPos, transform.rotation, col, col.transform.position, col.transform.rotation, out Vector3 normal, out float _))
                {
                    count++;
                    avgNormal += normal;
                }
            }
            avgNormal /= count;
            
            _groundNormal = avgNormal.normalized;
            
            if (Physics.Raycast(groundCheckPos, transform.forward, out RaycastHit hit, forwardAlignCheckDist, alignMask))
            {
                _groundNormal = hit.normal;
            }

            if (_groundNormal.y <= maxWalkableWallAngle)
            {
                _onInvalidSlope = true;
                _groundNormal = Vector3.up;
            }
            else
            {
                _onInvalidSlope = false;
            }
            
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