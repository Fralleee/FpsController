using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    public event Action<bool> OnCrouchStateChanged = delegate { };

    [Header("Crouching")]
    [SerializeField] float crouchingSpeed = 8f;
    [SerializeField] float crouchHeight = 1f;

    protected bool crouchButton;

    Vector3 crouchingScale;
    Vector3 defaultScale;
    float roofCheckHeight;

    void Crouch()
    {
      if (crouchButton)
      {
        if (!IsCrouching)
        {
          OnCrouchStateChanged(true);
          Body.GetComponent<Rigidbody>().AddForce(Vector3.up * 2f, ForceMode.VelocityChange);
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
