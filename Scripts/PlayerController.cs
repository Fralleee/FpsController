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
		public new Camera camera;
		public Transform cameraRig;
		public Transform orientation;
		public Transform body;
		[HideInInspector] public Rigidbody rigidBody;
		[HideInInspector] public CapsuleCollider capsule;

		[Header("Flags")]
		public bool IsLocked;
		public bool IsGrounded;
		public bool IsMoving;
		public bool IsJumping;
		public bool IsCrouching;

		[Header("Mouse look")]
		[SerializeField] float mouseSensitivity = 3f;
		[SerializeField] float smoothTime = 0.01f;
		[SerializeField] float clampY = 90f;

		[Header("Movement")]
		[SerializeField] float baseMovementSpeed = 4f;
		[SerializeField] float stopTime = 0.05f;
		[SerializeField] float airControl = 0.5f;
		[Readonly] public float modifiedMovementSpeed;

		[Header("Step Climbing")]
		[SerializeField] float stepHeight = 0.4f;
		[SerializeField] float stepSearchOvershoot = 0.01f;

		[Header("Ground Control")]
		[SerializeField] float maxSlopeAngle = 35;
		[SerializeField] float maxWalkableSlopeAngle = 45;
		[SerializeField] float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded (0.01f seems to work best for this)
		[SerializeField] float stickToGroundHelperDistance = 0.5f; // stops the character

		[Header("Jump")]
		[SerializeField] float baseJumpStrength = 8f;
		[SerializeField] float fallMultiplier = 2.5f;
		[SerializeField] float lowJumpModifier = 2f;
		[Readonly] public float modifiedJumpStrength;

		[Header("Crouching")]
		[SerializeField] float crouchingSpeed = 8f;
		[SerializeField] float crouchHeight = 1f;

		readonly List<ContactPoint> allCPs = new List<ContactPoint>();
		public Vector2 movement { get; private set; }
		public Vector2 mouseLook { get; private set; }
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

		void Awake()
		{
			rigidBody = body.GetComponent<Rigidbody>();
			capsule = body.GetComponent<CapsuleCollider>();

			defaultScale = body.localScale;
			crouchingScale = new Vector3(1, 0.5f, 1);
			crouchHeight = capsule.height * crouchingScale.y * body.localScale.y;
			roofCheckHeight = capsule.height - crouchHeight * 0.5f - 0.01f;

			ConfigureCursor(true);

			if (!camera)
				camera = Camera.main;

			playerInput = GetComponent<PlayerInput>();
			playerInput.actions["Movement"].performed += OnMovement;
			playerInput.actions["Look"].performed += OnLook;
			playerInput.actions["Jump"].canceled += OnJumpCancel;
			playerInput.actions["Jump"].performed += OnJump;
			playerInput.actions["Crouch"].canceled += OnCrouchCancel;
			playerInput.actions["Crouch"].performed += OnCrouch;

			modifiedMovementSpeed = baseMovementSpeed;
			modifiedJumpStrength = baseJumpStrength;
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
				mouseLook = Vector2.zero;
				movement = Vector2.zero;
				jumpButton = false;
				crouchButton = false;
			}

			if (IsLocked)
				return;

			desiredForce = orientation.right * movement.x + orientation.forward * movement.y;
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
			movement = context.ReadValue<Vector2>();
		}

		public void OnLook(InputAction.CallbackContext context)
		{
			mouseLook = context.ReadValue<Vector2>();
		}

		public void OnJumpCancel(InputAction.CallbackContext context)
		{
			jumpButton = false;
		}
		public void OnJump(InputAction.CallbackContext context)
		{
			jumpButton = true;
		}

		public void OnCrouchCancel(InputAction.CallbackContext context)
		{
			crouchButton = false;
		}
		public void OnCrouch(InputAction.CallbackContext context)
		{
			crouchButton = true;
		}
		#endregion

		#region Camera Control
		void CameraLook()
		{
			mouseCoords.y += Mathf.Clamp(mouseLook.y, -clampY, clampY) * mouseSensitivity;
			mouseCoords.x += mouseLook.x * mouseSensitivity;

			currentRotationX = Mathf.SmoothDamp(currentRotationX, mouseCoords.x + affectRotation.x, ref mouseLookDampX, smoothTime);
			currentRotationY = Mathf.SmoothDamp(currentRotationY, mouseCoords.y + affectRotation.y, ref mouseLookDampY, smoothTime);

			affectRotation = Vector2.SmoothDamp(affectRotation, Vector2.zero, ref affectRotation, 0);

			var rot = orientation.transform.rotation.eulerAngles;
			orientation.transform.localRotation = Quaternion.Euler(rot.x, currentRotationX, rot.z);
			cameraRig.localRotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);
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
			var distance = ((capsule.height / 2f) - capsule.radius) + groundCheckDistance;
			if (previouslyGrounded)
				distance += capsule.height * stepHeight;

			if (Physics.SphereCast(body.position, capsule.radius - 0.001f, Vector3.down, out RaycastHit hitInfo, distance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
			{
				IsGrounded = true;
				groundContactNormal = hitInfo.normal;
			}
			else
			{
				IsGrounded = false;
				groundContactNormal = Vector3.up;
			}

			if (previouslyGrounded != IsGrounded)
			{
				if (IsGrounded)
				{
					OnGroundEnter(rigidBody.velocity.y);
					IsJumping = false;
					jumpButton = false;
				}
				else
					OnGroundLeave();
			}
		}

		void SlopeControl()
		{
			rigidBody.useGravity = true;
			if (IsGrounded && rigidBody.velocity.y <= 0.2f)
			{
				var slopeAngle = Vector3.Angle(groundContactNormal, Vector3.up);
				if (slopeAngle > maxWalkableSlopeAngle)
				{
					rigidBody.AddForce(Physics.gravity * 3f);
				}
				if (slopeAngle > maxSlopeAngle + 1f)
				{
					return;
				}

				rigidBody.useGravity = false;
				rigidBody.AddForce(-groundContactNormal * 150f);
			}
		}

		void StickToGroundHelper()
		{
			var distance = ((capsule.height / 2f) - capsule.radius) + stickToGroundHelperDistance;
			if (Physics.SphereCast(body.position, capsule.radius, Vector3.down, out RaycastHit hitInfo, distance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
			{
				if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) >= maxWalkableSlopeAngle)
					return;

				rigidBody.velocity = Vector3.ProjectOnPlane(rigidBody.velocity, hitInfo.normal);
			}
		}
		#endregion
		#region Gravity Adjustments
		void GravityAdjuster()
		{
			if (IsGrounded)
				return;

			if (rigidBody.velocity.y < 0)
				rigidBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
			else if (rigidBody.velocity.y > 0 && !jumpButton)
				rigidBody.velocity += Vector3.up * Physics.gravity.y * (lowJumpModifier - 1) * Time.fixedDeltaTime;
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
			rigidBody.AddForce(desiredForce * modifiedMovementSpeed, ForceMode.Impulse);
			StoppingForcesGround();
		}

		void AirMove()
		{
			rigidBody.AddForce(desiredForce * modifiedMovementSpeed * airControl, ForceMode.Impulse);
			StoppingForcesAir();
		}

		void StoppingForcesGround()
		{
			rigidBody.velocity = Vector3.SmoothDamp(rigidBody.velocity, Vector3.zero, ref damp, stopTime);
		}
		void StoppingForcesAir()
		{
			rigidBody.velocity = Vector3.SmoothDamp(rigidBody.velocity, Vector3.zero, ref damp, stopTime).With(y: rigidBody.velocity.y);
		}
		#endregion
		#region Step Climbing
		void ClimbSteps()
		{
			Vector3 velocity = rigidBody.velocity;
			var grounded = FindGround(out ContactPoint groundCP, allCPs);
			Vector3 stepUpOffset = default;
			bool stepUp = false;
			if (grounded)
				stepUp = FindStep(out stepUpOffset, allCPs, groundCP, velocity);

			if (stepUp)
			{
				rigidBody.position += stepUpOffset;
				rigidBody.velocity = lastVelocity;
			}

			allCPs.Clear();
			lastVelocity = velocity;
		}


		bool FindGround(out ContactPoint groundCP, List<ContactPoint> allCPs)
		{
			groundCP = default;
			var found = false;
			foreach (ContactPoint cp in allCPs)
			{
				if (cp.normal.y > 0.0001f && (!found || cp.normal.y > groundCP.normal.y))
				{
					groundCP = cp;
					found = true;
				}
			}

			return found;
		}

		bool FindStep(out Vector3 stepUpOffset, List<ContactPoint> allCPs, ContactPoint groundCP, Vector3 currVelocity)
		{
			stepUpOffset = default;
			Vector2 velocityXZ = new Vector2(currVelocity.x, currVelocity.z);
			if (velocityXZ.sqrMagnitude < 0.0001f)
				return false;

			foreach (ContactPoint cp in allCPs)
			{
				bool test = ResolveStepUp(out stepUpOffset, cp, groundCP);
				if (test)
					return test;
			}
			return false;
		}

		bool ResolveStepUp(out Vector3 stepUpOffset, ContactPoint stepTestCP, ContactPoint groundCP)
		{
			stepUpOffset = default;
			Collider stepCol = stepTestCP.otherCollider;

			if (Mathf.Abs(stepTestCP.normal.y) >= 0.01f)
			{
				return false;
			}

			if (stepTestCP.point.y - groundCP.point.y >= stepHeight)
			{
				return false;
			}

			float actualStepHeight = groundCP.point.y + stepHeight + 0.0001f;
			Vector3 stepTestInvDir = new Vector3(-stepTestCP.normal.x, 0, -stepTestCP.normal.z).normalized;
			Vector3 origin = new Vector3(stepTestCP.point.x, actualStepHeight, stepTestCP.point.z) + (stepTestInvDir * stepSearchOvershoot);
			Vector3 direction = Vector3.down;
			if (!(stepCol.Raycast(new Ray(origin, direction), out RaycastHit hitInfo, stepHeight)))
			{
				return false;
			}

			Vector3 stepUpPoint = new Vector3(stepTestCP.point.x, hitInfo.point.y + 0.0001f, stepTestCP.point.z) + (stepTestInvDir * stepSearchOvershoot);
			Vector3 stepUpPointOffset = stepUpPoint - new Vector3(stepTestCP.point.x, groundCP.point.y, stepTestCP.point.z);

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
				if (body.localScale != crouchingScale)
				{
					body.localScale = Vector3.Lerp(body.localScale, crouchingScale, Time.deltaTime * crouchingSpeed);
				}
			}
			else if (IsCrouching && !Physics.Raycast(transform.position, Vector3.up, roofCheckHeight))
			{
				IsCrouching = false;
				OnCrouchStateChanged(false);
			}

			if (!IsCrouching && body.localScale != defaultScale)
			{
				body.localScale = Vector3.Lerp(body.localScale, defaultScale, Time.deltaTime * crouchingSpeed);
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
			rigidBody.useGravity = true;
			rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);
			rigidBody.AddForce(Vector3.up * modifiedJumpStrength, ForceMode.VelocityChange);
		}
		#endregion
		#region Limit speed
		void LimitSpeed()
		{
			var horizontalMovement = new Vector3(rigidBody.velocity.x, 0, rigidBody.velocity.z);
			if (horizontalMovement.magnitude <= modifiedMovementSpeed)
				return;

			horizontalMovement = horizontalMovement.normalized * modifiedMovementSpeed;
			rigidBody.velocity = new Vector3(horizontalMovement.x, rigidBody.velocity.y, horizontalMovement.z);
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
			playerInput.actions["Jump"].canceled -= OnJumpCancel;
			playerInput.actions["Jump"].performed -= OnJump;
			playerInput.actions["Crouch"].canceled -= OnCrouchCancel;
			playerInput.actions["Crouch"].performed -= OnCrouch;
		}
	}
}
