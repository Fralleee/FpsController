using Fralle.Core;
using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    public event Action OnGroundLeave = delegate { };

    [Header("Jumping")]
    [SerializeField] float jumpHeight = 2f;
    [Readonly] public float ModifiedJumpHeight;

    protected bool jumpButton;

    public bool queueJump;

    void Jumping()
    {
      queueJump = false;
      IsJumping = true;
      IsGrounded = false;
      extraCrouchBoost = true;

      CancelVelocityOnJump();
      RigidBody.AddForce(Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * gravityModifier * ModifiedJumpHeight), ForceMode.VelocityChange);

      OnGroundLeave();
    }

    void ResetJumpingFlag()
    {
      if (IsJumping && RigidBody.velocity.y <= 0)
      {
        IsJumping = false;
        extraCrouchBoost = false;
      }
    }

    void CancelVelocityOnJump()
    {
      if (RigidBody.velocity.y > 0f)
        RigidBody.AddForce(Vector3.up * -RigidBody.velocity.y, ForceMode.VelocityChange);
    }
  }
}
