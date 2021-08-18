using Fralle.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Fralle.FpsController
{
  public class AIController : MonoBehaviour, IDisableOnDeath
  {
    [Header("Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;

    NavMeshAgent navMeshAgent;
    Animator animator;

    int animIsMoving;
    int animVelocity;

    public float speed { get => navMeshAgent.speed; set { navMeshAgent.speed = value; } }
    public float stoppingDistance { get => navMeshAgent.stoppingDistance; set { navMeshAgent.stoppingDistance = value; } }
    public float remainingDistance => navMeshAgent.remainingDistance;
    public Vector3 velocity => navMeshAgent.velocity;

    public void SetDestination(Vector3 target)
    {
      if (!navMeshAgent.enabled)
        return;

      navMeshAgent.SetDestination(target);
    }

    public void SetRandomDestination(float maxDistance)
    {
      if (!navMeshAgent.enabled)
        return;

      Vector3 randDirection = Random.insideUnitSphere * maxDistance + transform.position;
      NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, maxDistance, -1);
      SetDestination(navHit.position);
    }

    public void Stop(float? resetSpeed = null)
    {
      if (!navMeshAgent.enabled)
        return;

      if (resetSpeed.HasValue)
        navMeshAgent.speed = resetSpeed.Value;

      navMeshAgent.isStopped = true;
      navMeshAgent.ResetPath();
    }

    void Awake()
    {
      navMeshAgent = GetComponent<NavMeshAgent>();
      animator = GetComponentInChildren<Animator>();

      animIsMoving = Animator.StringToHash("IsMoving");
      animVelocity = Animator.StringToHash("Velocity");
    }

    void Update()
    {
      if (!navMeshAgent.enabled)
        return;

      animator.SetBool(animIsMoving, navMeshAgent.velocity.magnitude > 0.1f);
      animator.SetFloat(animVelocity, navMeshAgent.velocity.magnitude / runSpeed);
    }

  }
}
