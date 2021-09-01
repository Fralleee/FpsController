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
      isJumping = true;
      isGrounded = false;
      extraCrouchBoost = true;

      CancelVelocityOnJump();
      rigidBody.AddForce(Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * gravityModifier * ModifiedJumpHeight), ForceMode.VelocityChange);

      OnGroundLeave();
    }

    void ResetJumpingFlag()
    {
      if (isJumping && rigidBody.velocity.y <= 0)
      {
        isJumping = false;
        extraCrouchBoost = false;
      }
    }

    void CancelVelocityOnJump()
    {
      if (rigidBody.velocity.y > 0f)
        rigidBody.AddForce(Vector3.up * -rigidBody.velocity.y, ForceMode.VelocityChange);
    }
  }
}
