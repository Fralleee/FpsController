using UnityEngine;

namespace Fralle.FpsController
{
  public interface ICameraController
  {
    void SetOffset(Vector3 offset, float duration = 0.5f);
  }
}
