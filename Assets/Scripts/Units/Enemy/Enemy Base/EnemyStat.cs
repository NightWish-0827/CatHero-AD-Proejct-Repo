using UnityEngine;

public class EnemyStat : MonoBehaviour
{
    [SerializeField] private EnemyStatSO statSO;

    public float MaxHealth => statSO != null ? statSO.MaxHealth : 10f;
    public float MoveSpeed => statSO != null ? statSO.MoveSpeed : 2f;
    public float AttackDamage => statSO != null ? statSO.AttackDamage : 5f;
    public float AttackRange => statSO != null ? statSO.AttackRange : 1.5f;
    public float AttackCooldown => statSO != null ? statSO.AttackCooldown : 1.5f;
}
