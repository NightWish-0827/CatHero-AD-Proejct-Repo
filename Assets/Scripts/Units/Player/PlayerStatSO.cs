using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStat", menuName = "CatHero/Player Stat")]
public class PlayerStatSO : ScriptableObject
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Attack")]
    [SerializeField] private float baseAttackDamage = 5f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private float attackRange = 10f;

    public float MaxHealth => maxHealth;
    public float BaseAttackDamage => baseAttackDamage;
    public float AttackInterval => attackInterval;
    public float AttackRange => attackRange;
}
