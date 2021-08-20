using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public class SwayTransformer : LocalTransformer, IRotator
  {
    [Header("Speed")]
    [SerializeField] float smoothRotation = 10f;

    [Header("Look rotation")]
    [SerializeField] float lookRotationAmount = 0.6f;
    [SerializeField] float maxLookRotation = 3f;

    RigidbodyController playerController;

    Quaternion currentRotation = Quaternion.identity;

    void Awake()
    {
      playerController = GetComponentInParent<RigidbodyController>();
    }

    public Quaternion GetRotation() => currentRotation;
    public override void Calculate()
    {
      float lookAmountX = Mathf.Clamp(playerController.MouseLook.x * lookRotationAmount, -maxLookRotation, maxLookRotation);
      float lookAmountY = Mathf.Clamp(playerController.MouseLook.y * lookRotationAmount, -maxLookRotation, maxLookRotation);

      Quaternion finalRotation = Quaternion.Euler(new Vector3(lookAmountY, lookAmountX, 0f));
      currentRotation = Quaternion.Slerp(currentRotation, finalRotation, Time.deltaTime * smoothRotation);
    }
  }
}