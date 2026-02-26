using UnityEngine;
using R3;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

[DisallowMultipleComponent]
public class SecondDrawHalfCinematic : MonoBehaviour
{
    [Header("Tint Targets (restore on end)")]
    [SerializeField] private SpriteRenderer[] targetSpriteRenderers = new SpriteRenderer[4];
    [SerializeField] private Color targetColor = Color.white;
    [SerializeField, Min(0f)] private float tintDuration = 0.6f;
    [SerializeField] private Ease tintEase = Ease.OutQuad;
    [SerializeField, Min(0f)] private float restoreDuration = 0.25f;
    [SerializeField] private Ease restoreEase = Ease.OutQuad;

    [Header("Timing (Unscaled)")]
    [SerializeField, Min(0f)] private float waitForDimClearTimeoutSeconds = 2.0f;
    [SerializeField, Min(0f)] private float holdSecondsBeforeFinish = 0.0f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem launchParticle;

    [SerializeField] private ParticleSystem impactParticlePrefab;
    [SerializeField] private Transform impactParticleParent;
    [SerializeField, Min(0f)] private float impactParticleAutoDestroySeconds = 2.0f;

    [Header("Star Prefab (Ultra-wide shot)")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private Transform starParent;

    [Header("Star Offset Origin (Parent + Offset)")]
    [SerializeField] private Transform offsetOrigin;

    [Header("Star Offsets (single star, origin + offset)")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private Vector3 phase1Offset = new Vector3(0f, 3.5f, 0f);
    [SerializeField] private Vector3 phase2Offset = new Vector3(2.5f, 0.5f, 0f);

    [Header("Star Move")]
    [SerializeField] private Ease phase1Ease = Ease.OutQuad;
    [SerializeField] private Ease phase2Ease = Ease.InQuad;
    [SerializeField, Min(0f)] private float impactDespawnDelaySeconds = 1.25f;
    [SerializeField, Min(0f)] private float phase1MoveDurationSeconds = 0.35f;
    [SerializeField, Min(0f)] private float airHangSecondsBeforeDescend = 0.0f;
    [SerializeField, Min(0f)] private float phase2MoveDurationSeconds = 0.18f;

    [Header("Star Sorting (Order In Layer)")]
    [SerializeField] private int phase1SortingOrder = -10;
    [SerializeField] private int phase2SortingOrder = 0;

    [Header("Star Scale (apply instantly at phase2 start)")]
    [SerializeField] private Vector3 phase1LocalScale = Vector3.one;
    [SerializeField] private Vector3 phase2LocalScale = Vector3.one * 1.35f;

    [Header("Enemy")]
    [SerializeField] private bool killAllEnemiesOnFinish = true;

    private CompositeDisposable _disposables;
    private CancellationToken _destroyToken;
    private bool _isRunning;

    private Color[] _cachedBaseColors;
    private sealed class SpawnedStar
    {
        public GameObject go;
        public Transform tr;
        public SpriteRenderer[] spriteRenderers;
    }

    private readonly List<SpawnedStar> _spawnedStars = new List<SpawnedStar>(32);
    private readonly List<Vector3> _impactPositions = new List<Vector3>(8);

    private void OnEnable()
    {
        _destroyToken = this.GetCancellationTokenOnDestroy();
        _disposables = new CompositeDisposable();

        GameEvents.OnSecondDrawHalfCinematicRequested
            .Subscribe(_ => StartSequenceAsync().Forget())
            .AddTo(_disposables);
    }

    private void OnDisable()
    {
        _disposables?.Dispose();
        _disposables = null;
    }

    private async UniTaskVoid StartSequenceAsync()
    {
        if (_isRunning) return;
        _isRunning = true;

        try
        {
            _impactPositions.Clear();
            await AwaitBackgroundDimClearedOrTimeoutAsync(waitForDimClearTimeoutSeconds, _destroyToken);

            CacheBaseColors();

            var tintTask = TintTargetsAsync(_destroyToken);

            PlayLaunchParticleOnce();

            await SpawnAndMoveStarsPhase1Async(phase1MoveDurationSeconds, _destroyToken);

            if (airHangSecondsBeforeDescend > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(airHangSecondsBeforeDescend), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, _destroyToken);
            }

            await MoveStarsPhase2Async(phase2MoveDurationSeconds, _destroyToken);

            try
            {
                await tintTask;
            }
            catch (OperationCanceledException)
            {
            }

            GameEvents.OnSecondDrawHalfCinematicImpact.OnNext(R3.Unit.Default);

            PlayImpactParticles();

            if (killAllEnemiesOnFinish)
            {
                KillAllEnemiesOnce();
            }

            await RestoreTargetsAsync(_destroyToken);

            if (holdSecondsBeforeFinish > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdSecondsBeforeFinish), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, _destroyToken);
            }

            ScheduleDespawnStars();

            GameEvents.OnSecondDrawHalfCinematicFinished.OnNext(R3.Unit.Default);
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async UniTask AwaitBackgroundDimClearedOrTimeoutAsync(float timeoutSeconds, CancellationToken token)
    {
        var tcs = new UniTaskCompletionSource();
        IDisposable disp = null;
        disp = GameEvents.OnBackgroundDimCleared.Subscribe(_ =>
        {
            tcs.TrySetResult();
            disp?.Dispose();
        });

        try
        {
            if (timeoutSeconds <= 0f)
            {
                await tcs.Task.AttachExternalCancellation(token);
                return;
            }

            await UniTask.WhenAny(
                tcs.Task.AttachExternalCancellation(token),
                UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token)
            );
        }
        finally
        {
            disp?.Dispose();
        }
    }

