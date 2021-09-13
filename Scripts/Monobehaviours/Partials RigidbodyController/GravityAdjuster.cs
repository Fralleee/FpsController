using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Gravity adjuster")]
    public float gravityModifier = 2f;
    [SerializeField] float maxFallSpeed = 30f;

    void GravityAdjuster()
    {
      if (isGrounded)
        return;

      if (rigidBody.velocity.y < -maxFallSpeed)
        rigidBody.velocity = rigidBody.velocity.With(y: -maxFallSpeed);
      else
        rigidBody.velocity += Physics.gravity * (gravityModifier - 1) * Time.fixedDeltaTime;
    }
  }
}
