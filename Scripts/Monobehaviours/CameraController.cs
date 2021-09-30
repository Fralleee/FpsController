using System.Collections;
using UnityEngine;

namespace Fralle.FpsController
{
  public class CameraController : MonoBehaviour, ICameraController
  {
    public RigidbodyController controller { get; set; }

    protected Vector3 currentOffset;

    Coroutine lerp;
    Vector3 lastOffset;
    Vector3 desiredOffset;

    public void SetOffset(Vector3 offset, float duration = 0.5f)
    {
      if (lerp != null)
        StopCoroutine(lerp);

      if (duration == 0f)
      {
        currentOffset = offset;
        return;
      }

      lastOffset = currentOffset;
      desiredOffset = offset;
      lerp = StartCoroutine(Lerp(duration));
    }

    IEnumerator Lerp(float lerpDuration)
    {
      float timeElapsed = 0;
      while (timeElapsed < lerpDuration)
      {
        currentOffset = Vector3.Slerp(lastOffset, desiredOffset, timeElapsed / lerpDuration);
        timeElapsed += Time.deltaTime;

        yield return null;
      }

      currentOffset = desiredOffset;
    }
  }
}
