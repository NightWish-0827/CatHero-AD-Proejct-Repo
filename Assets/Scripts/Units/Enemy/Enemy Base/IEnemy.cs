using UnityEngine;

public interface IEnemy : IPoolable
{
    EnemyState CurrentState { get; }
    bool IsAlive { get; }
    Transform Target { get; set; }
    EnemyStat Stats { get; }
    void Initialize(Transform target);
    void TakeDamage(float damage);
}
