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
        SetCapsuleDimensions(0.5f, 2f, 1f);
        return;
      }

      Model.localScale = Vector3.one;
      playerCamera.SetOffset(standOffset, crouchTime);
      isCrouching = false;
      OnCrouchStateChanged(false);
    }

    void SetCapsuleDimensions(float radius, float height, float yOffset)
    {
      capsule.radius = radius;
      capsule.height = Mathf.Clamp(height, radius * 2f, height);
      capsule.center = new Vector3(0f, yOffset, 0f);
    }

    bool CharacterCollisionsOverlap()
    {
      Vector3 position = transform.position;
      Bounds bounds = capsule.bounds;
      Vector3 bottom = position + bounds.center - Vector3.up * bounds.extents.y;
      Vector3 top = position + bounds.center + Vector3.up * bounds.extents.y;

      int hits = Physics.OverlapCapsuleNonAlloc(bottom, top, capsule.radius, overlappedColliders, groundLayers, QueryTriggerInteraction.Ignore);
      return hits > 0;
    }

  }
}
