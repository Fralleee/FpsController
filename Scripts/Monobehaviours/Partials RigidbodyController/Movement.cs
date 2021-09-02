using Fralle.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Movement")]
    [SerializeField] float baseMovementSpeed = 4f;
    [SerializeField] float stopTime = 0.05f;
    [SerializeField] float airControl = 0.5f;
    [FormerlySerializedAs("ModifiedMovementSpeed")] [ReadOnly] public float modifiedMovementSpeed;

    public Vector2 Movement { get; protected set; }

    Vector3 desiredForce;
    Vector3 damp;

    void Move()
    {
      if (isGrounded)
      {
        GroundMove();
        isMoving = desiredForce.magnitude > 0; // also check if not blocked

        Animator.SetBool(AnimIsMoving, isMoving);
      }
      else
        AirMove();
    }

    void GroundMove()
    {
      rigidBody.AddForce(desiredForce * modifiedMovementSpeed, ForceMode.Impulse);
      StoppingForcesGround();
    }

    void AirMove()
    {
      rigidBody.AddForce(desiredForce * modifiedMovementSpeed * airControl, ForceMode.Impulse);
      StoppingForcesAir();
    }

    void StoppingForcesGround()
    {
      rigidBody.velocity = Vector3.SmoothDamp(rigidBody.velocity, Vector3.zero, ref damp, stopTime);
    }

    void StoppingForcesAir()
    {
      rigidBody.velocity = Vector3.SmoothDamp(rigidBody.velocity, Vector3.zero, ref damp, stopTime).With(y: rigidBody.velocity.y);
    }
  }
}
