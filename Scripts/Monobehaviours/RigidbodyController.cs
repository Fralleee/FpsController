using Fralle.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    public event Action OnGroundLeave = delegate { };
    public event Action<bool> OnCrouch = delegate { };

    [HideInInspector] public new Camera camera;
    [HideInInspector] public PlayerCamera playerCamera;
    [HideInInspector] public Transform cameraRig;
    [HideInInspector] public Rigidbody rigidBody;
    [HideInInspector] public CapsuleCollider capsuleCollider;
    [HideInInspector] public Vector3 desiredVelocity;
    [HideInInspector] public Vector2 Movement { get; protected set; }
    [HideInInspector] public Vector2 MouseLook { get; protected set; }
    [HideInInspector] public float movementSpeedProduct;
    [HideInInspector] public int stepsSinceLastGrounded;
    [HideInInspector] public int stepsSinceLastJump;
    [HideInInspector] public bool previouslyGrounded;

    [FoldoutGroup("Status")] [ReadOnly] public float currentMaxMovementSpeed;
    [FoldoutGroup("Status")] [ReadOnly] public float modifiedJumpHeight;
    [FoldoutGroup("Status")] [ReadOnly] public Vector3 groundContactNormal;
    [FoldoutGroup("Status")] [ReadOnly] public float slopeAngle;
    [FoldoutGroup("Status")] [ReadOnly] public bool isLocked;
    [FoldoutGroup("Status")] [ReadOnly] public bool isGrounded;
    [FoldoutGroup("Status")] [ReadOnly] public bool isStable;
    [FoldoutGroup("Status")] [ReadOnly] public bool isMoving;
    [FoldoutGroup("Status")] [ReadOnly] public bool isJumping;
    [FoldoutGroup("Status")] [ReadOnly] public bool isCrouching;

    [FoldoutGroup("Mouse look")] public float mouseSensitivity = 3f;
    [FoldoutGroup("Mouse look")] public float smoothTime = 0.01f;
    [FoldoutGroup("Mouse look")] public float clampY = 90f;

    [FoldoutGroup("Movement")] public float maxMovementSpeed = 7f;
    [FoldoutGroup("Movement")] public float crouchModifier = 0.5f;
    [FoldoutGroup("Movement")] public float groundAcceleration = 7f;
    [FoldoutGroup("Movement")] public float airAcceleration = 0.75f;

    [FoldoutGroup("Ground Control")] public LayerMask groundLayers;
    [FoldoutGroup("Ground Control")] [Range(1, 90f)] public float maxSlopeAngle = 45;
    [FoldoutGroup("Ground Control")] public float groundCheckDistance = 0.1f;
    [FoldoutGroup("Ground Control")] public float snapToGroundProbingDistance = 1;
    [FoldoutGroup("Ground Control")] public int fallTimestepBuffer = 3;
    [FoldoutGroup("Ground Control")] public float gravityModifier = 2f;
    [FoldoutGroup("Ground Control")] public float maxFallSpeed = 30f;

    [FoldoutGroup("Jumping")] public float jumpHeight = 2f;
    [FoldoutGroup("Jumping")] public int jumpTimestepCooldown = 3;

    [FoldoutGroup("Crouching")] public Vector3 standOffset;
    [FoldoutGroup("Crouching")] public Vector3 crouchOffset;
    [FoldoutGroup("Crouching")] public float crouchTime = 0.2f;
    [FoldoutGroup("Crouching")] public float extraJumpBoost = 0.25f;

    protected Animator Animator;
    protected Transform Model;
    protected int AnimIsMoving;
    protected int AnimIsJumping;
    protected int AnimHorizontal;
    protected int AnimVertical;
    protected bool JumpButton;
    protected bool CrouchButton;

    LookRotationTransformer rotationTransformer;
    RaycastHit groundHitInfo;
    Vector2 cameraRotation;
    Vector2 refVelocity;
    Vector3 edgeCompensationNormal;
    bool queueJump;
    bool extraCrouchBoost;
    bool performEdgeCompensation;

    bool PerformSlopeSlideCompensation => isGrounded && isStable && slopeAngle > 0;
    bool CanJump => isGrounded && isStable && JumpButton && !isJumping;
    float GetAcceleration => isGrounded ? groundAcceleration : airAcceleration;
    float GetMaxSpeed => currentMaxMovementSpeed * (isGrounded && isCrouching ? crouchModifier : 1f);

    protected virtual void Awake()
    {
      rigidBody = GetComponent<Rigidbody>();
      capsuleCollider = GetComponent<CapsuleCollider>();
      Animator = GetComponentInChildren<Animator>();
      Model = transform.Find("Model").transform;

      playerCamera = FindObjectOfType<PlayerCamera>();
      playerCamera.controller = this;
      playerCamera.SetOffset(standOffset, 0f);
      cameraRig = playerCamera.transform;
      camera = cameraRig.GetComponentInChildren<Camera>();
      rotationTransformer = cameraRig.GetComponent<LookRotationTransformer>();

      AnimIsMoving = Animator.StringToHash("IsMoving");
      AnimIsJumping = Animator.StringToHash("IsJumping");
      AnimHorizontal = Animator.StringToHash("Horizontal");
      AnimVertical = Animator.StringToHash("Vertical");

      OnValidate();
    }

    protected virtual void Update()
    {
      if (isLocked)
        return;

      JumpInput();
    }

    protected virtual void FixedUpdate()
    {
      if (isLocked)
        return;

      ResetFlags();

      // Grounding
      stepsSinceLastGrounded++;
      stepsSinceLastJump++;
      GroundCheck();
      SnapToGround();
      EdgeAndSlopeHandling();

      desiredVelocity = Utils.GetDesiredVelocity(cameraRig, Movement);

      // Actions
      PerformCrouch();
      PerformJump();
      PerformMovement();

      GravityAdjuster();
      SetFlags();
    }

    void LateUpdate()
    {
      CameraLook();
    }

    void SetFlags()
    {
      movementSpeedProduct = isCrouching ? 0.5f : 1f;
      isMoving = isGrounded && isStable && desiredVelocity.magnitude > 0;

      bool animateFalling = stepsSinceLastGrounded > fallTimestepBuffer;
      Animator.SetBool(AnimIsJumping, animateFalling);
      Animator.SetBool(AnimIsMoving, isMoving);
    }

    void ResetFlags()
    {
      previouslyGrounded = isGrounded;
      isGrounded = false;
      isStable = false;
      slopeAngle = 90;
      groundContactNormal = Vector3.up;

      if (!isJumping || stepsSinceLastJump <= jumpTimestepCooldown)
        return;

      isJumping = false;
      extraCrouchBoost = false;
    }

    void GroundCheck()
    {
      performEdgeCompensation = false;
      Vector3 origin = Utils.GroundCastOrigin(transform.position, capsuleCollider.radius);
      bool groundHit = Physics.SphereCast(origin, capsuleCollider.radius - Physics.defaultContactOffset, Vector3.down, out groundHitInfo, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
      if (groundHit)
      {
        float sphereSlopeAngle = Vector3.Angle(groundHitInfo.normal, Vector3.up);
        float raycastSlopeAngle = 0;
        edgeCompensationNormal = groundHitInfo.normal;
        if (Physics.Raycast(groundHitInfo.point + Vector3.up * 0.01f, Vector3.down, out RaycastHit hitInfo, 0.02f, groundLayers, QueryTriggerInteraction.Ignore))
        {
          raycastSlopeAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
          if (raycastSlopeAngle < sphereSlopeAngle)
            groundHitInfo = hitInfo;
          else
            edgeCompensationNormal = hitInfo.normal;
        }

        isGrounded = true;
        performEdgeCompensation = desiredVelocity.magnitude == 0 && !raycastSlopeAngle.EqualsWithTolerance(sphereSlopeAngle, tolerance: 1f);
        SetGroundedProperties();
      }
    }

    void SnapToGround()
    {
      if (isGrounded)
        return;

      if (isJumping)
        return;

      if (stepsSinceLastGrounded > fallTimestepBuffer)
        return;

      if (!Physics.Raycast(Utils.GroundCastOrigin(transform.position, capsuleCollider.radius), Vector3.down, out groundHitInfo, snapToGroundProbingDistance, groundLayers, QueryTriggerInteraction.Ignore))
        return;

      isGrounded = true;
      rigidBody.velocity = Utils.SnapToGroundVelocity(rigidBody.velocity, groundHitInfo.normal);
      SetGroundedProperties();
    }

    void SetGroundedProperties()
    {
      groundContactNormal = groundHitInfo.normal;
      stepsSinceLastGrounded = 0;
      slopeAngle = Vector3.Angle(groundContactNormal, Vector3.up);
      isStable = slopeAngle < maxSlopeAngle + 1;
    }

    void EdgeAndSlopeHandling()
    {
      if (performEdgeCompensation)
        rigidBody.AddForce(Utils.ProjectOnContactPlane(-Physics.gravity, edgeCompensationNormal));
      else if (PerformSlopeSlideCompensation)
        rigidBody.AddForce(Utils.ProjectOnContactPlane(-Physics.gravity, groundContactNormal));
    }

    void StartCrouch()
    {
      isCrouching = true;
      Utils.SetCapsuleDimensions(capsuleCollider, 0.5f, 1f);
      Model.localScale = new Vector3(1f, 0.5f, 1f);
      playerCamera.SetOffset(crouchOffset, crouchTime);

      if (extraCrouchBoost)
      {
        rigidBody.AddForce(Utils.AddJumpForce(extraJumpBoost, gravityModifier), ForceMode.VelocityChange);
        extraCrouchBoost = false;
      }

      OnCrouch(true);
    }

    void EndCrouch()
    {
      if (Utils.RoofCheck(transform.position, 2f, capsuleCollider.radius, groundLayers))
        return;

      Utils.SetCapsuleDimensions(capsuleCollider, 0.5f, 2f);
      Model.localScale = Vector3.one;
      playerCamera.SetOffset(standOffset, crouchTime);
      isCrouching = false;
      OnCrouch(false);
    }

    void JumpInput()
    {
      if (!CanJump)
        return;

      JumpButton = false;
      queueJump = true;
    }

    void PerformCrouch()
    {
      if (CrouchButton && !isCrouching)
        StartCrouch();
      else if (!CrouchButton && isCrouching)
        EndCrouch();
    }

    void PerformJump()
    {
      if (!queueJump)
        return;

      stepsSinceLastJump = 0;
      queueJump = false;
      isJumping = true;
      isGrounded = false;
      extraCrouchBoost = true;

      rigidBody.AddForce(Vector3.up * -rigidBody.velocity.y, ForceMode.VelocityChange);
      rigidBody.AddForce(Utils.AddJumpForce(modifiedJumpHeight, gravityModifier), ForceMode.VelocityChange);

      OnGroundLeave();
    }

    void PerformMovement()
    {
      float acceleration = GetAcceleration;
      float maxSpeed = GetMaxSpeed;

      Vector3 velocity = rigidBody.velocity;
      Vector3 xAxis = Utils.ProjectOnContactPlane(Vector3.right, groundContactNormal).normalized;
      Vector3 zAxis = Utils.ProjectOnContactPlane(Vector3.forward, groundContactNormal).normalized;

      float currentX = Vector3.Dot(velocity, xAxis);
      float currentZ = Vector3.Dot(velocity, zAxis);
      float newX = Mathf.MoveTowards(currentX, desiredVelocity.x * maxSpeed, acceleration);
      float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z * maxSpeed, acceleration);

      Vector3 movementVelocity = xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

      if (isGrounded && !isStable) // On steep slope
      {
        Vector3 slopeDirection = Utils.ProjectOnContactPlane(Vector3.down, groundContactNormal).normalized;
        movementVelocity += Utils.SlideDownSlope(movementVelocity, slopeDirection);
      }

      rigidBody.velocity = velocity + movementVelocity;
    }

    void GravityAdjuster()
    {
      if (!isGrounded)
        rigidBody.velocity = Utils.ClampedGravity(rigidBody.velocity, maxFallSpeed, gravityModifier);
    }

    void CameraLook()
    {
      cameraRotation = Vector2.SmoothDamp(cameraRotation, new Vector2(cameraRotation.x + MouseLook.x * mouseSensitivity, Mathf.Clamp(cameraRotation.y + MouseLook.y * mouseSensitivity, -clampY, clampY)), ref refVelocity, smoothTime);

      Vector3 rot = cameraRig.transform.rotation.eulerAngles;
      cameraRig.transform.localRotation = Quaternion.Euler(rot.x, cameraRotation.x, rot.z);
      rotationTransformer.ApplyLookRotation(Quaternion.Euler(cameraRotation.y, cameraRotation.x, 0));
    }

    void OnValidate()
    {
      currentMaxMovementSpeed = maxMovementSpeed;
      modifiedJumpHeight = jumpHeight;
    }
  }
}
