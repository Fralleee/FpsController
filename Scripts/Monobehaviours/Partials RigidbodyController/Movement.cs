using Fralle.Core;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController : MonoBehaviour
  {
    [Header("Movement")]
    [SerializeField] float baseMovementSpeed = 4f;
    [SerializeField] float stopTime = 0.05f;
    [SerializeField] float airControl = 0.5f;
    [Readonly] public float ModifiedMovementSpeed;

    public Vector2 Movement { get; protected set; }

    Vector3 desiredForce;
    Vector3 damp;

    void Move()
    {
      if (IsGrounded)
      {
        GroundMove();
        IsMoving = desiredForce.magnitude > 0; // also check if not blocked

        animator.SetBool(animIsMoving, IsMoving);
      }
      else
        AirMove();
    }

    void GroundMove()
    {
      desiredForce = Vector3.ProjectOnPlane(desiredForce, GroundContactNormal).normalized;
      RigidBody.AddForce(desiredForce * ModifiedMovementSpeed, ForceMode.Impulse);
      StoppingForcesGround();
    }

    void AirMove()
    {
      RigidBody.AddForce(desiredForce * ModifiedMovementSpeed * airControl, ForceMode.Impulse);
      StoppingForcesAir();
    }

    void StoppingForcesGround()
    {
      RigidBody.velocity = Vector3.SmoothDamp(RigidBody.velocity, Vector3.zero, ref damp, stopTime);
    }

    void StoppingForcesAir()
    {
      RigidBody.velocity = Vector3.SmoothDamp(RigidBody.velocity, Vector3.zero, ref damp, stopTime).With(y: RigidBody.velocity.y);
    }
  }
}
