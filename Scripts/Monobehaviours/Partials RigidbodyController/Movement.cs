using Sirenix.OdinInspector;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Movement")]
    [SerializeField] float baseMovementSpeed = 4f;
    [SerializeField] float airModifier = 0.5f;
    [SerializeField] float crouchModifier = 0.5f;
    [ReadOnly] public float modifiedMovementSpeed;

    public Vector2 Movement { get; protected set; }

    Vector3 desiredVelocity;

    void Move()
    {
      float acceleration = isGrounded ? modifiedMovementSpeed : modifiedMovementSpeed * airModifier;
      float maxSpeed = modifiedMovementSpeed * (isGrounded && isCrouching ? crouchModifier : 1f);

      desiredVelocity = cameraRig.right * Movement.x * maxSpeed + cameraRig.forward * Movement.y * maxSpeed;

      isMoving = isGrounded && isStable && desiredVelocity.magnitude > 0;
      Animator.SetBool(AnimIsMoving, isMoving);

      Vector3 velocity = rigidBody.velocity;
      Vector3 xAxis = Vector3.right;
      Vector3 zAxis = Vector3.forward;

      float currentX = velocity.x;
      float currentZ = velocity.z;

      if (isStable && !isJumping)
      {
        xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        currentX = Vector3.Dot(velocity, xAxis);
        currentZ = Vector3.Dot(velocity, zAxis);
      }

      float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, acceleration);
      float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, acceleration);

      velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
      rigidBody.velocity = velocity;

      //float newYVel = Mathf.Round(velocity.y * 100f) / 100f;
      //if (!oldYVel.EqualsWithTolerance(newYVel))
      //  Debug.Log($"Before {oldYVel}, Now: {newYVel }");
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
      return vector - groundContactNormal * Vector3.Dot(vector, groundContactNormal);
    }
  }
}
