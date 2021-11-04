using UnityEngine;

namespace Fralle.FpsController
{
  public class MovingPlatform : MonoBehaviour
  {

    // Check oncollisionstay to see if collision is actually on top of

    void OnCollisionEnter(Collision collision)
    {
      if (collision.gameObject.TryGetComponent(out Rigidbody rb))
        rb.transform.SetParent(transform);
    }

    void OnCollisionExit(Collision collision)
    {
      if (collision.gameObject.TryGetComponent(out Rigidbody rb))
      {
        rb.transform.SetParent(null);
      }
    }
  }
}
