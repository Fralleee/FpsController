using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    public event Action OnGroundLeave = delegate { };

    [Header("Jumping")]
    [SerializeField] float jumpHeight = 2f;
    [ReadOnly] public float modifiedJumpHeight;

    protected bool JumpButton;

    public bool queueJump;

    void Jumping()
    {
      if (!queueJump)
        return;

      queueJump = false;
      isJumping = true;
      isGrounded = false;
      extraCrouchBoost = true;

      CancelVelocityOnJump();
      rigidBody.AddForce(Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * gravityModifier * modifiedJumpHeight), ForceMode.VelocityChange);

      OnGroundLeave();
    }

    void CancelVelocityOnJump()
    {
      rigidBody.AddForce(Vector3.up * -rigidBody.velocity.y, ForceMode.VelocityChange);
    }
  }
}
