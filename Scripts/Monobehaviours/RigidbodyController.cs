using Fralle.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    [FormerlySerializedAs("Camera")]
    [Header("Setup")]
    public Camera camera;
    [FormerlySerializedAs("CameraRig")] public Transform cameraRig;
    [FormerlySerializedAs("Orientation")] public Transform orientation;
    [FormerlySerializedAs("Body")] public Transform body;
    [SerializeField] LayerMask groundLayers;
    [FormerlySerializedAs("RigidBody")] [HideInInspector] public Rigidbody rigidBody;
    [FormerlySerializedAs("Capsule")] [HideInInspector] public CapsuleCollider capsule;

    [FormerlySerializedAs("IsLocked")]
    [Header("Status")]
    [Readonly] public bool isLocked;
    [FormerlySerializedAs("IsGrounded")] [Readonly] public bool isGrounded;
    [FormerlySerializedAs("IsMoving")] [Readonly] public bool isMoving;
    [FormerlySerializedAs("IsJumping")] [Readonly] public bool isJumping;
    [FormerlySerializedAs("IsCrouching")] [Readonly] public bool isCrouching;
    [FormerlySerializedAs("SlopeAngle")] [Readonly] public float slopeAngle;
    [FormerlySerializedAs("GroundContactNormal")] [Readonly] public Vector3 groundContactNormal;
    [FormerlySerializedAs("PreviouslyGrounded")] [Readonly] public bool previouslyGrounded;

    protected Animator Animator;
    protected Transform Model;
    protected int AnimIsMoving;
    protected int AnimIsJumping;
    protected int AnimHorizontal;
    protected int AnimVertical;
    protected int DefaultLayer;

    protected virtual void Awake()
    {
      DefaultLayer = LayerMask.NameToLayer("Default");

      rigidBody = body.GetComponent<Rigidbody>();
      capsule = body.GetComponent<CapsuleCollider>();
      Animator = body.GetComponentInChildren<Animator>();
      Model = body.Find("Model").transform;

      if (!camera)
        camera = Camera.main;

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

      if (isGrounded && jumpButton && !isJumping)
      {
        jumpButton = false;
        queueJump = true;
      }
    }

    protected virtual void FixedUpdate()
    {
      if (isLocked)
        return;

      ResetJumpingFlag();
      desiredForce = orientation.right * Movement.x + orientation.forward * Movement.y;

      Move();
      Crouch();

      if (queueJump)
        Jumping();

      GravityAdjuster();
      LimitSpeed();
      SlopeControl();
      StickToGroundHelper();

      GroundChange();
      ResetGroundVariables();
    }

    void GroundChange()
    {
      if (previouslyGrounded != isGrounded)
        Animator.SetBool(AnimIsJumping, !isGrounded);

      if (isGrounded && !previouslyGrounded)
      {
        OnGroundEnter(rigidBody.velocity.y);
      }

      previouslyGrounded = isGrounded;
    }

    public void ResetGroundVariables()
    {
      isGrounded = false;
      slopeAngle = 90;
    }

    void LateUpdate()
    {
      CameraLook();
    }

    void OnValidate()
    {
      ModifiedMovementSpeed = baseMovementSpeed;
      ModifiedJumpHeight = jumpHeight;
    }
  }
}
