using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Ground Control")]
    [SerializeField] float maxSlopeAngle = 45;

    int stepsSinceLastGrounded;

    bool SnapToGround()
    {
      return false;
    }

    void GroundCheck()
    {
      stepsSinceLastGrounded++;
      foreach (ContactPoint contactPoint in contacts)
      {
        Vector3 dir = Curve - contactPoint.point;
        if (!(dir.y > 0f) && !(contactPoint.normal.y > 0.01f))
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
