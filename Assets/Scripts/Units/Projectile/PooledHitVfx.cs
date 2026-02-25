using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// 히트 VFX 전용 풀링 컴포넌트.
/// Spawn 시 파티클을 재생하고, 충분한 시간이 지나면 자동으로 Despawn합니다.
/// </summary>
public sealed class PooledHitVfx : MonoBehaviour, IPoolable
{
    [SerializeField] private bool clearOnSpawn = true;
    [SerializeField] private bool playOnSpawn = true;

    // 예측이 어려운 파티클(서브이미터 등)을 위해 여유시간을 둡니다.
    [SerializeField] private float extraLifetimeSeconds = 0.1f;

    private ParticleSystem[] _systems;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void OnSpawn()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        if (_systems == null || _systems.Length == 0)
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        float max = 0f;
        for (int i = 0; i < _systems.Length; i++)
        {
            var ps = _systems[i];
            if (ps == null) continue;

            if (clearOnSpawn) ps.Clear(true);
            if (playOnSpawn) ps.Play(true);

            var main = ps.main;
            float dur = main.duration;
            float life = 0f;
            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                life = Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);
            }
            else
            {
                life = main.startLifetime.constantMax;
            }
            max = Mathf.Max(max, dur + life);
        }

        AutoDespawnAsync(max + Mathf.Max(0f, extraLifetimeSeconds), _cts.Token).Forget();
    }

    public void OnDespawn()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid AutoDespawnAsync(float seconds, CancellationToken token)
    {
        if (seconds <= 0f) seconds = 0.1f;
        await UniTask.Delay(System.TimeSpan.FromSeconds(seconds), cancellationToken: token);
        if (token.IsCancellationRequested) return;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

