using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    void StickToGroundHelper()
    {
      if (shouldNotBeGrounded)
        return;

      RigidBody.velocity = Vector3.ProjectOnPlane(RigidBody.velocity, GroundContactNormal);
    }
  }
}
