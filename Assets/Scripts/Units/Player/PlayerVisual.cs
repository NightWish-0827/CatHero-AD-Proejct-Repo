using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [Header("Attack VFX")]
    [Tooltip("공격 시 1회 재생할 파티클(Visual 하위/루트 내부 오브젝트를 연결)")]
    [SerializeField] private ParticleSystem[] attackParticles;
    [Tooltip("매 공격마다 확실히 '1회' 재생되도록 Clear+Play로 재시작합니다.")]
    [SerializeField] private bool restartParticlesOnAttack = true;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int AttackState = Animator.StringToHash("PC Attack");

    private void Awake()
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        animator ??= GetComponentInParent<Animator>();
        animator ??= GetComponentInChildren<Animator>();
    }

    public void PlayAttack()
    {
        if (animator == null) return;
        PlayAttackVfx();
        if (IsInAttackState()) { animator.Play(AttackState, 0, 0f); return; }
        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(AttackTrigger);
    }

    private void PlayAttackVfx()
    {
        if (attackParticles == null || attackParticles.Length == 0) return;
        for (int i = 0; i < attackParticles.Length; i++)
        {
            var ps = attackParticles[i];
            if (ps == null) continue;

            if (restartParticlesOnAttack)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
            else
            {
                if (!ps.isPlaying) ps.Play(true);
            }
        }
    }

    private bool IsInAttackState()
    {
        if (animator == null) return false;
        return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackState;
    }
}
