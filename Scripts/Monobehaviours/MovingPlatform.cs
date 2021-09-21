using Fralle.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Fralle.FpsController
{
  public class MovingPlatform : MonoBehaviour
  {
    [SerializeField] Vector3 targetPosition;
    [SerializeField] float acceleration = 1f;
    [SerializeField] float maxSpeed = 2f;
    [SerializeField] float distanceBuffer = 0.1f;
    [SerializeField] bool loop;

    List<Rigidbody> travelers = new List<Rigidbody>();
    new Rigidbody rigidbody;
    SpringJoint springJoint;
    Transform connector;
    Vector3 startPosition;
    Vector3 direction;
    float connectorY;

    void Awake()
    {
      rigidbody = GetComponent<Rigidbody>();
      springJoint = GetComponent<SpringJoint>();
    }

    void Start()
    {
      connector = springJoint.connectedBody.transform;
      connectorY = connector.position.y;

      startPosition = transform.position;
      direction = (targetPosition - startPosition).normalized;
    }

    void FixedUpdate()
    {
      connector.position = transform.position.With(y: connectorY);
      PerformMovement();
      PerformTravelerMovement();
      PerformLoop();
    }

    void PerformMovement()
    {
      Vector3 velocity = rigidbody.velocity;

      float currentX = Vector3.Dot(velocity, Vector3.right);
      float currentZ = Vector3.Dot(velocity, Vector3.forward);

      float newX = Mathf.MoveTowards(currentX, direction.x * maxSpeed, acceleration);
      float newZ = Mathf.MoveTowards(currentZ, direction.z * maxSpeed, acceleration);

      Vector3 movementVelocity = Vector3.right * (newX - currentX) + Vector3.forward * (newZ - currentZ);

      rigidbody.velocity = velocity + movementVelocity;
    }

    void PerformTravelerMovement()
    {
      foreach (var rb in travelers)
        rb.velocity += rigidbody.velocity;
    }

    void PerformLoop()
    {
      if (!loop)
        return;

      if (Vector3.Distance(transform.position, targetPosition) < distanceBuffer)
      {
        Vector3 newTarget = startPosition;
        startPosition = targetPosition;
        targetPosition = newTarget;
        direction = (targetPosition - startPosition).normalized;
      }
    }

    void OnCollisionEnter(Collision collision)
    {
      if (collision.gameObject.TryGetComponent(out Rigidbody rb))
        travelers.Add(rb);
    }

    void OnCollisionExit(Collision collision)
    {
      if (collision.gameObject.TryGetComponent(out Rigidbody rb))
      {
        travelers.Remove(rb);
      }
    }
  }
}
