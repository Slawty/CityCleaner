using UnityEngine;

public interface IVacuumCarryable : IVacuumable
{
    bool IsAttached { get; }
    void ReleaseFromVacuum();
    void ShootFromVacuum(Vector3 direction, float force);
}
