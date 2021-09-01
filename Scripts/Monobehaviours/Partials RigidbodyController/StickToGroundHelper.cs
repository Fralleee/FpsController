using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    void StickToGroundHelper()
    {
      if (shouldNotBeGrounded)
        return;

      rigidBody.velocity = Vector3.ProjectOnPlane(rigidBody.velocity, groundContactNormal);
    }
  }
}
