using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Ground Control")]
    [SerializeField] float maxSlopeAngle = 45;


    void SlopeControl()
    {
      if (!isGrounded)
        return;

      isStable = slopeAngle < maxSlopeAngle + 1;

      //if (isStable)
      //{
      //  Vector3 upSlopeForce = Vector3.ProjectOnPlane(-Physics.gravity, groundContactNormal);
      //  rigidBody.AddForce(upSlopeForce);
      //}
      //else
      //{
      //  Vector3 downSlopeForce = Vector3.ProjectOnPlane(Physics.gravity * gravityModifier, groundContactNormal);
      //  rigidBody.AddForce(downSlopeForce * modifiedMovementSpeed);
      //}
    }
  }
}
