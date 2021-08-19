using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {

    void LimitSpeed()
    {
      Vector3 horizontalMovement = new Vector3(RigidBody.velocity.x, 0, RigidBody.velocity.z);
      if (horizontalMovement.magnitude <= ModifiedMovementSpeed)
        return;

      horizontalMovement = horizontalMovement.normalized * ModifiedMovementSpeed;
      RigidBody.velocity = new Vector3(horizontalMovement.x, RigidBody.velocity.y, horizontalMovement.z);
    }
  }
}
