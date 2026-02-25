using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

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
        if (IsInAttackState()) { animator.Play(AttackState, 0, 0f); return; }
        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(AttackTrigger);
    }

    private bool IsInAttackState()
    {
        if (animator == null) return false;
        return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackState;
    }
}
