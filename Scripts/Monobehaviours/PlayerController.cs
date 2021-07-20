using Fralle.Core.Attributes;
using Fralle.Core.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fralle.FpsController
{
	public class PlayerController : MonoBehaviour
	{
		public event Action<float> OnGroundEnter = delegate { };
		public event Action OnGroundLeave = delegate { };
		public event Action<bool> OnCrouchStateChanged = delegate { };

		[Header("Setup")]
		public Camera Camera;
		public Transform CameraRig;
		public Transform Orientation;
		public Transform Body;
		[HideInInspector] public Rigidbody RigidBody;
		[HideInInspector] public CapsuleCollider Capsule;

		[Header("Flags")]
		[Readonly] public bool IsLocked;
		[Readonly] public bool IsGrounded;
		[Readonly] public bool IsMoving;
		[Readonly] public bool IsJumping;
		[Readonly] public bool IsCrouching;

		[Header("Mouse look")]
		[SerializeField] float mouseSensitivity = 3f;
		[SerializeField] float smoothTime = 0.01f;
		[SerializeField] float clampY = 90f;

		[Header("Movement")]
		[SerializeField] float baseMovementSpeed = 4f;
		[SerializeField] float stopTime = 0.05f;
		[SerializeField] float airControl = 0.5f;
		[Readonly] public float ModifiedMovementSpeed;

		[Header("Step Climbing")]
		[SerializeField] float stepHeight = 0.4f;
		[SerializeField] float stepSearchOvershoot = 0.01f;

		[Header("Ground Control")]
		[SerializeField] float maxSlopeGlideAngle = 35;
		[SerializeField] float maxWalkableSlopeAngle = 45;
		[SerializeField] float groundCheckDistance = 0.1f;

		[Header("Jump")]
		[SerializeField] float baseJumpStrength = 8f;
		[SerializeField] float fallMultiplier = 2.5f;
		[SerializeField] float lowJumpModifier = 2f;
		[Readonly] public float ModifiedJumpStrength;

		[Header("Crouching")]
		[SerializeField] float crouchingSpeed = 8f;
		[SerializeField] float crouchHeight = 1f;

		readonly List<ContactPoint> allCPs = new List<ContactPoint>();
		public Vector2 Movement { get; private set; }
		public Vector2 MouseLook { get; private set; }
		PlayerInput playerInput;
		Vector3 lastVelocity;
		Vector3 desiredForce;
		Vector3 groundContactNormal;
		Vector3 damp;
		Vector3 crouchingScale;
		Vector3 defaultScale;
		Vector2 affectRotation = Vector2.zero;
		Vector2 mouseCoords;
		bool previouslyGrounded;
		bool queueJump;
		bool jumpButton;
		bool crouchButton;
		float currentRotationX;
		float currentRotationY;
		float mouseLookDampX;
		float mouseLookDampY;
		float roofCheckHeight;
		float slopeMultiplier = 1f;
		readonly float slopeGlideMax = 100f;
		[Readonly] public float SlopeAngle;
		float ActualGroundCheckDistance => (Capsule.height / 2f) - Capsule.radius + groundCheckDistance;

		void Awake()
		{
			RigidBody = Body.GetComponent<Rigidbody>();
			Capsule = Body.GetComponent<CapsuleCollider>();

			defaultScale = Body.localScale;
			crouchingScale = new Vector3(1, 0.5f, 1);
			crouchHeight = Capsule.height * crouchingScale.y * Body.localScale.y;
			roofCheckHeight = Capsule.height - crouchHeight * 0.5f - 0.01f;

			ConfigureCursor();

			if (!Camera)
				Camera = Camera.main;

			playerInput = GetComponent<PlayerInput>();
			playerInput.actions["Movement"].performed += OnMovement;
			playerInput.actions["Look"].performed += OnLook;
			playerInput.actions["Jump"].performed += OnJump;
			playerInput.actions["Crouch"].performed += OnCrouch;

			ModifiedMovementSpeed = baseMovementSpeed;
			ModifiedJumpStrength = baseJumpStrength;
		}

		void Update()
		{
			if (IsLocked)
				return;

			if (IsGrounded && jumpButton)
			{
				queueJump = true;
			}
		}

		void FixedUpdate()
		{
			if (!playerInput.inputIsActive)
			{
				MouseLook = Vector2.zero;
				Movement = Vector2.zero;
				jumpButton = false;
				crouchButton = false;
			}

			if (IsLocked)
				return;

			desiredForce = Orientation.right * Movement.x + Orientation.forward * Movement.y;
			GroundedCheck();

			if (IsGrounded)
			{
				SlopeControl();
				ClimbSteps();
			}

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

		#region Input
		public void OnMovement(InputAction.CallbackContext context)
		{
			Movement = context.ReadValue<Vector2>();
		}

		public void OnLook(InputAction.CallbackContext context)
		{
			MouseLook = context.ReadValue<Vector2>();
		}

		public void OnJump(InputAction.CallbackContext context)
		{
			jumpButton = context.ReadValueAsButton();
		}

		public void OnCrouch(InputAction.CallbackContext context)
		{
			crouchButton = context.ReadValueAsButton();
		}
		#endregion

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

		public static void ConfigureCursor(bool doLock = true)
		{
			if (doLock)
			{
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
			}
			else
			{
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
		}
		#endregion
		#region Ground Control
		void GroundedCheck()
		{
			previouslyGrounded = IsGrounded;

			Debug.DrawLine(Body.position + Capsule.center, Body.position + Capsule.center + Vector3.down * (ActualGroundCheckDistance + Capsule.radius - 0.001f));

			if (Physics.SphereCast(Body.position + Capsule.center, Capsule.radius - 0.001f, Vector3.down, out RaycastHit hitInfo, ActualGroundCheckDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
			{
				if (Physics.Raycast(Body.position + Capsule.center, Vector3.down, out RaycastHit raycastHit,
					(ActualGroundCheckDistance + Capsule.radius - 0.001f), Physics.AllLayers, QueryTriggerInteraction.Ignore))
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

			RigidBody.useGravity = !IsGrounded;
			if (previouslyGrounded == IsGrounded)
				return;
			if (IsGrounded)
			{
				OnGroundEnter(RigidBody.velocity.y);
				IsJumping = false;
				jumpButton = false;
			}
			else
				OnGroundLeave();
		}

		void SlopeControl()
		{
			SlopeAngle = Vector3.Angle(groundContactNormal, Vector3.up);

			if (Physics.Raycast(Orientation.position, desiredForce, out RaycastHit raycastHit, Capsule.radius,
				Physics.AllLayers, QueryTriggerInteraction.Ignore))
			{
				float forwardSlopeAngle = Vector3.Angle(raycastHit.normal, Vector3.up);

				if (forwardSlopeAngle >= 89) // not a slope
					slopeMultiplier = 1f;
				else if (forwardSlopeAngle >= maxWalkableSlopeAngle + 1)
					slopeMultiplier = 0.25f;
				else
					slopeMultiplier = Mathf.Clamp(1 - (forwardSlopeAngle - (maxSlopeGlideAngle + 1)) / ((maxWalkableSlopeAngle + 1) - (maxSlopeGlideAngle + 1)), 0.5f, 1);
			}
			else
			{
				slopeMultiplier = 1f;
			}


			if (SlopeAngle <= 0)
				return;

			if (SlopeAngle > maxWalkableSlopeAngle + 1)
			{
				RigidBody.AddForce(Vector3.down * slopeGlideMax, ForceMode.Acceleration);
				return;
			}

			if (SlopeAngle > maxSlopeGlideAngle + 1f)
			{
				float factor = SlopeAngle / maxWalkableSlopeAngle;
				RigidBody.AddForce(Vector3.down * slopeGlideMax * factor, ForceMode.Acceleration);
				return;
			}

			if (!(RigidBody.velocity.y >= -0.2f))
				return;

			RigidBody.useGravity = false;
			RigidBody.AddForce(-groundContactNormal * 150f);
		}

		void StickToGroundHelper()
		{
			if (Mathf.Abs(Vector3.Angle(groundContactNormal, Vector3.up)) >= maxWalkableSlopeAngle)
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
			}
			else
			{
				AirMove();
			}
		}

		void GroundMove()
		{
			desiredForce = Vector3.ProjectOnPlane(desiredForce, groundContactNormal).normalized;
			RigidBody.AddForce(desiredForce * ModifiedMovementSpeed * slopeMultiplier, ForceMode.Impulse);
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
		#region Step Climbing
		void ClimbSteps()
		{
			Vector3 velocity = RigidBody.velocity;
			bool grounded = FindGround(out ContactPoint groundCp, allCPs);
			Vector3 stepUpOffset = default;
			bool stepUp = false;
			if (grounded)
				stepUp = FindStep(out stepUpOffset, allCPs, groundCp, velocity);

			if (stepUp)
			{
				RigidBody.position += stepUpOffset;
				RigidBody.velocity = lastVelocity;
			}

			allCPs.Clear();
			lastVelocity = velocity;
		}


		static bool FindGround(out ContactPoint groundCp, IEnumerable<ContactPoint> allCPs)
		{
			groundCp = default;
			bool found = false;
			foreach (ContactPoint cp in allCPs)
			{
				if (!(cp.normal.y > 0.0001f) || (found && !(cp.normal.y > groundCp.normal.y)))
					continue;
				groundCp = cp;
				found = true;
			}

			return found;
		}

		bool FindStep(out Vector3 stepUpOffset, IEnumerable<ContactPoint> allContactPoints, ContactPoint groundCp, Vector3 currVelocity)
		{
			stepUpOffset = default;
			Vector2 velocityXz = new Vector2(currVelocity.x, currVelocity.z);
			if (velocityXz.sqrMagnitude < 0.0001f)
				return false;

			foreach (ContactPoint cp in allContactPoints)
			{
				bool test = ResolveStepUp(out stepUpOffset, cp, groundCp);
				if (test)
					return true;
			}
			return false;
		}

		bool ResolveStepUp(out Vector3 stepUpOffset, ContactPoint stepTestCp, ContactPoint groundCp)
		{
			stepUpOffset = default;
			Collider stepCol = stepTestCp.otherCollider;

			if (Mathf.Abs(stepTestCp.normal.y) >= 0.01f)
			{
				return false;
			}

			if (stepTestCp.point.y - groundCp.point.y >= stepHeight)
			{
				return false;
			}

			float actualStepHeight = groundCp.point.y + stepHeight + 0.0001f;
			Vector3 stepTestInvDir = new Vector3(-stepTestCp.normal.x, 0, -stepTestCp.normal.z).normalized;
			Vector3 origin = new Vector3(stepTestCp.point.x, actualStepHeight, stepTestCp.point.z) + (stepTestInvDir * stepSearchOvershoot);
			Vector3 direction = Vector3.down;
			if (!(stepCol.Raycast(new Ray(origin, direction), out RaycastHit hitInfo, stepHeight)))
			{
				return false;
			}

			Vector3 stepUpPoint = new Vector3(stepTestCp.point.x, hitInfo.point.y + 0.0001f, stepTestCp.point.z) + (stepTestInvDir * stepSearchOvershoot);
			Vector3 stepUpPointOffset = stepUpPoint - new Vector3(stepTestCp.point.x, groundCp.point.y, stepTestCp.point.z);

			stepUpOffset = stepUpPointOffset;
			return true;
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
			else if (IsCrouching && !Physics.Raycast(transform.position, Vector3.up, roofCheckHeight))
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
			RigidBody.useGravity = true;
			RigidBody.velocity = new Vector3(RigidBody.velocity.x, 0f, RigidBody.velocity.z);
			RigidBody.AddForce(Vector3.up * ModifiedJumpStrength * slopeMultiplier, ForceMode.VelocityChange);
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

		void OnCollisionEnter(Collision col)
		{
			allCPs.AddRange(col.contacts);
		}

		void OnCollisionStay(Collision col)
		{
			allCPs.AddRange(col.contacts);
		}

		void OnDestroy()
		{
			playerInput.actions["Movement"].performed -= OnMovement;
			playerInput.actions["Look"].performed -= OnLook;
			playerInput.actions["Jump"].performed -= OnJump;
			playerInput.actions["Crouch"].performed -= OnCrouch;
		}

		void OnValidate()
		{
			ModifiedMovementSpeed = baseMovementSpeed;
			ModifiedJumpStrength = baseJumpStrength;
		}
	}
}
