using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMotor : MonoBehaviour
{
    [Header("Movement")]
    public bool useRootMotion = false;
    public bool rotateByWorld = false;
    public bool useContinuousSprint = true;
    public bool sprintOnlyFree = true;
    public enum LocomotionType
    {
        FreeWithStrafe,
        OnlyStrafe,
        OnlyFree,
    }
    public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;

    public MovementSpeed freeSpeed, strafeSpeed;

    [Header("Airborne")]

    public bool jumpWithRigidbodyForce = false;
    public bool jumpAndRotate = true;
    public float jumpTimer = 0.3f;
    public float jumpHeight = 4f;

    public float airSpeed = 5f;
    public float airSmooth = 6f;
    public float extraGravity = -10f;
    [HideInInspector]
    public float limitFallVelocity = -15f;

    [Header("Ground")]
    public LayerMask groundLayer = 1 << 0;
    public float groundMinDistance = 0.25f;
    public float groundMaxDistance = 0.5f;
    [Range(30, 80)] public float slopeLimit = 75f;

    internal Animator animator;
    internal Rigidbody _rigidbody;                                                      // access the Rigidbody component
    internal PhysicMaterial frictionPhysics, maxFrictionPhysics, slippyPhysics;         // create PhysicMaterial for the Rigidbody
    internal CapsuleCollider _capsuleCollider;                                          // access CapsuleCollider information

    // movement bools
    internal bool isJumping;
    internal bool isStrafing
    {
        get
        {
            return _isStrafing;
        }
        set
        {
            _isStrafing = value;
        }
    }
    internal bool isGrounded { get; set; }
    internal bool isSprinting { get; set; }
    public bool stopMove { get; protected set; }

    internal float inputMagnitude;                      // sets the inputMagnitude to update the animations in the animator controller
    internal float verticalSpeed;                       // set the verticalSpeed based on the verticalInput
    internal float horizontalSpeed;                     // set the horizontalSpeed based on the horizontalInput       
    internal float moveSpeed;                           // set the current moveSpeed for the MoveCharacter method
    internal float verticalVelocity;                    // set the vertical velocity of the rigidbody
    internal float colliderRadius, colliderHeight;      // storage capsule collider extra information        
    internal float heightReached;                       // max height that character reached in air;
    internal float jumpCounter;                         // used to count the routine to reset the jump
    internal float groundDistance;                      // used to know the distance from the ground
    internal RaycastHit groundHit;                      // raycast to hit the ground 
    internal bool lockMovement = false;                 // lock the movement of the controller (not the animation)
    internal bool lockRotation = false;                 // lock the rotation of the controller (not the animation)        
    internal bool _isStrafing;                          // internally used to set the strafe movement                
    internal Transform rotateTarget;                    // used as a generic reference for the camera.transform
    internal Vector3 input;                             // generate raw input for the controller
    internal Vector3 colliderCenter;                    // storage the center of the capsule collider info                
    internal Vector3 inputSmooth;                       // generate smooth input based on the inputSmooth value       
    internal Vector3 moveDirection;                     // used to know the direction you're moving 

    public void Init()
    {
        animator = GetComponent<Animator>();
        animator.updateMode = AnimatorUpdateMode.AnimatePhysics;

        frictionPhysics = new PhysicMaterial();
        frictionPhysics.name = "frictionPhysics";
        frictionPhysics.staticFriction = .25f;
        frictionPhysics.dynamicFriction = .25f;
        frictionPhysics.frictionCombine = PhysicMaterialCombine.Multiply;

        maxFrictionPhysics = new PhysicMaterial();
        maxFrictionPhysics.name = "maxFrictionPhysics";
        maxFrictionPhysics.staticFriction = 1f;
        maxFrictionPhysics.dynamicFriction = 1f;
        maxFrictionPhysics.frictionCombine = PhysicMaterialCombine.Maximum;

        slippyPhysics = new PhysicMaterial();
        slippyPhysics.name = "slippyPhysics";
        slippyPhysics.staticFriction = 0f;
        slippyPhysics.dynamicFriction = 0f;
        slippyPhysics.frictionCombine = PhysicMaterialCombine.Minimum;

        _rigidbody = GetComponent<Rigidbody>();

        _capsuleCollider = GetComponent<CapsuleCollider>();

        colliderCenter = GetComponent<CapsuleCollider>().center;
        colliderRadius = GetComponent<CapsuleCollider>().radius;
        colliderHeight = GetComponent<CapsuleCollider>().height;

        isGrounded = true;
    }

    public virtual void UpdateMotor()
    {
        CheckGround();
        CheckSlopeLimit();
        ControlJumpBehaviour();
        AirControl();
    }

    #region Locomotion

    public virtual void SetControllerMoveSpeed(MovementSpeed speed)
    {
        if (speed.walkByDefault)
            moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.runningSpeed : speed.walkSpeed, speed.movementSmooth * Time.deltaTime);
        else
            moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.sprintSpeed : speed.runningSpeed, speed.movementSmooth * Time.deltaTime);
    }

    public virtual void MoveCharacter(Vector3 _direction)
    {
        inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

        if (!isGrounded || isJumping) return;

        _direction.y = 0;
        _direction.x = Mathf.Clamp(_direction.x, -1f, 1f);
        _direction.z = Mathf.Clamp(_direction.z, -1f, 1f);

        if (_direction.magnitude > 1f)
            _direction.Normalize();

        Vector3 targetPosition = (useRootMotion ? animator.rootPosition : _rigidbody.position) + _direction * (stopMove ? 0 : moveSpeed) * Time.deltaTime;
        Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

        bool useVerticalVelocity = true;
        if (useVerticalVelocity) targetVelocity.y = _rigidbody.velocity.y;
        _rigidbody.velocity = targetVelocity;
    }

    public virtual void CheckSlopeLimit()
    {
        if (input.sqrMagnitude < 0.1) return;

        RaycastHit hitinfo;
        var hitAngle = 0f;

        if (Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), transform.position + moveDirection.normalized * (_capsuleCollider.radius + 0.2f), out hitinfo, groundLayer))
        {
            hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

            var targetPoint = hitinfo.point + moveDirection.normalized * _capsuleCollider.radius;
            if ((hitAngle > slopeLimit) && Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), targetPoint, out hitinfo, groundLayer))
            {
                hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                if (hitAngle > slopeLimit && hitAngle < 85f)
                {
                    stopMove = true;
                    return;
                }
            }
        }
        stopMove = false;
    }

    public virtual void RotateToPosition(Vector3 position)
    {
        Vector3 desiredDirection = position - transform.position;
        RotateToDirection(desiredDirection.normalized);
    }

    public virtual void RotateToDirection(Vector3 direction)
    {
        RotateToDirection(direction, isStrafing ? strafeSpeed.rotationSpeed : freeSpeed.rotationSpeed);
    }

    public virtual void RotateToDirection(Vector3 direction, float rotationSpeed)
    {
        if (!jumpAndRotate && !isGrounded) return;
        direction.y = 0f;
        Vector3 desiredForward = Vector3.RotateTowards(transform.forward, direction.normalized, rotationSpeed * Time.deltaTime, .1f);
        Quaternion _newRotation = Quaternion.LookRotation(desiredForward);
        transform.rotation = _newRotation;
    }

    #endregion

    #region Jump Methods

    protected virtual void ControlJumpBehaviour()
    {
        if (!isJumping) return;

        jumpCounter -= Time.deltaTime;
        if (jumpCounter <= 0)
        {
            jumpCounter = 0;
            isJumping = false;
        }

        var vel = _rigidbody.velocity;
        vel.y = jumpHeight;
        _rigidbody.velocity = vel;
    }

    public virtual void AirControl()
    {
        if ((isGrounded && !isJumping)) return;
        if (transform.position.y > heightReached) heightReached = transform.position.y;
        inputSmooth = Vector3.Lerp(inputSmooth, input, airSmooth * Time.deltaTime);

        if (jumpWithRigidbodyForce && !isGrounded)
        {
            _rigidbody.AddForce(moveDirection * airSpeed * Time.deltaTime, ForceMode.VelocityChange);
            return;
        }

        moveDirection.y = 0;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -1f, 1f);
        moveDirection.z = Mathf.Clamp(moveDirection.z, -1f, 1f);

        Vector3 targetPosition = _rigidbody.position + (moveDirection * airSpeed) * Time.deltaTime;
        Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

        targetVelocity.y = _rigidbody.velocity.y;
        _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, targetVelocity, airSmooth * Time.deltaTime);
    }

    protected virtual bool jumpFwdCondition
    {
        get
        {
            Vector3 p1 = transform.position + _capsuleCollider.center + Vector3.up * -_capsuleCollider.height * 0.5F;
            Vector3 p2 = p1 + Vector3.up * _capsuleCollider.height;
            return Physics.CapsuleCastAll(p1, p2, _capsuleCollider.radius * 0.5f, transform.forward, 0.6f, groundLayer).Length == 0;
        }
    }

    #endregion

    #region Ground Check                

    protected virtual void CheckGround()
    {
        CheckGroundDistance();
        ControlMaterialPhysics();

        if (groundDistance <= groundMinDistance)
        {
            isGrounded = true;
            if (!isJumping && groundDistance > 0.05f)
                _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);

            heightReached = transform.position.y;
        }
        else
        {
            if (groundDistance >= groundMaxDistance)
            {
                isGrounded = false;
                verticalVelocity = _rigidbody.velocity.y;

                if (!isJumping)
                {
                    _rigidbody.AddForce(transform.up * extraGravity * Time.deltaTime, ForceMode.VelocityChange);
                }
            }
            else if (!isJumping)
            {
                _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
            }
        }
    }

    protected virtual void ControlMaterialPhysics()
    {
        _capsuleCollider.material = (isGrounded && GroundAngle() <= slopeLimit + 1) ? frictionPhysics : slippyPhysics;

        if (isGrounded && input == Vector3.zero)
            _capsuleCollider.material = maxFrictionPhysics;
        else if (isGrounded && input != Vector3.zero)
            _capsuleCollider.material = frictionPhysics;
        else
            _capsuleCollider.material = slippyPhysics;
    }

    protected virtual void CheckGroundDistance()
    {
        if (_capsuleCollider != null)
        {
            float radius = _capsuleCollider.radius * 0.9f;
            var dist = 10f;
            Ray ray2 = new Ray(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down);

            if (Physics.Raycast(ray2, out groundHit, (colliderHeight / 2) + dist, groundLayer) && !groundHit.collider.isTrigger)
                dist = transform.position.y - groundHit.point.y;

            if (dist >= groundMinDistance)
            {
                Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                Ray ray = new Ray(pos, -Vector3.up);
                if (Physics.SphereCast(ray, radius, out groundHit, _capsuleCollider.radius + groundMaxDistance, groundLayer) && !groundHit.collider.isTrigger)
                {
                    Physics.Linecast(groundHit.point + (Vector3.up * 0.1f), groundHit.point + Vector3.down * 0.15f, out groundHit, groundLayer);
                    float newDist = transform.position.y - groundHit.point.y;
                    if (dist > newDist) dist = newDist;
                }
            }
            groundDistance = (float)System.Math.Round(dist, 2);
        }
    }

    public virtual float GroundAngle()
    {
        var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
        return groundAngle;
    }

    public virtual float GroundAngleFromDirection()
    {
        var dir = isStrafing && input.magnitude > 0 ? (transform.right * input.x + transform.forward * input.z).normalized : transform.forward;
        var movementAngle = Vector3.Angle(dir, groundHit.normal) - 90;
        return movementAngle;
    }

    #endregion

    [System.Serializable]
    public class MovementSpeed
    {
        [Range(1f, 20f)]
        public float movementSmooth = 6f;
        [Range(0f, 1f)]
        public float animationSmooth = 0.2f;
        public float rotationSpeed = 16f;
        public bool walkByDefault = false;
        public bool rotateWithCamera = false;
        public float walkSpeed = 2f;
        public float runningSpeed = 4f;
        public float sprintSpeed = 6f;
    }
}

