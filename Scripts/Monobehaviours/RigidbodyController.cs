using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    [Header("Setup")]
    public Camera Camera;
    public Transform CameraRig;
    public Transform Orientation;
    public Transform Body;
    [SerializeField] LayerMask groundLayers;
    [HideInInspector] public Rigidbody RigidBody;
    [HideInInspector] public CapsuleCollider Capsule;

    [Header("Status")]
    [Readonly] public bool IsLocked;
    [Readonly] public bool IsGrounded;
    [Readonly] public bool IsMoving;
    [Readonly] public bool IsJumping;
    [Readonly] public bool IsCrouching;
    [Readonly] public float SlopeAngle;
    [Readonly] public Vector3 GroundContactNormal;
    [Readonly] public bool PreviouslyGrounded;

    protected Animator animator;
    protected int animIsMoving;
    protected int animIsJumping;
    protected int animHorizontal;
    protected int animVertical;

    protected virtual void Awake()
    {
      RigidBody = Body.GetComponent<Rigidbody>();
      Capsule = Body.GetComponent<CapsuleCollider>();
      animator = Body.GetComponentInChildren<Animator>();

      defaultScale = Body.localScale;
      crouchingScale = new Vector3(1, 0.5f, 1);
      crouchHeight = Capsule.height * crouchingScale.y * Body.localScale.y;
      roofCheckHeight = Capsule.height - crouchHeight * 0.5f - 0.01f;

      if (!Camera)
        Camera = Camera.main;

      rotationTransformer = CameraRig.GetComponent<LookRotationTransformer>();

      ModifiedMovementSpeed = baseMovementSpeed;
      ModifiedJumpStrength = baseJumpStrength;

      animIsMoving = Animator.StringToHash("IsMoving");
      animIsJumping = Animator.StringToHash("IsJumping");
      animHorizontal = Animator.StringToHash("Horizontal");
      animVertical = Animator.StringToHash("Vertical");
    }

    protected virtual void Update()
    {
      if (IsLocked)
        return;

      if (IsGrounded && jumpButton && !IsJumping)
      {
        jumpButton = false;
        queueJump = true;
      }
    }

    protected virtual void FixedUpdate()
    {
      if (IsLocked)
        return;

      desiredForce = Orientation.right * Movement.x + Orientation.forward * Movement.y;

      Move();
      Crouch();

      if (queueJump)
        Jumping();

      GravityAdjuster();
      LimitSpeed();
      SlopeControl();

      if (IsGrounded)
        StickToGroundHelper();

      GroundChange();
      ResetGroundVariables();
    }

    void GroundChange()
    {
      if (PreviouslyGrounded != IsGrounded)
        animator.SetBool(animIsJumping, !IsGrounded);

      if (IsGrounded && !PreviouslyGrounded)
      {
        OnGroundEnter(RigidBody.velocity.y);
      }

      PreviouslyGrounded = IsGrounded;
    }

    public void ResetGroundVariables()
    {
      IsGrounded = false;
      SlopeAngle = 90;
    }

    void LateUpdate()
    {
      CameraLook();
    }

    void OnValidate()
    {
      ModifiedMovementSpeed = baseMovementSpeed;
      ModifiedJumpStrength = baseJumpStrength;
    }
  }
}
