using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public class TiltTransformer : LocalTransformer, IRotator
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

    public Quaternion GetRotation() => currentRotation;
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