    private async UniTask TintTargetsAsync(CancellationToken token)
    {
        if (targetSpriteRenderers == null || targetSpriteRenderers.Length == 0) return;

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        for (int i = 0; i < targetSpriteRenderers.Length; i++)
        {
            var sr = targetSpriteRenderers[i];
            if (sr == null) continue;
            seq.Join(sr.DOColor(targetColor, tintDuration).SetEase(tintEase).SetUpdate(true).SetTarget(sr));
        }

        if (seq.active)
        {
            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
        }
    }

    private void CacheBaseColors()
    {
        if (targetSpriteRenderers == null || targetSpriteRenderers.Length == 0)
        {
            _cachedBaseColors = Array.Empty<Color>();
            return;
        }

        _cachedBaseColors = new Color[targetSpriteRenderers.Length];
        for (int i = 0; i < targetSpriteRenderers.Length; i++)
        {
            var sr = targetSpriteRenderers[i];
            _cachedBaseColors[i] = sr != null ? sr.color : Color.white;
        }
    }

    private async UniTask RestoreTargetsAsync(CancellationToken token)
    {
        if (targetSpriteRenderers == null || targetSpriteRenderers.Length == 0) return;
        if (_cachedBaseColors == null || _cachedBaseColors.Length != targetSpriteRenderers.Length) return;

        float dur = Mathf.Max(0f, restoreDuration);
        if (Mathf.Approximately(dur, 0f))
        {
            for (int i = 0; i < targetSpriteRenderers.Length; i++)
            {
                var sr = targetSpriteRenderers[i];
                if (sr == null) continue;
                sr.color = _cachedBaseColors[i];
            }
            return;
        }

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        for (int i = 0; i < targetSpriteRenderers.Length; i++)
        {
            var sr = targetSpriteRenderers[i];
            if (sr == null) continue;
            seq.Join(sr.DOColor(_cachedBaseColors[i], dur).SetEase(restoreEase).SetUpdate(true).SetTarget(sr));
        }

        if (seq.active)
        {
            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
        }
    }

    private async UniTask SpawnAndMoveStarsPhase1Async(float duration, CancellationToken token)
    {
        _spawnedStars.Clear();
        if (starPrefab == null || duration <= 0f) return;

        Transform parent = starParent != null ? starParent : transform;
        Transform origin = offsetOrigin != null ? offsetOrigin : transform;
        Vector3 basePos = origin.position;

        float dur = Mathf.Max(0.01f, duration);

        Vector3 spawnPos = basePos + spawnOffset;
        var go = Instantiate(starPrefab, spawnPos, Quaternion.identity, parent);
        var star = new SpawnedStar
        {
            go = go,
            tr = go != null ? go.transform : null,
            spriteRenderers = go != null ? go.GetComponentsInChildren<SpriteRenderer>(true) : Array.Empty<SpriteRenderer>()
        };
        _spawnedStars.Add(star);
        SetSortingOrderRecursive(star.tr, phase1SortingOrder);
        if (star.tr != null) star.tr.localScale = phase1LocalScale;

        Vector3 to1 = basePos + phase1Offset;
        if (star.tr != null)
        {
            await star.tr.DOMove(to1, dur).SetEase(phase1Ease).SetUpdate(true).SetTarget(star.tr)
                .AsyncWaitForCompletion()
                .AsUniTask()
                .AttachExternalCancellation(token);
        }
    }

