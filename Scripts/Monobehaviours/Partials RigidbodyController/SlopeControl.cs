using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Ground Control")]
    [SerializeField] float maxAngleWalkable = 35;
    [SerializeField] float maxAngleGrounded = 45;

    bool ShouldSlide => slopeAngle > maxAngleWalkable + 1;
    bool ShouldNotBeGrounded => slopeAngle > maxAngleGrounded + 1;

    void SlopeControl()
    {
      if (!isGrounded)
        return;

      if (ShouldSlide)
      {
        Vector3 downSlopeForce = Vector3.ProjectOnPlane(Physics.gravity * gravityModifier, groundContactNormal);
        if (ShouldNotBeGrounded)
        {
          isGrounded = false;
          rigidBody.AddForce(downSlopeForce * modifiedMovementSpeed * 2f);
        }
        else
          rigidBody.AddForce(downSlopeForce * modifiedMovementSpeed);
      }
      else
      {
        Vector3 upSlopeForce = Vector3.ProjectOnPlane(-Physics.gravity, groundContactNormal);
        rigidBody.AddForce(upSlopeForce);
      }
    }
  }
}
