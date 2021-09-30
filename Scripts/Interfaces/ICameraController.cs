using UnityEngine;

namespace Fralle.FpsController
{
  public interface ICameraController
  {
    RigidbodyController controller { get; set; }
    void SetOffset(Vector3 offset, float duration = 0.5f);
  }
}
