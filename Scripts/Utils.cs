using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public static class Utils
  {
    static Collider[] overlappedColliders = new Collider[8];

    public static Vector3 ProjectOnContactPlane(Vector3 velocity, Vector3 normal) => velocity - normal * Vector3.Dot(velocity, normal);

    public static Vector3 ClampedGravity(Vector3 velocity, float maxFallSpeed, float gravityModifier)
    {
      return velocity.y < -maxFallSpeed ? velocity.With(y: -maxFallSpeed) : velocity + Physics.gravity * (gravityModifier - 1) * Time.fixedDeltaTime;
    }

    public static Vector3 SnapToGroundVelocity(Vector3 velocity, Vector3 normal)
    {
      float dot = Vector3.Dot(velocity, normal);
      return (dot > 0f) ? (velocity - normal * dot).normalized * velocity.magnitude : velocity;
    }

    public static Vector3 SlopeCounterForce(RigidbodyController controller)
    {
      return (controller.isGrounded && controller.isStable) ? controller.rigidBody.velocity + ProjectOnContactPlane(-Physics.gravity, controller.groundContactNormal) : controller.rigidBody.velocity;
    }

    public static Vector3 SlideDownSlope(Vector3 velocity, Vector3 slopeDirection)
    {
      float clampedDot = Mathf.Max(0, Vector3.Dot(velocity.normalized, -slopeDirection));
      return slopeDirection * clampedDot * velocity.magnitude;
    }

    public static Vector3 AddJumpForce(float power, float gravityModifier) => Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * gravityModifier * power);

    public static CapsuleConfiguration SetCapsuleDimensions(float radius, float height)
    {
      height = Mathf.Clamp(height, radius * 2f, height);
      return new CapsuleConfiguration
      {
        height = height,
        center = new Vector3(0f, height * 0.5f, 0f),
        radius = radius,
        bottom = Vector3.zero,
        top = new Vector3(0f, height, 0f)
      };
    }
    public static bool CapsuleCollisionsOverlap(CapsuleConfiguration capsule, int layerMask)
    {
      int hits = Physics.OverlapCapsuleNonAlloc(
        capsule.bottom + Vector3.up * capsule.radius,
        capsule.top + Vector3.down * capsule.radius * 1.01f,
        capsule.radius,
        overlappedColliders,
        layerMask,
        QueryTriggerInteraction.Ignore);
      return hits > 0;
    }

    public static Vector3 GroundCastOrigin(Vector3 position, float radius) => position + Vector3.up * (radius + Physics.defaultContactOffset);

    public static Vector3 GetDesiredVelocity(Transform orientation, Vector2 input)
    {
      Vector3 right = new Vector3(orientation.right.x, 0, orientation.right.z).normalized;
      Vector3 forward = Quaternion.Euler(0, -90, 0) * right;
      return right * input.x + forward * input.y;
    }
  }
}
