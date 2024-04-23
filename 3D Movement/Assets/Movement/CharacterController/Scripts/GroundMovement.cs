using NuiN.NExtensions;
using UnityEngine;

namespace NuiN.Movement
{
    public class GroundMovement : MonoBehaviour, IMovement
    {
        [Header("Dependencies")]
        [SerializeField] Rigidbody rb;
        [SerializeField] SphereCollider feet;

        [Header("Move Speed Settings")]
        [SerializeField] float moveSpeed = 0.375f;
        [SerializeField] float runSpeedMult = 1.5f;

        [SerializeField] float maxAirVelocityMagnitude = 6.2f;

        [SerializeField] float groundSpeedMult = 1.8f;
        [SerializeField] float groundDrag = 10f;
        [SerializeField] float airDrag = 0.002f;
        
        [Header("Rotate Speed Settings")]
        [SerializeField] float walkingRotateSpeed = 99999f;
        [SerializeField] float runningRotateSpeed = 99999f;

        [Header("Jump Settings")] 
        [SerializeField] SimpleTimer jumpDelay = new(0.2f);
        [SerializeField] float jumpForce = 8f;
        [SerializeField] int maxAirJumps = 1;
        [SerializeField] float downForceMult = 0.1f;
        [SerializeField] float downForceStartUpVelocity = 0.1f;

        [Header("Environment Settings")]
        [SerializeField] LayerMask groundMask;
        [SerializeField] float groundCheckDist = 0.25f;
        [SerializeField] float slopeCheckDist = 0.25f;
        //[SerializeField] float maxSlopeAngle = 45f;
        
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
            Vector3 direction = input.GetDirection().With(y: 0);

            bool running = input.IsRunning();

            float speed = (running ? moveSpeed * runSpeedMult : moveSpeed);
    
            _grounded =  Physics.OverlapSphere(feet.transform.position, feet.radius, groundMask).Length > 0;

            Vector3 bottomOfFeet = feet.transform.position - new Vector3(0, feet.radius / 2, 0);
            bool onSlope = Physics.Raycast(bottomOfFeet, direction, out RaycastHit slopeHit, slopeCheckDist, groundMask);
            Debug.Log(onSlope);

            Vector3 moveVector = direction * speed;
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
            }
            else
            {
                moveVector *= groundSpeedMult;
                rb.drag = groundDrag;
                _curAirJumps = 0;
            }
    
            rb.velocity += moveVector.With(y: 0);

            DrawDebug(bottomOfFeet, direction);
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

        void DrawDebug(Vector3 bottomOfFeet, Vector3 direction)
        {
            Debug.DrawRay(bottomOfFeet, direction * slopeCheckDist, Color.yellow);
        }
    }
}