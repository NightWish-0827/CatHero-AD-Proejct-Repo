using UnityEngine;
using DG.Tweening;
using System;

public class EnemyVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Tween Settings (tweak in Inspector)")]
    [SerializeField, Min(0f)] private float spawnScaleDuration = 0.4f;
    [SerializeField] private Ease spawnScaleEase = Ease.OutBack;

    [SerializeField, Min(0f)] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private SpriteRenderer[] hitFlashRenderers;

    [SerializeField] private Vector3 hitPunchScale = new Vector3(-0.2f, -0.2f, -0.2f);
    [SerializeField, Min(0f)] private float hitPunchDuration = 0.15f;
    [SerializeField, Min(1)] private int hitPunchVibrato = 10;
    [SerializeField, Range(0f, 1f)] private float hitPunchElasticity = 1f;

    [SerializeField, Min(0f)] private float dieDuration = 0.2f;
    [SerializeField] private Ease dieScaleEase = Ease.InBounce;
    [SerializeField] private float dieTargetScaleY = 0f;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private Transform tweenRoot;
    private Transform baseRoot;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private bool basePoseCaptured;

    private SpriteRenderer[] cachedFlashRenderers;
    private Color[] flashOriginalColors;
    private Tween hitFlashTween;

    private bool bobEnabled;
    private float bobAmplitude;
    private float bobFrequency;

    private void Awake()
    {
        ResolveTargets(captureBasePose: true);
        EnsureFlashRenderersCached();
    }

    private void ResolveTargets(bool captureBasePose)
    {
        tweenRoot = visualRoot != null ? visualRoot : transform;

        if (spriteRenderer == null)
        {
            spriteRenderer = tweenRoot.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer != null && tweenRoot != null && !spriteRenderer.transform.IsChildOf(tweenRoot))
        {
            tweenRoot = spriteRenderer.transform;
        }

        if (tweenRoot == null) tweenRoot = transform;

        if (captureBasePose && !basePoseCaptured)
        {
            baseRoot = tweenRoot;
            baseLocalPosition = baseRoot.localPosition;
            baseLocalScale = baseRoot.localScale;
            basePoseCaptured = true;
        }
    }

    private void EnsureFlashRenderersCached()
    {
        if (hitFlashRenderers != null && hitFlashRenderers.Length > 0)
        {
            cachedFlashRenderers = hitFlashRenderers;
        }
        else
        {
            var root = visualRoot != null ? visualRoot : (baseRoot != null ? baseRoot : tweenRoot);
            if (root == null) root = transform;
            cachedFlashRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (cachedFlashRenderers == null) cachedFlashRenderers = Array.Empty<SpriteRenderer>();

        if (flashOriginalColors == null || flashOriginalColors.Length != cachedFlashRenderers.Length)
        {
            flashOriginalColors = new Color[cachedFlashRenderers.Length];
        }
    }

    private static Vector3 CompensatePunchForParentScale(Transform root, Vector3 punch)
    {
        if (root == null) return punch;
        var parent = root.parent;
        if (parent == null) return punch;

        Vector3 ps = parent.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(ps.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(ps.y));
        float sz = Mathf.Max(0.0001f, Mathf.Abs(ps.z));

        return new Vector3(punch.x / sx, punch.y / sy, punch.z / sz);
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
        if (hitFlashTween != null && hitFlashTween.active) hitFlashTween.Kill();
    }

    public void ResetForSpawn()
    {
        ResolveTargets(captureBasePose: false);
        EnsureFlashRenderersCached();
        KillTweens();
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root != null)
        {
            root.localPosition = baseLocalPosition;
            root.localScale = baseLocalScale;
        }

        if (cachedFlashRenderers != null && cachedFlashRenderers.Length > 0)
        {
            for (int i = 0; i < cachedFlashRenderers.Length; i++)
            {
                var r = cachedFlashRenderers[i];
                if (r == null) continue;
                var c = r.color;
                c.a = 1f;
                r.color = c;
            }
        }
        else if (spriteRenderer != null)
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
        return root.DOScale(baseLocalScale, duration).SetEase(spawnScaleEase).SetTarget(root);
    }

    public Tween PlaySpawnScale()
    {
        return PlaySpawnScale(spawnScaleDuration);
    }

    public void PlayHitFlash(float duration)
    {
        EnsureFlashRenderersCached();
        if (cachedFlashRenderers == null || cachedFlashRenderers.Length == 0) return;

        if (hitFlashTween != null && hitFlashTween.active) hitFlashTween.Kill();

        for (int i = 0; i < cachedFlashRenderers.Length; i++)
        {
            var r = cachedFlashRenderers[i];
            flashOriginalColors[i] = r != null ? r.color : Color.white;
        }

        float t = 0f;
        hitFlashTween = DOTween.To(() => t, v =>
        {
            t = v;
            for (int i = 0; i < cachedFlashRenderers.Length; i++)
            {
                var r = cachedFlashRenderers[i];
                if (r == null) continue;

                var start = flashOriginalColors[i];
                var target = new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, start.a);
                r.color = Color.LerpUnclamped(start, target, t);
            }
        }, 1f, duration)
        .SetEase(Ease.OutQuad)
        .SetTarget(this)
        .OnComplete(() =>
        {
            for (int i = 0; i < cachedFlashRenderers.Length; i++)
            {
                var r = cachedFlashRenderers[i];
                if (r == null) continue;
                r.color = flashOriginalColors[i];
            }
        });
    }

    public void PlayHitFlash()
    {
        PlayHitFlash(hitFlashDuration);
    }

    public void PlayHitPunchScale(Vector3 punch, float duration, int vibrato, float elasticity)
    {
        ResolveTargets(captureBasePose: false);
        var root = baseRoot != null ? baseRoot : tweenRoot;
        if (root == null) return;
        Vector3 compensated = CompensatePunchForParentScale(root, punch);
        root.DOPunchScale(compensated, duration, vibrato, elasticity).SetTarget(root);
    }

    public void PlayHitPunchScale()
    {
        PlayHitPunchScale(hitPunchScale, hitPunchDuration, hitPunchVibrato, hitPunchElasticity);
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
            seq.Append(root.DOScaleY(dieTargetScaleY, duration).SetEase(dieScaleEase).SetTarget(root));
        }
        if (spriteRenderer != null)
        {
            seq.Join(DOTween.ToAlpha(() => spriteRenderer.color, x => spriteRenderer.color = x, 0f, duration).SetTarget(spriteRenderer));
        }
        return seq;
    }

    public Sequence CreateDieSequence()
    {
        return CreateDieSequence(dieDuration);
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
