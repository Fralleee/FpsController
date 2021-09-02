using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    void StickToGroundHelper()
    {
      if (ShouldNotBeGrounded)
        return;

      rigidBody.velocity = Vector3.ProjectOnPlane(rigidBody.velocity, groundContactNormal);
    }
  }
}
