using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {

    void LimitSpeed()
    {
      Vector3 horizontalMovement = new Vector3(rigidBody.velocity.x, 0, rigidBody.velocity.z);
      if (horizontalMovement.magnitude <= ModifiedMovementSpeed)
        return;

      horizontalMovement = horizontalMovement.normalized * ModifiedMovementSpeed;
      rigidBody.velocity = new Vector3(horizontalMovement.x, rigidBody.velocity.y, horizontalMovement.z);
    }
  }
}
