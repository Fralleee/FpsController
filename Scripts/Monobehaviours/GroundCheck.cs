using Fralle.FpsController;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
  RigidbodyController rigidbodyController;
  CapsuleCollider capsuleCollider;

  ContactPoint[] contacts;

  Vector3 bottom => capsuleCollider.bounds.center - (Vector3.up * capsuleCollider.bounds.extents.y);
  Vector3 curve => bottom + (Vector3.up * capsuleCollider.radius * 0.5f);

  void Awake()
  {
    rigidbodyController = GetComponentInParent<RigidbodyController>();
    capsuleCollider = GetComponent<CapsuleCollider>();
  }

  void OnCollisionStay(Collision collision)
  {
    if (rigidbodyController.IsJumping)
      return;

    contacts = collision.contacts;
    foreach (ContactPoint contactPoint in contacts)
    {
      Vector3 dir = curve - contactPoint.point;
      if (dir.y > 0f || contactPoint.normal.y > 0.01f)
      {
        rigidbodyController.GroundContactNormal = contactPoint.normal;
        rigidbodyController.SlopeAngle = Mathf.Min(rigidbodyController.SlopeAngle, Vector3.Angle(contactPoint.normal, Vector3.up));
        rigidbodyController.IsGrounded = true;
      }
    }
  }

  //void OnDrawGizmosSelected()
  //{
  //  Gizmos.color = Color.blue;
  //  foreach (ContactPoint contactPoint in contacts)
  //  {
  //    Gizmos.DrawSphere(contactPoint.point, .25f);
  //    Vector3 dir = curve - contactPoint.point;
  //    Gizmos.DrawLine(contactPoint.point, dir);
  //  }

  //  Gizmos.color = Color.red;
  //  Gizmos.DrawSphere(bottom, .05f);
  //  Gizmos.DrawSphere(curve, .05f);
  //}
}