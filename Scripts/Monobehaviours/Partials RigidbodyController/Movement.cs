using Sirenix.OdinInspector;
using UnityEngine;

namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    [Header("Movement")]
    [SerializeField] float maxMovementSpeed = 7f;
    [SerializeField] float crouchModifier = 0.5f;
    [SerializeField] float groundAcceleration = 7f;
    [SerializeField] float airAcceleration = 0.5f;
    [ReadOnly] public float currentMaxMovementSpeed;

    public Vector2 Movement { get; protected set; }

    Vector3 ProjectOnContactPlane(Vector3 vector) => vector - groundContactNormal * Vector3.Dot(vector, groundContactNormal);

    Vector3 desiredVelocity;

    void Move()
    {
      float acceleration = isGrounded ? groundAcceleration : airAcceleration;
      float maxSpeed = currentMaxMovementSpeed * (isGrounded && isCrouching ? crouchModifier : 1f);

      Vector3 right = new Vector3(cameraRig.right.x, 0, cameraRig.right.z).normalized;
      Vector3 forward = Quaternion.Euler(0, -90, 0) * right;

      desiredVelocity = right * Movement.x * maxSpeed + forward * Movement.y * maxSpeed;

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
    }
  }
}
