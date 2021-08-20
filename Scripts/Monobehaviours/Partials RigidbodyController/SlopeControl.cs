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

    bool shouldSlide => SlopeAngle > maxAngleWalkable + 1;
    bool shouldNotBeGrounded => SlopeAngle > maxAngleGrounded + 1;

    void SlopeControl()
    {
      if (!IsGrounded)
        return;

      if (shouldSlide)
      {
        Vector3 downSlopeForce = Vector3.ProjectOnPlane(Physics.gravity * gravityModifier, GroundContactNormal);
        if (shouldNotBeGrounded)
        {
          IsGrounded = false;
          RigidBody.AddForce(downSlopeForce * ModifiedMovementSpeed * 2f);
        }
        else
          RigidBody.AddForce(downSlopeForce * ModifiedMovementSpeed);
      }
      else
      {
        Vector3 upSlopeForce = Vector3.ProjectOnPlane(-Physics.gravity, GroundContactNormal);
        RigidBody.AddForce(upSlopeForce);
      }
    }
  }
}
