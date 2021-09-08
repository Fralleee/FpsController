using Fralle.Core;
using Fralle.FpsController;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

public class PlayerCamera : MasterPositioner
{
  [ReadOnly] public RigidbodyController controller;

  public Transform weaponCamera;
  public Transform weaponHolder;

  Coroutine lerp;
  Vector3 lastOffset;
  Vector3 currentOffset;
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

  public override Vector3 GetPosition() => controller.transform.position + currentOffset;

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
