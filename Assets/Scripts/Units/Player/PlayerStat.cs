using UnityEngine;

/// <summary>Root 하위. PC Stat SO를 참조하여 스탯 제공.</summary>
public class PlayerStat : MonoBehaviour
{
    [SerializeField] private PlayerStatSO statSO;

    public float MaxHealth => statSO != null ? statSO.MaxHealth : 100f;
    public float BaseAttackDamage => statSO != null ? statSO.BaseAttackDamage : 5f;
    public float AttackInterval => statSO != null ? statSO.AttackInterval : 0.5f;
    public float AttackRange => statSO != null ? statSO.AttackRange : 10f;
}
