using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    [Header("Mouse look")]
    [SerializeField] float mouseSensitivity = 3f;
    [SerializeField] float smoothTime = 0.01f;
    [SerializeField] float clampY = 90f;

    public Vector2 MouseLook { get; protected set; }

    LookRotationTransformer rotationTransformer;
    Vector2 mouseCoords;
    float currentRotationX;
    float currentRotationY;
    float mouseLookDampX;
    float mouseLookDampY;

    void CameraLook()
    {
      mouseCoords.y += MouseLook.y * mouseSensitivity;
      mouseCoords.y = Mathf.Clamp(mouseCoords.y, -clampY, clampY);
      mouseCoords.x += MouseLook.x * mouseSensitivity;

      currentRotationX = Mathf.SmoothDamp(currentRotationX, mouseCoords.x, ref mouseLookDampX, smoothTime);
      currentRotationY = Mathf.SmoothDamp(currentRotationY, mouseCoords.y, ref mouseLookDampY, smoothTime);

      Vector3 rot = Orientation.transform.rotation.eulerAngles;
      Orientation.transform.localRotation = Quaternion.Euler(rot.x, currentRotationX, rot.z);
      rotationTransformer.ApplyLookRotation(Quaternion.Euler(currentRotationY, currentRotationX, 0));
    }
  }
}
