using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    void GroundCheck()
    {
      if (contacts != null)
      {
        foreach (ContactPoint contactPoint in contacts)
        {
          Vector3 dir = Curve - contactPoint.point;
          if (!(dir.y > 0f) && !(contactPoint.normal.y > 0.01f))
            continue;

          groundContactNormal = contactPoint.normal;
          slopeAngle = Mathf.Min(slopeAngle, Vector3.Angle(contactPoint.normal, Vector3.up));
          isGrounded = true;
        }
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
        Vector3 dir = Curve - contactPoint.point;
        Gizmos.DrawLine(contactPoint.point, dir);
      }

      Gizmos.color = Color.red;
      Gizmos.DrawSphere(Bottom, .05f);
      Gizmos.DrawSphere(Curve, .05f);
    }

  }
}
