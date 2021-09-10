using Fralle.Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    [Header("Setup")]
    [SerializeField] LayerMask groundLayers;

    [HideInInspector] public new Camera camera;
    [HideInInspector] public PlayerCamera playerCamera;
    [HideInInspector] public Transform cameraRig;
    [HideInInspector] public Rigidbody rigidBody;
    [HideInInspector] public CapsuleCollider capsuleCollider;

    [Header("Status")]
    [ReadOnly] public bool isLocked;
    [ReadOnly] public bool isGrounded;
    [ReadOnly] public bool isStable;
    [ReadOnly] public bool isMoving;
    [ReadOnly] public bool isJumping;
    [ReadOnly] public bool isCrouching;
    [ReadOnly] public float slopeAngle;
    [ReadOnly] public Vector3 groundContactNormal;
    [ReadOnly] public bool previouslyGrounded;

    protected Animator Animator;
    protected Transform Model;
    protected int AnimIsMoving;
    protected int AnimIsJumping;
    protected int AnimHorizontal;
    protected int AnimVertical;

    List<ContactPoint> contacts = new List<ContactPoint>();
    Vector3 Bottom => capsuleCollider.bounds.center - Vector3.up * capsuleCollider.bounds.extents.y;
    Vector3 Top => capsuleCollider.bounds.center + Vector3.up * capsuleCollider.bounds.extents.y;
    Vector3 Curve => Bottom + Vector3.up * capsuleCollider.radius * 0.5f;

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

      if (!isGrounded || !JumpButton || isJumping)
        return;

      JumpButton = false;
      queueJump = true;

    }

    protected virtual void FixedUpdate()
    {
      if (isLocked)
        return;

      ResetFlags();
      GroundCheck();
      SnapToGround();

      // Where we want to move
      SetDesiredVelocity();
      SlopeControl();

      // Speed modifiers
      Crouch();
      Jumping();
      GravityAdjuster();

      // Perform movement
      Move();

      contacts.Clear();
    }

    public void ResetFlags()
    {
      previouslyGrounded = isGrounded;
      if (stepsSinceLastGrounded > fallTimestepBuffer)
        isGrounded = false;
      isStable = false;
      slopeAngle = 90;
      groundContactNormal = Vector3.up;

      if (!isJumping || !(rigidBody.velocity.y <= 0))
        return;

      isJumping = false;
      extraCrouchBoost = false;
    }

    void LateUpdate()
    {
      CameraLook();
    }

    void OnValidate()
    {
      currentMaxMovementSpeed = maxMovementSpeed;
      modifiedJumpHeight = jumpHeight;
    }
  }
}
