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
    [SerializeField] float airAcceleration = 0.75f;
    [ReadOnly] public float currentMaxMovementSpeed;

    public Vector2 Movement { get; protected set; }

    Vector3 ProjectOnContactPlane(Vector3 vector) => vector - groundContactNormal * Vector3.Dot(vector, groundContactNormal);
    Vector3 desiredVelocity;

    void Move()
    {
      float acceleration = isGrounded ? groundAcceleration : airAcceleration;
      float maxSpeed = currentMaxMovementSpeed;
      maxSpeed *= isGrounded && isCrouching ? crouchModifier : 1f;

      isMoving = isGrounded && isStable && desiredVelocity.magnitude > 0;
      Animator.SetBool(AnimIsMoving, isMoving);

      Vector3 velocity = rigidBody.velocity;
      Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
      Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;
      float currentX = Vector3.Dot(velocity, xAxis);
      float currentZ = Vector3.Dot(velocity, zAxis);
      float newX = Mathf.MoveTowards(currentX, desiredVelocity.x * maxSpeed, acceleration);
      float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z * maxSpeed, acceleration);

      Vector3 movementVelocity = xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

      if (isGrounded && !isStable || isJumping)
      {
        Vector3 downGroundNormal = ProjectOnContactPlane(Vector3.down).normalized;
        float traversingUpwardsSlopeFactor = Vector3.Dot(movementVelocity.normalized, -downGroundNormal);
        movementVelocity += downGroundNormal * traversingUpwardsSlopeFactor * movementVelocity.magnitude;
      }

      velocity += movementVelocity;
      rigidBody.velocity = velocity;
    }


    void SetDesiredVelocity()
    {
      Vector3 right = new Vector3(cameraRig.right.x, 0, cameraRig.right.z).normalized;
      Vector3 forward = Quaternion.Euler(0, -90, 0) * right;

      desiredVelocity = right * Movement.x + forward * Movement.y;
    }

    void SlopeControl()
    {
      if (!isGrounded)
        return;

      if (isStable)
      {
        Vector3 upSlopeForce = Vector3.ProjectOnPlane(-Physics.gravity, groundContactNormal);
        rigidBody.AddForce(upSlopeForce);
      }
    }
  }
}
