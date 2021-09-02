using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    public event Action OnGroundLeave = delegate { };

    [Header("Jumping")]
    [SerializeField] float jumpHeight = 2f;
    [FormerlySerializedAs("ModifiedJumpHeight")] [ReadOnly] public float modifiedJumpHeight;

    protected bool JumpButton;

    public bool queueJump;

    void Jumping()
    {
      queueJump = false;
      isJumping = true;
      isGrounded = false;
      extraCrouchBoost = true;

      CancelVelocityOnJump();
      rigidBody.AddForce(Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * gravityModifier * modifiedJumpHeight), ForceMode.VelocityChange);

      OnGroundLeave();
    }

    void ResetJumpingFlag()
    {
      if (!isJumping || !(rigidBody.velocity.y <= 0))
        return;

      isJumping = false;
      extraCrouchBoost = false;
    }

    void CancelVelocityOnJump()
    {
      if (rigidBody.velocity.y > 0f)
        rigidBody.AddForce(Vector3.up * -rigidBody.velocity.y, ForceMode.VelocityChange);
    }
  }
}
