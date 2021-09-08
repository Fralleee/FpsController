using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Ground Control")]
    [SerializeField] float maxSlopeAngle = 45;


    void SlopeControl()
    {
      if (!isGrounded)
        return;

      isStable = slopeAngle < maxSlopeAngle + 1;
    }
  }
}
