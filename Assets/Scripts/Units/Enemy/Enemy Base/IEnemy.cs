using UnityEngine;

public interface IEnemy : IPoolable
{
    EnemyState CurrentState { get; }
    bool IsAlive { get; }
    Transform Target { get; set; }
    EnemyStatDB Stats { get; }
    void Initialize(Transform target, EnemyStatDB statDB);
    void TakeDamage(float damage);
}
