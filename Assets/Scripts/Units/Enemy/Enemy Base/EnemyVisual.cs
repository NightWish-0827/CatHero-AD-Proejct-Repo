using UnityEngine;
using DG.Tweening;

public class EnemyVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private Transform tweenRoot;
    private Transform baseRoot;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private bool basePoseCaptured;

    private bool bobEnabled;
    private float bobAmplitude;
    private float bobFrequency;

    private void Awake()
    {
        ResolveTargets(captureBasePose: true);
    }

    private void ResolveTargets(bool captureBasePose)
    {
        tweenRoot = visualRoot != null ? visualRoot : transform;

        if (spriteRenderer == null)
        {
            spriteRenderer = tweenRoot.GetComponentInChildren<SpriteRenderer>(true);
        }

        // 스케일/펀치/사망 스케일 트윈이 "실제 보이는 스프라이트"에 적용되도록 보정
        // (색상 플래시는 spriteRenderer에 직접 걸리므로 이 불일치가 있으면 스케일 연출만 안 보이는 케이스가 생김)
        if (spriteRenderer != null && tweenRoot != null && !spriteRenderer.transform.IsChildOf(tweenRoot))
        {
            tweenRoot = spriteRenderer.transform;
        }

        if (tweenRoot == null) tweenRoot = transform;

        // IMPORTANT: base 포즈는 최초 1회만 캡처해야 함.
        // (사망 트윈으로 스케일이 0이 된 상태에서 재캡처되면, 풀 재사용 시 영구적으로 안 보이는 문제가 발생)
        if (captureBasePose && !basePoseCaptured)
        {
            baseRoot = tweenRoot;
            baseLocalPosition = baseRoot.localPosition;
            baseLocalScale = baseRoot.localScale;
            basePoseCaptured = true;
        }
    }

    private void Update()
    {
        if (!bobEnabled) return;
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root == null) return;
        root.localPosition = baseLocalPosition + Vector3.up * (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
    }

    public void KillTweens()
    {
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root != null) root.DOKill();
        if (spriteRenderer != null) spriteRenderer.DOKill();
    }

    public void ResetForSpawn()
    {
        ResolveTargets(captureBasePose: false);
        KillTweens();
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root != null)
        {
            root.localPosition = baseLocalPosition;
            root.localScale = baseLocalScale;
        }
        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    public Tween PlaySpawnScale(float duration)
    {
        ResolveTargets(captureBasePose: false);
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root == null) return null;
        root.localScale = Vector3.zero;
        return root.DOScale(baseLocalScale, duration).SetEase(Ease.OutBack).SetTarget(root);
    }

    public void PlayHitFlash(float duration)
    {
        if (spriteRenderer == null) return;
        Color start = spriteRenderer.color;
        start.a = 1f;
        DOTween.To(() => spriteRenderer.color, x => spriteRenderer.color = x, new Color(1f, 0f, 0f, start.a), duration)
            .SetTarget(spriteRenderer)
            .OnComplete(() => spriteRenderer.color = start);
    }

    public void PlayHitPunchScale(Vector3 punch, float duration, int vibrato, float elasticity)
    {
        ResolveTargets(captureBasePose: false);
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root == null) return;
        root.DOPunchScale(punch, duration, vibrato, elasticity).SetTarget(root);
    }

    public Tween PlayPunchPosition(Vector3 punch, float duration, int vibrato, float elasticity, Ease ease = Ease.OutQuad)
    {
        ResolveTargets(captureBasePose: false);
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root == null) return null;
        return root.DOPunchPosition(punch, duration, vibrato, elasticity).SetEase(ease).SetTarget(root);
    }

    public Sequence CreateDieSequence(float duration)
    {
        ResolveTargets(captureBasePose: false);
        var seq = DOTween.Sequence();
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root != null)
        {
            seq.Append(root.DOScaleY(0f, duration).SetEase(Ease.InBounce).SetTarget(root));
        }
        if (spriteRenderer != null)
        {
            seq.Join(DOTween.ToAlpha(() => spriteRenderer.color, x => spriteRenderer.color = x, 0f, duration).SetTarget(spriteRenderer));
        }
        return seq;
    }

    public void EnableBob(float amplitude, float frequency)
    {
        bobAmplitude = Mathf.Max(0f, amplitude);
        bobFrequency = Mathf.Max(0f, frequency);
        bobEnabled = bobAmplitude > 0f && bobFrequency > 0f;
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (!bobEnabled && root != null) root.localPosition = baseLocalPosition;
    }

    public void DisableBob()
    {
        bobEnabled = false;
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root != null) root.localPosition = baseLocalPosition;
    }
}
