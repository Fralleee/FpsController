using Fralle.Core;
using Fralle.Core.Extensions;
using System;
using UnityEngine;

namespace Fralle.FpsController
{
	public class RigidbodyController : MonoBehaviour
	{
		public event Action<float> OnGroundEnter = delegate { };
		public event Action OnGroundLeave = delegate { };
		public event Action<bool> OnCrouchStateChanged = delegate { };

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

		[Header("Mouse look")]
		[SerializeField] float mouseSensitivity = 3f;
		[SerializeField] float smoothTime = 0.01f;
		[SerializeField] float clampY = 90f;

		[Header("Movement")]
		[SerializeField] float baseMovementSpeed = 4f;
		[SerializeField] float stopTime = 0.05f;
		[SerializeField] float airControl = 0.5f;
		[Readonly] public float ModifiedMovementSpeed;

		[Header("Ground Control")]
		[SerializeField] float maxAngleWalkable = 35;
		[SerializeField] float maxAngleGrounded = 45;
		[SerializeField] float groundCheckDistance = 0.1f;

		[Header("Jump")]
		[SerializeField] float baseJumpStrength = 8f;
		[SerializeField] float fallMultiplier = 2.5f;
		[SerializeField] float lowJumpModifier = 2f;
		[Readonly] public float ModifiedJumpStrength;

		[Header("Crouching")]
		[SerializeField] float crouchingSpeed = 8f;
		[SerializeField] float crouchHeight = 1f;

		public Vector2 Movement { get; protected set; }
		public Vector2 MouseLook { get; protected set; }
		protected Animator animator;
		Vector3 desiredForce;
		Vector3 groundContactNormal;
		Vector3 damp;
		Vector3 crouchingScale;
		Vector3 defaultScale;
		Vector2 affectRotation = Vector2.zero;
		Vector2 mouseCoords;
		bool previouslyGrounded;
		bool queueJump;
		protected bool jumpButton;
		protected bool crouchButton;
		float currentRotationX;
		float currentRotationY;
		float mouseLookDampX;
		float mouseLookDampY;
		float roofCheckHeight;
		float ActualGroundCheckDistance => (Capsule.height / 2f) - Capsule.radius + groundCheckDistance;
		int animIsMoving;
		int animIsJumping;
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

			if (IsGrounded && jumpButton)
			{
				queueJump = true;
			}
		}

		protected virtual void FixedUpdate()
		{
			if (IsLocked)
				return;

			desiredForce = Orientation.right * Movement.x + Orientation.forward * Movement.y;
			GroundedCheck();

			//if (IsGrounded)
			//{
			//	SlopeControl();
			//}

			Move();
			Crouch();
			Jump();
			GravityAdjuster();
			LimitSpeed();

			if (previouslyGrounded && !IsJumping)
			{
				StickToGroundHelper();
			}
		}

		void LateUpdate()
		{
			CameraLook();
		}

