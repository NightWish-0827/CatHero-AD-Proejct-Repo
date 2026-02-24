using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

[SceneReferral]
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;

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

        var enemy = instance.GetComponentInChildren<IEnemy>();
        if (enemy != null)
        {
            enemy.Initialize(playerTarget);
        }
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}
