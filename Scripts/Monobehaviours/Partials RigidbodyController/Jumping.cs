using Fralle.Core;
using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    public event Action OnGroundLeave = delegate { };

    [Header("Jumping")]
    [SerializeField] float baseJumpStrength = 8f;
    [Readonly] public float ModifiedJumpStrength;

    protected bool jumpButton;

    bool queueJump;

    void Jumping()
    {
      queueJump = false;
      IsJumping = true;
      IsGrounded = false;

      Invoke("RemoveJumpFlag", 0.1f);

      CancelVelocityOnJump();
      RigidBody.AddForce(Vector3.up * ModifiedJumpStrength, ForceMode.VelocityChange);

      OnGroundLeave();
    }

    void RemoveJumpFlag()
    {
      IsJumping = false;
    }

    void CancelVelocityOnJump()
    {
      if (RigidBody.velocity.y > 0f)
        RigidBody.AddForce(Vector3.up * -RigidBody.velocity.y, ForceMode.VelocityChange);
    }
  }
}