		#region Camera Control
		void CameraLook()
		{
			mouseCoords.y += MouseLook.y * mouseSensitivity;
			mouseCoords.y = Mathf.Clamp(mouseCoords.y, -clampY, clampY);
			mouseCoords.x += MouseLook.x * mouseSensitivity;

			currentRotationX = Mathf.SmoothDamp(currentRotationX, mouseCoords.x + affectRotation.x, ref mouseLookDampX, smoothTime);
			currentRotationY = Mathf.SmoothDamp(currentRotationY, mouseCoords.y + affectRotation.y, ref mouseLookDampY, smoothTime);

			affectRotation = Vector2.SmoothDamp(affectRotation, Vector2.zero, ref affectRotation, 0);

			Vector3 rot = Orientation.transform.rotation.eulerAngles;
			Orientation.transform.localRotation = Quaternion.Euler(rot.x, currentRotationX, rot.z);
			CameraRig.localRotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);
		}

		#endregion
		#region Ground Control
		void GroundedCheck()
		{
			previouslyGrounded = IsGrounded;
			SlopeAngle = 0;
			RigidBody.useGravity = true;

			Debug.DrawLine(Body.position + Capsule.center, Body.position + Capsule.center + Vector3.down * (ActualGroundCheckDistance + Capsule.radius - 0.001f));

			if (Physics.SphereCast(Body.position + Capsule.center, Capsule.radius - 0.001f, Vector3.down, out RaycastHit hitInfo, ActualGroundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore))
			{
				if (Physics.Raycast(Body.position + Capsule.center, Vector3.down, out RaycastHit raycastHit,
					(ActualGroundCheckDistance + Capsule.radius - 0.001f), groundLayers, QueryTriggerInteraction.Ignore))
				{
					groundContactNormal = raycastHit.normal;
				}
				else
				{
					groundContactNormal = hitInfo.normal;
				}
				IsGrounded = true;
			}
			else
			{
				IsGrounded = false;
				groundContactNormal = Vector3.up;
			}

			animator.SetBool(animIsJumping, !IsGrounded);

			if (IsGrounded)
			{

				if (previouslyGrounded != IsGrounded)
				{
					OnGroundEnter(RigidBody.velocity.y);
					IsJumping = false;
					jumpButton = false;
				}

				SlopeAngle = Vector3.Angle(groundContactNormal, Vector3.up);
				RigidBody.useGravity = SlopeAngle > maxAngleWalkable + 1;

				bool shouldSlideDown = SlopeAngle > maxAngleWalkable + 1;
				if (shouldSlideDown)
				{
					var downSlopeForce = Vector3.ProjectOnPlane(Physics.gravity, groundContactNormal);
					RigidBody.AddForce(downSlopeForce * 5f);

					bool shouldNotBeGrounded = SlopeAngle > maxAngleGrounded + 1;
					if (shouldNotBeGrounded)
						IsGrounded = false;
				}

			}

		}

		void StickToGroundHelper()
		{
			if (Mathf.Abs(SlopeAngle) >= maxAngleWalkable)
				return;

			RigidBody.velocity = Vector3.ProjectOnPlane(RigidBody.velocity, groundContactNormal);
		}
		#endregion
		#region Gravity Adjustments
		void GravityAdjuster()
		{
			if (IsGrounded)
				return;

			if (RigidBody.velocity.y < 0)
				RigidBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
			else if (RigidBody.velocity.y > 0 && !jumpButton)
				RigidBody.velocity += Vector3.up * Physics.gravity.y * (lowJumpModifier - 1) * Time.fixedDeltaTime;
		}
		#endregion
		#region Movement
		void Move()
		{
			if (IsGrounded)
			{
				GroundMove();
				IsMoving = desiredForce.magnitude > 0; // also check if not blocked

				animator.SetBool(animIsMoving, IsMoving);
			}
			else
			{
				AirMove();
			}
		}

		void GroundMove()
		{
			desiredForce = Vector3.ProjectOnPlane(desiredForce, groundContactNormal).normalized;
			RigidBody.AddForce(desiredForce * ModifiedMovementSpeed, ForceMode.Impulse);
			StoppingForcesGround();
		}

		void AirMove()
		{
			RigidBody.AddForce(desiredForce * ModifiedMovementSpeed * airControl, ForceMode.Impulse);
			StoppingForcesAir();
		}

		void StoppingForcesGround()
		{
			RigidBody.velocity = Vector3.SmoothDamp(RigidBody.velocity, Vector3.zero, ref damp, stopTime);
		}
		void StoppingForcesAir()
		{
			RigidBody.velocity = Vector3.SmoothDamp(RigidBody.velocity, Vector3.zero, ref damp, stopTime).With(y: RigidBody.velocity.y);
		}
		#endregion
		#region Crouching
		void Crouch()
		{
			if (crouchButton)
			{
				if (!IsCrouching)
					OnCrouchStateChanged(true);

				IsCrouching = true;
				if (Body.localScale != crouchingScale)
				{
					Body.localScale = Vector3.Lerp(Body.localScale, crouchingScale, Time.deltaTime * crouchingSpeed);
				}
			}
			else if (IsCrouching && !Physics.Raycast(transform.position, Vector3.up, roofCheckHeight, groundLayers))
			{
				IsCrouching = false;
				OnCrouchStateChanged(false);
			}

			if (!IsCrouching && Body.localScale != defaultScale)
			{
				Body.localScale = Vector3.Lerp(Body.localScale, defaultScale, Time.deltaTime * crouchingSpeed);
			}
		}
		#endregion
		#region Jumping
		void Jump()
		{
			if (!queueJump)
				return;

			queueJump = false;
			IsJumping = true;
			IsGrounded = false;
			RigidBody.velocity = new Vector3(RigidBody.velocity.x, 0f, RigidBody.velocity.z);
			RigidBody.AddForce(Vector3.up * ModifiedJumpStrength, ForceMode.VelocityChange);

			OnGroundLeave();
		}
		#endregion
		#region Limit speed
		void LimitSpeed()
		{
			Vector3 horizontalMovement = new Vector3(RigidBody.velocity.x, 0, RigidBody.velocity.z);
			if (horizontalMovement.magnitude <= ModifiedMovementSpeed)
				return;

			horizontalMovement = horizontalMovement.normalized * ModifiedMovementSpeed;
			RigidBody.velocity = new Vector3(horizontalMovement.x, RigidBody.velocity.y, horizontalMovement.z);
		}
		#endregion

		void OnValidate()
		{
			ModifiedMovementSpeed = baseMovementSpeed;
			ModifiedJumpStrength = baseJumpStrength;
		}
	}
}
