using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStat", menuName = "CatHero/Enemy Stat")]
public class EnemyStatSO : ScriptableObject
{
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;

    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
}
