using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public static class Utils
  {
    public static void SetCapsuleDimensions(CapsuleCollider capsuleCollider, float height)
    {
      capsuleCollider.height = height;
      capsuleCollider.center = new Vector3(0f, height * 0.5f, 0f);
    }

    public static bool RoofCheck(Vector3 position, float heightOffset, float radius, int layerMask)
    {
      return Physics.CheckSphere(position + Vector3.up * (heightOffset - radius - Physics.defaultContactOffset), radius - Physics.defaultContactOffset, layerMask);
    }

    public static Vector3 ProjectOnContactPlane(Vector3 velocity, Vector3 normal) => velocity - normal * Vector3.Dot(velocity, normal);

    public static Vector3 ClampedFallSpeed(Vector3 velocity, float maxFallSpeed) => velocity.y < -maxFallSpeed ? velocity.With(y: -maxFallSpeed) : velocity;

    public static Vector3 SnapToGroundVelocity(Vector3 velocity, Vector3 normal)
    {
      float dot = Vector3.Dot(velocity, normal);
      return (dot > 0f) ? (velocity - normal * dot).normalized * velocity.magnitude : velocity;
    }

    public static Vector3 SlideDownSlope(Vector3 velocity, Vector3 slopeDirection)
    {
      float clampedDot = Mathf.Max(0, Vector3.Dot(velocity.normalized, -slopeDirection));
      return slopeDirection * clampedDot * velocity.magnitude;
    }

    public static Vector3 AddJumpForce(float power) => Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * power);

    public static Vector3 GroundCastOrigin(Vector3 position, float radius) => position + Vector3.up * (radius + Physics.defaultContactOffset);

    public static Vector3 GetDesiredVelocity(Transform orientation, Vector2 input)
    {
      Vector3 right = new Vector3(orientation.right.x, 0, orientation.right.z).normalized;
      Vector3 forward = Quaternion.Euler(0, -90, 0) * right;
      return right * input.x + forward * input.y;
    }
  }
}
