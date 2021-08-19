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

    void SlopeControl()
    {
      RigidBody.useGravity = true;

      if (IsGrounded)
      {
        bool shouldSlideDown = SlopeAngle > maxAngleWalkable + 1;
        RigidBody.useGravity = shouldSlideDown;

        if (shouldSlideDown)
        {
          Vector3 downSlopeForce = Vector3.ProjectOnPlane(Physics.gravity, GroundContactNormal);
          RigidBody.AddForce(downSlopeForce * 5f);

          bool shouldNotBeGrounded = SlopeAngle > maxAngleGrounded + 1;
          if (shouldNotBeGrounded)
            IsGrounded = false;
        }
      }
    }
  }
}
