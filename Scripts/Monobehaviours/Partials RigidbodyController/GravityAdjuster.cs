using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    [Header("Gravity adjuster")]
    public float gravityModifier = 2f;

    void GravityAdjuster()
    {
      if (isGrounded)
        return;

      rigidBody.velocity += Physics.gravity * (gravityModifier - 1) * Time.fixedDeltaTime;
    }
  }
}
