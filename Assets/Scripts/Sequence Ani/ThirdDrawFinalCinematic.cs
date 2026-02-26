using UnityEngine;
using R3;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

[DisallowMultipleComponent]
public class ThirdDrawFinalCinematic : MonoBehaviour
{
    [Serializable]
    private struct CameraFollowOverride
    {
        public CameraFollow follow;
        public bool overrideTarget;
        public Transform target;
        public bool overrideIsActive;
        public bool isActive;
    }

    [Header("Tint Targets (restore on end)")]
    [SerializeField] private SpriteRenderer[] targetSpriteRenderers = new SpriteRenderer[4];
    [SerializeField] private Color targetColor = Color.white;
    [SerializeField, Min(0f)] private float tintDuration = 0.55f;
    [SerializeField] private Ease tintEase = Ease.OutQuad;
    [SerializeField, Min(0f)] private float restoreDuration = 0.25f;
    [SerializeField] private Ease restoreEase = Ease.OutQuad;

    [Header("Timing (Unscaled)")]
    [SerializeField, Min(0f)] private float waitForDimClearTimeoutSeconds = 2.0f;
    [SerializeField, Min(0f)] private float holdSecondsBeforeImpact = 0.05f;
    [SerializeField, Min(0f)] private float holdSecondsBeforeFinish = 0.15f;

    [Header("Dragon Breath - Move")]
    [SerializeField] private Transform movingObject;
    [SerializeField] private Transform moveTarget;
    [SerializeField] private Vector3 moveTargetLocalPos = new Vector3(2.5f, 0.75f, 0f);
    [SerializeField] private bool snapMoveOnStart = false;
    [SerializeField, Min(0f)] private float moveDurationSeconds = 0.55f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [SerializeField, Min(0f)] private float returnMoveDurationSeconds = 0.35f;
    [SerializeField] private Ease returnMoveEase = Ease.InOutCubic;

    [Header("Dragon Breath - Cleanup")]
    [SerializeField] private bool disableMovingObjectOnAllEnemiesKilled = true;

    [Header("Dragon Breath - Particle")]
    [SerializeField] private bool autoFindRootChildParticle = true;
    [SerializeField] private ParticleSystem breathParticle;

    [Header("Breath Damage Sweep (Front -> Back)")]
    [SerializeField] private float damagePerHit = float.MaxValue;
    [SerializeField, Min(0f)] private float damageTickIntervalSeconds = 0.06f;
    [SerializeField, Min(0f)] private float holdSecondsBeforeDamageSweep = 0.0f;
    [SerializeField, Min(0)] private int maxSweepCount = 0;

    [Header("Camera Sweep (Left -> Right)")]
    [SerializeField] private bool cameraSweepEnabled = true;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private bool disableCameraFollowDuringSweep = true;
    [SerializeField, Min(0f)] private float cameraSweepSpeed = 7.5f;
    [SerializeField, Min(0f)] private float cameraSweepDurationSeconds = 2.8f;
    [SerializeField] private float cameraSweepEndPaddingX = 3.5f;

    [Header("Camera Pace (Important)")]
    [SerializeField, Min(0f)] private float cameraMaxLeadAheadOfNextKillX = 1.8f;
    [SerializeField, Min(0f)] private float cameraInitialLeadX = 0.0f;
    [SerializeField] private bool ensureCameraReachesEndXAfterKills = false;

    [Header("Death Trigger By Camera")]
    [SerializeField] private float deathTriggerOffsetX = 0.0f;

    [Header("Hit Particles (Optional)")]
    [SerializeField] private ParticleSystem hitParticlePrefab;
    [SerializeField] private Transform hitParticleParent;
    [SerializeField, Min(0f)] private float hitParticleAutoDestroySeconds = 1.5f;
    [SerializeField, Min(0)] private int maxHitParticles = 24;

    [Header("Camera Override (Optional)")]
    [SerializeField] private CameraFollowOverride cameraOverride = new CameraFollowOverride
    {
        follow = null,
        overrideTarget = true,
        target = null,
        overrideIsActive = false,
        isActive = true
    };

    [Header("Enemy")]
    [SerializeField] private bool killAllEnemiesOnImpact = false;
    [SerializeField] private bool stopSpawnerOnStart = true;
    [SerializeField] private bool restoreCameraFollowAfterFinish = false;

