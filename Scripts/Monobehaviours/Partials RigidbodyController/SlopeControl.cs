using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    public event Action<float> OnGroundEnter = delegate { };

    [Header("Ground Control")]
    [SerializeField] float maxAngleWalkable = 35;
    [SerializeField] float maxAngleGrounded = 45;

    bool shouldSlide => slopeAngle > maxAngleWalkable + 1;
    bool shouldNotBeGrounded => slopeAngle > maxAngleGrounded + 1;

    void SlopeControl()
    {
      if (!isGrounded)
        return;

      if (shouldSlide)
      {
        Vector3 downSlopeForce = Vector3.ProjectOnPlane(Physics.gravity * gravityModifier, groundContactNormal);
        if (shouldNotBeGrounded)
        {
          isGrounded = false;
          rigidBody.AddForce(downSlopeForce * ModifiedMovementSpeed * 2f);
        }
        else
          rigidBody.AddForce(downSlopeForce * ModifiedMovementSpeed);
      }
      else
      {
        Vector3 upSlopeForce = Vector3.ProjectOnPlane(-Physics.gravity, groundContactNormal);
        rigidBody.AddForce(upSlopeForce);
      }
    }
  }
}
