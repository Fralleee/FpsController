using Fralle.Core.Extensions;
using Fralle.PingTap;
using UnityEngine;

namespace Fralle.FpsController
{
	public class Tilt : Transformer
	{
		[Header("Speed")]
		[SerializeField] float smoothSpeed = 10f;

		[Header("Strafe rotation")]
		[SerializeField] float strafeRotationAmount = 1.6f;
		[SerializeField] float maxStrafeRotation = 5f;

		RigidbodyController playerController;

		Quaternion currentRotation = Quaternion.identity;

		void Awake()
		{
			playerController = GetComponentInParent<RigidbodyController>();
		}

		public override Vector3 GetPosition() => Vector3.zero;
		public override Quaternion GetRotation() => currentRotation;
		public override void Calculate()
		{
			if (playerController.IsMoving && !playerController.Movement.x.EqualsWithTolerance(0f))
			{
				float strafeAmount = Mathf.Clamp(-playerController.Movement.x * strafeRotationAmount, -maxStrafeRotation, maxStrafeRotation);
				Quaternion strafeRot = Quaternion.Euler(new Vector3(0f, 0f, strafeAmount));
				currentRotation = Quaternion.Lerp(currentRotation, Quaternion.identity * strafeRot, Time.deltaTime * smoothSpeed);
			}
			else
				currentRotation = Quaternion.Lerp(currentRotation, Quaternion.identity, Time.deltaTime * smoothSpeed);
		}
	}
}
