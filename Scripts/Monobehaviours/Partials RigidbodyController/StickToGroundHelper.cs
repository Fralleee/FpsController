namespace Fralle.FpsController
{
  public partial class RigidbodyController
  {
    void StickToGroundHelper()
    {
      if (!isStable)
        return;

      //rigidBody.velocity = Vector3.ProjectOnPlane(rigidBody.velocity, groundContactNormal);
      //ApplyVelocity();
    }

    //void ApplyVelocity()
    //{
    //  Vector3 velocity = rigidBody.velocity;
    //  float acceleration = isGrounded & isStable ? modifiedMovementSpeed : modifiedMovementSpeed * airModifier;

    //  Vector3 xAxis = Vector3.right;
    //  Vector3 zAxis = Vector3.forward;

    //  float currentX = velocity.x;
    //  float currentZ = velocity.z;

    //  if (isStable && !isJumping)
    //  {
    //    xAxis = ProjectOnContactPlane(Vector3.right).normalized;
    //    zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

    //    currentX = Vector3.Dot(velocity, xAxis);
    //    currentZ = Vector3.Dot(velocity, zAxis);
    //  }

    //  //float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, acceleration);
    //  //float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, acceleration);

    //  velocity += xAxis * currentX + zAxis * currentZ;
    //  rigidBody.AddForce(velocity);

    //  if (isStable)
    //    Debug.DrawRay(rigidBody.transform.position, velocity.normalized, Color.blue);
    //  else
    //    Debug.DrawRay(rigidBody.transform.position, velocity.normalized, Color.red);
    //}

    //Vector3 ProjectOnContactPlane(Vector3 vector)
    //{
    //  return vector - groundContactNormal * Vector3.Dot(vector, groundContactNormal);
    //}
  }
}
