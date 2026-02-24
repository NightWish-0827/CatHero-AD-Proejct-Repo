using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>캣 히어로: 웨이브별 적 스폰. PoolManager 사용.</summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private EnemyStatDB defaultStats = new EnemyStatDB
    {
        maxHealth = 10f,
        moveSpeed = 2f,
        attackDamage = 5f,
        attackRange = 1.5f,
        attackCooldown = 1.5f
    };

    [Header("Wave")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemiesPerWave = 5;
    [SerializeField] private float waveCooldown = 3f;

    private Transform playerTarget;
    private CancellationTokenSource cts;
    private int currentWave;
    private bool isRunning;

    public void StartSpawning(Transform playerTarget)
    {
        this.playerTarget = playerTarget;
        if (isRunning) return;

        isRunning = true;
        cts?.Cancel();
        cts = new CancellationTokenSource();
        currentWave = 0;

        SpawnLoopAsync(cts.Token).Forget();
    }

    public void StopSpawning()
    {
        isRunning = false;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private async UniTaskVoid SpawnLoopAsync(CancellationToken token)
    {
        while (isRunning && !token.IsCancellationRequested)
        {
            currentWave++;
            GameEvents.OnWaveStarted.OnNext(currentWave);

            for (int i = 0; i < maxEnemiesPerWave && isRunning && !token.IsCancellationRequested; i++)
            {
                SpawnEnemy();
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(spawnInterval),
                    cancellationToken: token);
            }

            GameEvents.OnWaveCleared.OnNext(currentWave);
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(waveCooldown),
                cancellationToken: token);
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || playerTarget == null || PoolManager.Instance == null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        var instance = PoolManager.Instance.Spawn(enemyPrefab, pos, Quaternion.identity);

        if (instance.TryGetComponent(out IEnemy enemy))
        {
            enemy.Initialize(playerTarget, defaultStats);
        }
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}
