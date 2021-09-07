using Fralle.Core;
using Sirenix.OdinInspector;
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
    [HideInInspector] public CapsuleCollider capsule;

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

    protected virtual void Awake()
    {
      rigidBody = GetComponent<Rigidbody>();
      capsule = GetComponent<CapsuleCollider>();
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

      isStable = false;

      ResetJumpingFlag();
      SlopeControl();

      Crouch();
      Move();

      if (queueJump)
        Jumping();

      GravityAdjuster();
      LimitSpeed();

      GroundChange();
      ResetGroundVariables();
    }

    void GroundChange()
    {
      if (previouslyGrounded != isGrounded)
        Animator.SetBool(AnimIsJumping, !isGrounded);

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
      modifiedMovementSpeed = baseMovementSpeed;
      modifiedJumpHeight = jumpHeight;
    }
  }
}