    private async UniTask MoveStarsPhase2Async(float duration, CancellationToken token)
    {
        if (_spawnedStars.Count == 0 || duration <= 0f) return;

        Transform origin = offsetOrigin != null ? offsetOrigin : transform;
        Vector3 basePos = origin.position;
        float dur = Mathf.Max(0.01f, duration);

        for (int i = 0; i < _spawnedStars.Count; i++)
        {
            var star = _spawnedStars[i];
            if (star == null || star.go == null || star.tr == null) continue;
            SetSortingOrderRecursive(star.tr, phase2SortingOrder);
            star.tr.localScale = phase2LocalScale;
        }

        _impactPositions.Clear();
        var tweens = new List<Tween>(_spawnedStars.Count);
        for (int i = 0; i < _spawnedStars.Count; i++)
        {
            var star = _spawnedStars[i];
            if (star == null || star.go == null || star.tr == null) continue;

            Vector3 to2 = basePos + phase2Offset;
            _impactPositions.Add(to2);

            Tween t = star.tr.DOMove(to2, dur).SetEase(phase2Ease).SetUpdate(true).SetTarget(star.tr);
            tweens.Add(t);
        }

        for (int i = 0; i < tweens.Count; i++)
        {
            await tweens[i].AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
        }

        for (int i = 0; i < _spawnedStars.Count; i++)
        {
            var star = _spawnedStars[i];
            if (star?.go == null) continue;
            Destroy(star.go);
        }
        _spawnedStars.Clear();
    }

    private void PlayImpactParticles()
    {
        if (_impactPositions.Count > 0)
        {
            for (int i = 0; i < _impactPositions.Count; i++)
            {
                SpawnImpactParticleAt(_impactPositions[i]);
            }
            return;
        }
    }

    private void ScheduleDespawnStars()
    {
        if (_spawnedStars.Count == 0) return;

        float delay = Mathf.Max(0f, impactDespawnDelaySeconds);
        for (int i = 0; i < _spawnedStars.Count; i++)
        {
            var star = _spawnedStars[i];
            if (star?.go == null) continue;

            if (delay <= 0f)
            {
                Destroy(star.go);
            }
            else
            {
                Destroy(star.go, delay);
            }
        }
        _spawnedStars.Clear();
    }

    // 이거 왜 했지?
    private static void SetSortingOrderRecursive(Transform root, int order)
    {
        if (root == null) return;

        var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var r = spriteRenderers[i];
                if (r == null) continue;
                r.sortingOrder = order;
            }
        }

        var particleRenderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (particleRenderers != null)
        {
            for (int i = 0; i < particleRenderers.Length; i++)
            {
                var pr = particleRenderers[i];
                if (pr == null) continue;
                pr.sortingOrder = order;
            }
        }
    }

    private void PlayLaunchParticleOnce()
    {
        if (launchParticle == null) return;

        SetUnscaledTimeRecursive(launchParticle, true);
        launchParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        launchParticle.Play(true);
    }

    private void SpawnImpactParticleAt(Vector3 worldPos)
    {
        if (impactParticlePrefab == null) return;

        Transform parent = impactParticleParent != null ? impactParticleParent : transform;
        var ps = Instantiate(impactParticlePrefab, worldPos, Quaternion.identity, parent);

        SetUnscaledTimeRecursive(ps, true);
        ps.Play(true);

        float destroyAfter = Mathf.Max(0f, impactParticleAutoDestroySeconds);
        if (destroyAfter > 0f)
        {
            Destroy(ps.gameObject, destroyAfter);
        }
    }

    private static void SetUnscaledTimeRecursive(ParticleSystem root, bool unscaled)
    {
        if (root == null) return;
        var all = root.GetComponentsInChildren<ParticleSystem>(true);
        if (all == null) return;

        for (int i = 0; i < all.Length; i++)
        {
            var ps = all[i];
            if (ps == null) continue;
            var main = ps.main;
            main.useUnscaledTime = unscaled;
        }
    }

    private static void KillAllEnemiesOnce()
    {
        var list = EnemyRegistry.Enemies;
        if (list == null || list.Count == 0) return;

        var snapshot = new List<IEnemy>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e != null && e.IsAlive) snapshot.Add(e);
        }

        for (int i = 0; i < snapshot.Count; i++)
        {
            snapshot[i].TakeDamage(float.MaxValue);
        }
    }
}

