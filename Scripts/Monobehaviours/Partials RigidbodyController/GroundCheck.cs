using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Ground Control")]
    [SerializeField] float maxSlopeAngle = 45;
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] float snapToGroundProbingDistance = 1;
    [SerializeField] int fallTimestepBuffer = 3;

    int stepsSinceLastGrounded;
    RaycastHit groundHitInfo;
    Vector3 origin => transform.position + Vector3.up * (capsuleCollider.radius + Physics.defaultContactOffset);

    void SnapToGround()
    {
      if (isGrounded)
        return;

      if (isJumping)
        return;

      if (stepsSinceLastGrounded > fallTimestepBuffer)
        return;

      if (!Physics.Raycast(origin, Vector3.down, out groundHitInfo, snapToGroundProbingDistance, groundLayers, QueryTriggerInteraction.Ignore))
        return;

      stepsSinceLastGrounded = 0;
      isGrounded = true;
      SetSlope(groundHitInfo);

      Vector3 velocity = rigidBody.velocity;
      float speed = velocity.magnitude;
      float dot = Vector3.Dot(velocity, groundHitInfo.normal);
      if (dot > 0f)
        rigidBody.velocity = (velocity - groundHitInfo.normal * dot).normalized * speed;
    }

    void GroundCheck()
    {
      bool groundHit = Physics.SphereCast(origin, capsuleCollider.radius - Physics.defaultContactOffset, Vector3.down, out groundHitInfo, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
      if (groundHit)
      {
        stepsSinceLastGrounded = 0;
        isGrounded = true;

        if (Physics.Raycast(groundHitInfo.point + Vector3.up * 0.01f, Vector3.down, out RaycastHit hitInfo, 0.02f, groundLayers, QueryTriggerInteraction.Ignore))
        {
          float sphereSlopeAngle = Vector3.Angle(groundHitInfo.normal, Vector3.up);
          float raycastSlopeAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
          if (raycastSlopeAngle < sphereSlopeAngle)
            groundHitInfo = hitInfo;
        }

        SetSlope(groundHitInfo);
      }
    }

    void SetSlope(RaycastHit hitInfo)
    {
      groundContactNormal = hitInfo.normal;
      slopeAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
      isStable = slopeAngle < maxSlopeAngle + 1;
    }
  }
}
