using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Ground Control")]
    [SerializeField] float maxSlopeAngle = 45;
    [SerializeField] float fallTimestepBuffer = 3;
    [SerializeField] float snapToGroundProbingDistance = 0.5f;

    int stepsSinceLastGrounded;

    void SnapToGround()
    {
      Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 0.5f, Color.red);
      if (isJumping)
        return;

      if (stepsSinceLastGrounded > fallTimestepBuffer)
        return;


      if (!Physics.Raycast(transform.position + Vector3.up * 0.01f, Vector3.down * snapToGroundProbingDistance, out RaycastHit hit))
        return;

      Vector3 dir = Curve - hit.point;
      if (dir.y < 0f && hit.normal.y < 0.01f)
        return;

      float contactSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
      groundContactNormal = hit.normal;
      slopeAngle = contactSlopeAngle;
      stepsSinceLastGrounded = 0;
      isGrounded = true;

      Vector3 velocity = rigidBody.velocity;
      float speed = velocity.magnitude;
      float dot = Vector3.Dot(velocity, hit.normal);
      if (dot > 0f)
        rigidBody.velocity = (velocity - hit.normal * dot).normalized * speed;
    }

    void GroundCheck()
    {
      stepsSinceLastGrounded++;
      foreach (ContactPoint contactPoint in contacts)
      {
        Vector3 dir = Curve - contactPoint.point;
        if (dir.y < 0f && contactPoint.normal.y < 0.01f)
          continue;

        float contactSlopeAngle = Vector3.Angle(contactPoint.normal, Vector3.up);
        if (contactSlopeAngle < slopeAngle)
        {
          groundContactNormal = contactPoint.normal;
          slopeAngle = contactSlopeAngle;
        }

        stepsSinceLastGrounded = 0;
        isGrounded = true;
      }

      if (previouslyGrounded != isGrounded)
        Animator.SetBool(AnimIsJumping, !isGrounded);
    }

    void OnCollisionStay(Collision collision)
    {
      if (isJumping)
        return;

      contacts.AddRange(collision.contacts);
    }

    void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.blue;
      foreach (ContactPoint contactPoint in contacts)
      {
        Gizmos.DrawSphere(contactPoint.point, .25f);
      }
    }
  }
}
