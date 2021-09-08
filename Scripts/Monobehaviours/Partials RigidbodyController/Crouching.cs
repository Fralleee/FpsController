using System;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    public event Action<bool> OnCrouchStateChanged = delegate { };

    protected bool CrouchButton;

    [SerializeField] Vector3 standOffset;
    [SerializeField] Vector3 crouchOffset;
    [SerializeField] float crouchTime = 0.2f;

    Collider[] overlappedColliders = new Collider[8];
    bool extraCrouchBoost;
    float extraMarginTopCheck = 1.01f;

    void Crouch()
    {
      switch (CrouchButton)
      {
        case true when !isCrouching:
          StartCrouch();
          break;
        case false when isCrouching:
          EndCrouch();
          break;
      }
    }

    void StartCrouch()
    {
      isCrouching = true;
      SetCapsuleDimensions(0.5f, 1f, 0.5f);
      Model.localScale = new Vector3(1f, 0.5f, 1f);
      playerCamera.SetOffset(crouchOffset, crouchTime);


      if (extraCrouchBoost)
      {
        rigidBody.AddForce(Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * gravityModifier * 0.5f), ForceMode.VelocityChange);
        extraCrouchBoost = false;
      }

      OnCrouchStateChanged(true);
    }

    void EndCrouch()
    {
      SetCapsuleDimensions(0.5f, 2f, 1f);
      if (CharacterCollisionsOverlap())
      {
        SetCapsuleDimensions(0.5f, 1f, 0.5f);
        return;
      }

      Model.localScale = Vector3.one;
      playerCamera.SetOffset(standOffset, crouchTime);
      isCrouching = false;
      OnCrouchStateChanged(false);
    }

    void SetCapsuleDimensions(float radius, float height, float yOffset)
    {
      capsuleCollider.height = Mathf.Clamp(height, radius * 2f, height);
      capsuleCollider.center = new Vector3(0f, yOffset, 0f);
      capsuleCollider.radius = radius;
    }

    bool CharacterCollisionsOverlap()
    {
      int hits = Physics.OverlapCapsuleNonAlloc(Bottom + Vector3.up * capsuleCollider.radius, Top + Vector3.down * capsuleCollider.radius * extraMarginTopCheck, capsuleCollider.radius, overlappedColliders, groundLayers, QueryTriggerInteraction.Ignore);
      return hits > 0;
    }

    //void OnDrawGizmosSelected()
    //{
    //  if (capsuleCollider == null)
    //    return;

    //  Gizmos.color = Color.red;
    //  Gizmos.DrawSphere(Bottom + Vector3.up * capsuleCollider.radius, capsuleCollider.radius);
    //  Gizmos.DrawSphere(Top + Vector3.down * capsuleCollider.radius, capsuleCollider.radius);
    //}
  }
}
