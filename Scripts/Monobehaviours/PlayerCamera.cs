using Fralle.Core;
using Fralle.FpsController;
using UnityEngine;

public class PlayerCamera : MasterPositioner
{
  public Transform playerTransform;
  public RigidbodyController controller;

  Vector3 lastOffset;
  Vector3 currentOffset;
  Vector3 desiredOffset;

  float lerpDuration;
  float timeElapsed;

  public void SetOffset(Vector3 offset, float time = 0.5f)
  {
    if (time == 0f)
    {
      currentOffset = offset;
      return;
    }

    lastOffset = currentOffset;
    desiredOffset = offset;
    lerpDuration = time;
    timeElapsed = 0;
  }

  public override Vector3 GetPosition() => playerTransform.position + currentOffset;

  private void Update()
  {
    if (timeElapsed < lerpDuration)
    {
      currentOffset = Vector3.Slerp(lastOffset, desiredOffset, timeElapsed / lerpDuration);
      timeElapsed += Time.deltaTime;
    }
  }

}
