using UnityEngine;

//악몽 몬스터 Variation. EnemyBase 공통 로직 + Nightmare 전용 연출.
public class NightmareMonster : EnemyBase
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    protected override SpriteRenderer HitEffectSprite => spriteRenderer;
}
// NightmareMonster
