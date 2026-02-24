using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    private Transform _target;
    private IEnemy _enemy;
    private float _damage;
    private float _speed;
    private float _hitRadiusSqr;

    public void Initialize(Transform target, IEnemy enemy, float damage, float speed, float hitRadius)
    {
        _target = target;
        _enemy = enemy;
        _damage = damage;
        _speed = speed;
        _hitRadiusSqr = hitRadius * hitRadius;
    }

    public void OnSpawn() { }

    public void OnDespawn() { }

    private void Update()
    {
        if (_target == null || _enemy == null || !_enemy.IsAlive)
        {
            ReturnToPool();
            return;
        }

        Vector3 dir = (_target.position - transform.position).normalized;
        transform.position += dir * (_speed * Time.deltaTime);

        float sqrDist = (_target.position - transform.position).sqrMagnitude;
        if (sqrDist <= _hitRadiusSqr)
        {
            _enemy.TakeDamage(_damage);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
