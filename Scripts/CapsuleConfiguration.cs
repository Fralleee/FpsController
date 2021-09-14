using UnityEngine;

namespace Fralle.FpsController
{
  public struct CapsuleConfiguration
  {
    public Vector3 top { get; set; }
    public Vector3 center { get; set; }
    public Vector3 bottom { get; set; }
    public float radius { get; set; }
    public float height { get; set; }

    public void Apply(CapsuleCollider capsuleCollider)
    {
      capsuleCollider.height = height;
      capsuleCollider.radius = radius;
      capsuleCollider.center = center;
    }
  }
}
