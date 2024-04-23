using NuiN.NExtensions;
using UnityEngine;

namespace NuiN.Movement
{
    public class GroundMovement : MonoBehaviour, IMovement
    {
        [Header("Dependencies")]
        [SerializeField] Rigidbody rb;
        [SerializeField] SphereCollider feet;
        [SerializeField] Transform stepCheck;

        [Header("Move Speed Settings")]
        [SerializeField] float moveSpeed = 0.375f;
        [SerializeField] float runSpeedMult = 1.5f;
        [SerializeField] float maxAirVelocityMagnitude = 6.2f;
        [SerializeField] float groundSpeedMult = 1.8f;
        [SerializeField] float groundDrag = 15f;
        [SerializeField] float airDrag = 0.002f;
        [SerializeField] float airNoInputCounteractMult = 0.01f;

        [Header("Step Settings")] 
        [SerializeField] float stepHeight = 0.2f;
        [SerializeField] float stepSpeed = 0.15f;
        [SerializeField] float bottomStepCheckDist = 0.3f;
        [SerializeField] float topStepCheckDist = 0.4f;
        
        [Header("Rotate Speed Settings")]
        [SerializeField] float walkingRotateSpeed = 99999f;
        [SerializeField] float runningRotateSpeed = 99999f;

        [Header("Jump Settings")] 
        [SerializeField] SimpleTimer jumpDelay = new(0.2f);
        [SerializeField] float jumpForce = 6f;
        [SerializeField] int maxAirJumps = 1;
        [SerializeField] float downForceMult = 0.15f;
        [SerializeField] float downForceStartUpVelocity = 3f;

        [Header("Environment Settings")]
        [SerializeField] LayerMask groundMask;
        [SerializeField] float groundCheckDist = 0.25f;
        [SerializeField] float slopeCheckDist = 0.25f;
        [SerializeField] float maxSlopeAngle = 45f;

        Vector3 _direction;
        int _curAirJumps;
        bool _grounded;
        bool _jumping;

        void Reset()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (!_grounded && rb.velocity.y <= downForceStartUpVelocity)
            {
                rb.velocity += Vector3.down * downForceMult;
            }
        }

        void IMovement.Move(IMovementInput input)
        {
            _direction = input.GetDirection().With(y: 0);

            bool inputtingDirection = _direction != Vector3.zero;

            bool running = input.IsRunning();

            float speed = (running ? moveSpeed * runSpeedMult : moveSpeed);
    
            _grounded =  Physics.OverlapSphere(feet.transform.position, feet.radius, groundMask).Length > 0;

            Vector3 moveVector = _direction * speed;
            Vector3 groundVelocity = rb.velocity.With(y: 0);
            Vector3 nextFrameVelocity = groundVelocity + moveVector;

            if (!_grounded || _jumping)
            {
                rb.drag = airDrag;
                float maxAirVel = running ? maxAirVelocityMagnitude * runSpeedMult : maxAirVelocityMagnitude;

                // only allow movement in a direction that doesnt increase forward velocity past the max air vel
                if (nextFrameVelocity.magnitude >= maxAirVel && nextFrameVelocity.magnitude >= groundVelocity.magnitude)
                {
                    moveVector = Vector3.ProjectOnPlane(moveVector, groundVelocity.normalized);
                }

                if (!inputtingDirection)
                {
                    moveVector = -rb.velocity * airNoInputCounteractMult;
                }
            }
            else
            {
                moveVector *= groundSpeedMult;
                rb.drag = groundDrag;
                _curAirJumps = 0;
                //ClimbSteps();
            }
            
            Vector3 bottomOfFeet = feet.transform.position - new Vector3(0, feet.radius, 0);
            bool onSlope = Physics.Raycast(bottomOfFeet, _direction, out RaycastHit slopeHit, slopeCheckDist, groundMask);
            if (onSlope)
            {
                float angle = VectorUtils.DirectionAngle(slopeHit.normal) - 90;
                if (angle > maxSlopeAngle && rb.velocity.y > 0)
                {
                    // moving towards slope
                }
            }
            
            rb.velocity += moveVector.With(y: 0);

            DrawDebug(bottomOfFeet);
        }

        void ClimbSteps()
        {
            Vector3 stepCheckBottom = stepCheck.position;
            Vector3 stepCheckTop = stepCheck.position.Add(y: stepHeight);

            Vector3 direction = rb.velocity.With(y: 0);
            
            if (Physics.Raycast(stepCheckBottom, direction, bottomStepCheckDist, groundMask) && 
                !Physics.Raycast(stepCheckTop, direction, topStepCheckDist, groundMask))
            {
                rb.position += Vector3.zero.With(y: stepSpeed);
            }
        }

        void IMovement.Rotate(IMovementInput input)
        {
            Quaternion rotation = input.GetRotation();
            float rotateSpeed = input.IsRunning() ? runningRotateSpeed : walkingRotateSpeed;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, rotateSpeed);
        }

        void IMovement.Jump()
        {
            if (!jumpDelay.Complete()) return;

            // set jumping true to immediately switch to air drag in movement logic
            _jumping = true;

            Vector3 vel = rb.velocity;
            
            if (_grounded)
            {
                _curAirJumps = 0;
                rb.velocity = vel.With(y: jumpForce);
                return;
            }

            if (_curAirJumps >= maxAirJumps) return;
            _curAirJumps++;

            // only sets y velocity when y velocity is less than potential jump force. Otherwise it would set y vel to a lower value when going faster
            
            if (vel.y <= jumpForce)
            {
                rb.velocity = vel.With(y: jumpForce);
            }
            else
            {
                rb.velocity += Vector3.up * jumpForce;
            }
        }

        void OnCollisionEnter(Collision other)
        {
            _jumping = false;
        }

        void DrawDebug(Vector3 bottomOfFeet)
        {
            Debug.DrawRay(bottomOfFeet, _direction * slopeCheckDist, Color.yellow);
        }
    }
}