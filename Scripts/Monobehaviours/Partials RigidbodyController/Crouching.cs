using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    public event Action<bool> OnCrouchStateChanged = delegate { };

    [Header("Crouching")]
    [SerializeField] float crouchingSpeed = 8f;

    Vector3 crouchingScale;
    Vector3 defaultScale;
    protected bool crouchButton;
    bool extraCrouchBoost;
    float roofCheckHeight;
    float crouchHeight = 1f;

    void Crouch()
    {
      if (crouchButton)
      {
        if (!IsCrouching)
        {
          OnCrouchStateChanged(true);

          if (extraCrouchBoost)
          {
            RigidBody.AddForce(Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * gravityModifier * 0.5f), ForceMode.VelocityChange);
            extraCrouchBoost = false;
          }
        }

        IsCrouching = true;
        if (Body.localScale != crouchingScale)
        {
          Body.localScale = Vector3.Lerp(Body.localScale, crouchingScale, Time.deltaTime * crouchingSpeed);
        }
      }
      else if (IsCrouching && !Physics.Raycast(transform.position, Vector3.up, roofCheckHeight, groundLayers))
      {
        IsCrouching = false;
        OnCrouchStateChanged(false);
      }

      if (!IsCrouching && Body.localScale != defaultScale)
      {
        Body.localScale = Vector3.Lerp(Body.localScale, defaultScale, Time.deltaTime * crouchingSpeed);
      }
    }
  }
}