    private CompositeDisposable _disposables;
    private CancellationToken _destroyToken;
    private bool _isRunning;

    private Color[] _cachedBaseColors;
    private readonly List<IEnemy> _enemySnapshot = new List<IEnemy>(256);
    private Vector3 _movingObjectOriginalLocalPos;
    private bool _hasMovingObjectOriginalLocalPos;
    private ParticleSystem _resolvedBreathParticle;

    private bool _cameraCached;
    private Transform _cameraPrevTarget;
    private bool _cameraPrevIsActive;

    private void OnEnable()
    {
        _destroyToken = this.GetCancellationTokenOnDestroy();
        _disposables = new CompositeDisposable();

        GameEvents.OnThirdDrawFinalCinematicRequested
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
            await AwaitBackgroundDimClearedOrTimeoutAsync(waitForDimClearTimeoutSeconds, _destroyToken);

            CacheBaseColors();

            var tintTask = TintTargetsAsync(_destroyToken);

            if (holdSecondsBeforeImpact > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdSecondsBeforeImpact), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, _destroyToken);
            }

            if (stopSpawnerOnStart)
            {
                var spawner = FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
                spawner?.StopSpawning();
            }

            ApplyCameraOverride(true);

            if (movingObject != null)
            {
                _movingObjectOriginalLocalPos = movingObject.localPosition;
                _hasMovingObjectOriginalLocalPos = true;
            }
            else
            {
                _hasMovingObjectOriginalLocalPos = false;
            }

            ResolveBreathParticle();
            StopBreathParticle();

            await MoveObjectIfNeededAsync(_destroyToken);

            if (snapMoveOnStart)
            {
                PlayBreathParticleOnce();
            }

            GameEvents.OnThirdDrawFinalCinematicImpact.OnNext(R3.Unit.Default);

            if (holdSecondsBeforeDamageSweep > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdSecondsBeforeDamageSweep), DelayType.DeltaTime, PlayerLoopTiming.Update, _destroyToken);
            }

            await SweepByCameraAndKillAsync(_destroyToken);

            if (movingObject != null)
            {
                StopBreathParticle();
                await FadeOutMovingObjectSpriteRenderersAsync(_destroyToken);

                if (disableMovingObjectOnAllEnemiesKilled)
                {
                    movingObject.gameObject.SetActive(false);
                }
            }

            if (killAllEnemiesOnImpact)
            {
                KillAllEnemiesOnce();
            }

            try
            {
                await tintTask;
            }
            catch (OperationCanceledException)
            {
            }

            await RestoreTargetsAsync(_destroyToken);

            if (holdSecondsBeforeFinish > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdSecondsBeforeFinish), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, _destroyToken);
            }

            GameEvents.OnThirdDrawFinalCinematicFinished.OnNext(R3.Unit.Default);
        }
        finally
        {
            ApplyCameraOverride(false);
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

    private void ApplyCameraOverride(bool enable)
    {
        if (cameraOverride.follow == null)
        {
            cameraOverride.follow = FindFirstObjectByType<CameraFollow>(FindObjectsInactive.Include);
        }
        var follow = cameraOverride.follow;
        if (follow == null) return;

        if (enable)
        {
            if (!_cameraCached)
            {
                _cameraPrevTarget = follow.Target;
                _cameraPrevIsActive = follow.IsActive;
                _cameraCached = true;
            }

            if (cameraOverride.overrideIsActive)
            {
                follow.IsActive = cameraOverride.isActive;
            }

            if (cameraOverride.overrideTarget)
            {
                Transform t = cameraOverride.target != null ? cameraOverride.target : movingObject;
                if (t != null) follow.Target = t;
            }
            return;
        }

        if (_cameraCached && restoreCameraFollowAfterFinish)
        {
            if (cameraOverride.overrideTarget) follow.Target = _cameraPrevTarget;
            if (cameraOverride.overrideIsActive) follow.IsActive = _cameraPrevIsActive;
        }
    }

    private async UniTask MoveObjectIfNeededAsync(CancellationToken token)
    {
        if (movingObject == null) return;

        Vector3 to = moveTarget != null ? moveTarget.localPosition : moveTargetLocalPos;

        float dur = Mathf.Max(0f, moveDurationSeconds);
        if (Mathf.Approximately(dur, 0f))
        {
            movingObject.localPosition = to;
            return;
        }

        await movingObject
            .DOLocalMove(to, dur)
            .SetEase(moveEase)
            .SetUpdate(true)
            .SetTarget(movingObject)
            .AsyncWaitForCompletion()
            .AsUniTask()
            .AttachExternalCancellation(token);
    }

    private async UniTask ReturnMovingObjectToOriginalLocalPosAsync(CancellationToken token)
    {
        if (movingObject == null) return;
        if (!_hasMovingObjectOriginalLocalPos) return;

        float dur = Mathf.Max(0f, returnMoveDurationSeconds);
        if (Mathf.Approximately(dur, 0f))
        {
            movingObject.localPosition = _movingObjectOriginalLocalPos;
            return;
        }

        await movingObject
            .DOLocalMove(_movingObjectOriginalLocalPos, dur)
            .SetEase(returnMoveEase)
            .SetUpdate(false)
            .SetTarget(movingObject)
            .AsyncWaitForCompletion()
            .AsUniTask()
            .AttachExternalCancellation(token);
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

    private void PlayBreathParticleOnce()
    {
        var ps = ResolveBreathParticle();
        if (ps == null) return;
        SetUnscaledTimeRecursive(ps, true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(true);
    }

    private ParticleSystem ResolveBreathParticle()
    {
        if (_resolvedBreathParticle != null) return _resolvedBreathParticle;

        var ps = breathParticle;

        if (ps == null && autoFindRootChildParticle && movingObject != null)
        {
            for (int i = 0; i < movingObject.childCount; i++)
            {
                Transform child = movingObject.GetChild(i);
                if (child == null) continue;
                ps = child.GetComponentInChildren<ParticleSystem>(true);
                if (ps != null) break;
            }
        }

        _resolvedBreathParticle = ps;
        if (_resolvedBreathParticle != null)
        {
            var main = _resolvedBreathParticle.main;
            main.playOnAwake = false;
        }

        return _resolvedBreathParticle;
    }

    private void StopBreathParticle()
    {
        var ps = _resolvedBreathParticle != null ? _resolvedBreathParticle : breathParticle;
        if (ps == null) return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private async UniTask FadeOutMovingObjectSpriteRenderersAsync(CancellationToken token)
    {
        if (movingObject == null) return;

        const float fadeDuration = 0.45f;
        const float postFadeHold = 0.05f;

        var srs = movingObject.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs == null || srs.Length == 0) return;

        Sequence seq = DOTween.Sequence().SetUpdate(false).SetTarget(movingObject);
        for (int i = 0; i < srs.Length; i++)
        {
            var sr = srs[i];
            if (sr == null) continue;
            seq.Join(sr.DOFade(0f, fadeDuration).SetEase(Ease.OutQuad).SetUpdate(false).SetTarget(sr));
        }

        if (seq.active)
        {
            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
        }

        if (postFadeHold > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(postFadeHold), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
        }
    }

    private async UniTask<bool> SweepByCameraAndKillAsync(CancellationToken token)
    {
        _enemySnapshot.Clear();
        var list = EnemyRegistry.Enemies;
        if (list == null || list.Count == 0) return true;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            var uo = e as UnityEngine.Object;
            if (e == null || uo == null) continue;
            if (!e.IsAlive) continue;
            _enemySnapshot.Add(e);
        }
        if (_enemySnapshot.Count == 0) return true;

        _enemySnapshot.Sort((a, b) =>
        {
            float ax = ((a as MonoBehaviour) != null) ? ((MonoBehaviour)a).transform.position.x : float.MinValue;
            float bx = ((b as MonoBehaviour) != null) ? ((MonoBehaviour)b).transform.position.x : float.MinValue;
            return ax.CompareTo(bx);
        });

        int max = maxSweepCount > 0 ? Mathf.Min(maxSweepCount, _enemySnapshot.Count) : _enemySnapshot.Count;
        float minGap = Mathf.Max(0f, damageTickIntervalSeconds);

        cameraFollow ??= FindFirstObjectByType<CameraFollow>(FindObjectsInactive.Include);
        Transform camTr = cameraFollow != null ? cameraFollow.transform : (Camera.main != null ? Camera.main.transform : null);
        if (camTr == null) return false;

        float endX = camTr.position.x;
        for (int i = 0; i < max; i++)
        {
            var tr = (_enemySnapshot[i] as MonoBehaviour)?.transform;
            if (tr == null) continue;
            endX = Mathf.Max(endX, tr.position.x);
        }
        endX += cameraSweepEndPaddingX;

        bool prevFollowActive = cameraFollow != null && cameraFollow.IsActive;
        if (disableCameraFollowDuringSweep && cameraFollow != null) cameraFollow.IsActive = false;

        int spawnedParticles = 0;
        float lastKillAt = -999f;
        int killIndex = 0;

        float startX = camTr.position.x;
        float distTotal = Mathf.Max(0f, endX - startX);
        float sweepSpeed = cameraSweepSpeed > 0.01f
            ? cameraSweepSpeed
            : (cameraSweepDurationSeconds > 0.01f ? (distTotal / cameraSweepDurationSeconds) : 0f);
        if (sweepSpeed <= 0.0001f) sweepSpeed = 7.5f;

        while (!token.IsCancellationRequested)
        {
            token.ThrowIfCancellationRequested();

            if (cameraSweepEnabled)
            {
                float dt = Mathf.Max(0f, Time.deltaTime);
                float currentX = camTr.position.x;
                float desiredX = currentX + (sweepSpeed * dt);
                float nextX = Mathf.Min(desiredX, endX);

                if (killIndex < max && cameraMaxLeadAheadOfNextKillX > 0f)
                {
                    var nextEnemyTr = (_enemySnapshot[killIndex] as MonoBehaviour)?.transform;
                    if (nextEnemyTr != null)
                    {
                        float lead = (killIndex == 0) ? cameraInitialLeadX : 0f;
                        float cap = nextEnemyTr.position.x + cameraMaxLeadAheadOfNextKillX + lead;
                        float capped = Mathf.Min(nextX, cap);
                        nextX = Mathf.Max(currentX, capped);
                    }
                }

                if (!Mathf.Approximately(nextX, camTr.position.x))
                {
                    var p = camTr.position;
                    p.x = nextX;
                    camTr.position = p;
                }
            }

            while (killIndex < max)
            {
                var e = _enemySnapshot[killIndex];
                var uo = e as UnityEngine.Object;
                if (e == null || uo == null || !e.IsAlive)
                {
                    killIndex++;
                    continue;
                }

                var tr = (e as MonoBehaviour)?.transform;
                if (tr == null)
                {
                    killIndex++;
                    continue;
                }

                float triggerX = tr.position.x + deathTriggerOffsetX;
                if (camTr.position.x < triggerX)
                {
                    break;
                }

                float now = Time.time;
                if (minGap > 0f && lastKillAt > -100f && (now - lastKillAt) < minGap)
                {
                    break;
                }

                e.TakeDamage(damagePerHit);
                lastKillAt = now;

                if (hitParticlePrefab != null && spawnedParticles < Mathf.Max(0, maxHitParticles))
                {
                    Transform parent = hitParticleParent != null ? hitParticleParent : transform;
                    var ps = Instantiate(hitParticlePrefab, tr.position, Quaternion.identity, parent);
                    SetUnscaledTimeRecursive(ps, true);
                    ps.Play(true);
                    spawnedParticles++;

                    float destroyAfter = Mathf.Max(0f, hitParticleAutoDestroySeconds);
                    if (destroyAfter > 0f) Destroy(ps.gameObject, destroyAfter);
                }

                killIndex++;

                if (minGap <= 0f)
                {
                    break;
                }
            }

            if (killIndex >= max && EnemyRegistry.Enemies.Count == 0)
            {
                break;
            }

            if (cameraSweepEnabled && camTr.position.x >= endX && killIndex < max)
            {
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        bool killedLastIndexed = killIndex >= max;

        if (ensureCameraReachesEndXAfterKills && cameraSweepEnabled)
        {
            while (!token.IsCancellationRequested && camTr.position.x < endX)
            {
                token.ThrowIfCancellationRequested();
                float dt = Mathf.Max(0f, Time.deltaTime);
                float x = Mathf.Min(endX, camTr.position.x + (sweepSpeed * dt));
                var p = camTr.position;
                p.x = x;
                camTr.position = p;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        if (disableCameraFollowDuringSweep && cameraFollow != null && restoreCameraFollowAfterFinish)
        {
            cameraFollow.IsActive = prevFollowActive;
        }

        await UniTask.WaitUntil(() => EnemyRegistry.Enemies.Count == 0, PlayerLoopTiming.Update, token);
        return killedLastIndexed;
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

