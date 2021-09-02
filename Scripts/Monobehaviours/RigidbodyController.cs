using Fralle.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    [Header("Setup")]
    public Camera camera;
    public Transform cameraRig;
    public Transform orientation;
    public Transform body;
    [SerializeField] LayerMask groundLayers;
    [HideInInspector] public Rigidbody rigidBody;
    [HideInInspector] public CapsuleCollider capsule;


    [Header("Status")]
    [ReadOnly] public bool isLocked;
    [ReadOnly] public bool isGrounded;
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
