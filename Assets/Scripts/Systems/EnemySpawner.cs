using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Random = UnityEngine.Random;

[SceneReferral]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnDistanceAhead = 8f;

    [SerializeField] private float groundY = 0f;

    [SerializeField] private EnemySpawnTableSO spawnTable;
    [SerializeField, Min(0f)] private float clusterSpacingX = 0.8f;

    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemiesPerWave = 5;

    private Transform playerTarget;
    private CancellationTokenSource cts;
    private int currentWave;
    private bool isRunning;
    private float nextIntervalSpawnAt;
    private int nextEntryIndex;

    public void StartSpawning(Transform playerTarget)
    {
        this.playerTarget = playerTarget;
        if (isRunning) return;

        isRunning = true;
        cts?.Cancel();
        cts = new CancellationTokenSource();
        currentWave = 0;
        nextIntervalSpawnAt = Time.time;
        nextEntryIndex = 0;

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
            await UniTask.WaitUntil(
                () => Time.time >= nextIntervalSpawnAt && EnemyRegistry.Enemies.Count == 0,
                cancellationToken: token);

            currentWave++;
            GameEvents.OnWaveStarted.OnNext(currentWave);

            SpawnIntervalPack();

            await UniTask.WaitUntil(
                () => EnemyRegistry.Enemies.Count == 0,
                cancellationToken: token);

            GameEvents.OnWaveCleared.OnNext(currentWave);

            nextIntervalSpawnAt = Time.time + spawnInterval;
        }
    }

    private void SpawnIntervalPack()
    {
        if (playerTarget == null || PoolManager.Instance == null) return;

        bool hasTable = spawnTable != null && spawnTable.Entries != null && spawnTable.Entries.Count > 0;
        if (!hasTable && enemyPrefab == null) return;

        Vector3 basePos = GetBaseSpawnPos();

        if (hasTable)
        {
            var entry = ChooseNextEntrySequential(spawnTable);
            if (entry == null) return;
            SpawnFromEntry(entry, basePos);
            return;
        }

        int count = Mathf.Max(1, maxEnemiesPerWave);
        float spacingX = Mathf.Max(0f, clusterSpacingX);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = basePos + new Vector3(i * spacingX, 0f, 0f);
            SpawnSingle(enemyPrefab, pos);
        }
    }

    private Vector3 GetBaseSpawnPos()
    {
        if (playerTarget == null) return Vector3.zero;

        return new Vector3(
            playerTarget.position.x + spawnDistanceAhead,
            groundY,
            playerTarget.position.z);
    }

    private void SpawnSingle(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return;
        var instance = PoolManager.Instance.Spawn(prefab, pos, Quaternion.identity);
        var enemy = instance.GetComponentInChildren<IEnemy>();
        if (enemy != null) enemy.Initialize(playerTarget);
    }

    private void SpawnSingle(GameObject prefab, Vector3 pos, EnemySpawnEntry entry)
    {
        if (prefab == null) return;
        var instance = PoolManager.Instance.Spawn(prefab, pos, Quaternion.identity);

        var enemy = instance.GetComponentInChildren<IEnemy>();
        if (enemy != null) enemy.Initialize(playerTarget);

        var enemyBase = enemy as EnemyBase;
        if (enemyBase != null && entry != null)
        {
            bool isGround = entry.surface == EnemySpawnSurface.Ground;
            bool lockY = isGround || entry.lockYAfterSpawn;
            float lockedY = isGround ? groundY : pos.y;
            enemyBase.ConfigureVerticalLock(lockY, lockedY);
        }
        else if (enemyBase != null)
        {
            enemyBase.ConfigureVerticalLock(true, groundY);
        }
    }

    private void SpawnFromEntry(EnemySpawnEntry entry, Vector3 basePos)
    {
        if (entry == null) return;

        GameObject prefab = entry.prefab != null ? entry.prefab : enemyPrefab;
        if (prefab == null) return;

        bool isGround = entry.surface == EnemySpawnSurface.Ground;
        float baseY = isGround ? groundY : (groundY + entry.spawnYOffset);
        Vector3 typedBasePos = new Vector3(basePos.x, baseY, basePos.z);

        SpawnGroup(prefab, typedBasePos, entry, isGround);
    }

    private EnemySpawnEntry ChooseNextEntrySequential(EnemySpawnTableSO table)
    {
        if (table == null || table.Entries == null || table.Entries.Count == 0) return null;

        int n = table.Entries.Count;
        for (int attempt = 0; attempt < n; attempt++)
        {
            int idx = nextEntryIndex % n;
            nextEntryIndex = (idx + 1) % n;

            var e = table.Entries[idx];
            if (e != null) return e;
        }

        return null;
    }

    private void SpawnGroup(GameObject prefab, Vector3 basePos, EnemySpawnEntry entry, bool isGround)
    {
        if (entry == null) return;

        if (entry.groupOffsets != null && entry.groupOffsets.Count > 0)
        {
            for (int i = 0; i < entry.groupOffsets.Count; i++)
            {
                Vector2 off = entry.groupOffsets[i];
                float x = basePos.x + off.x;
                float y = isGround ? groundY : (basePos.y + off.y);
                SpawnSingle(prefab, new Vector3(x, y, basePos.z), entry);
            }
            return;
        }

        int count = Mathf.Max(1, entry.groupCount);

        if (count <= 1 && entry.pattern == EnemySpawnPattern.Cluster)
        {
            int min = Mathf.Max(2, entry.clusterCount.x);
            int max = Mathf.Max(min, entry.clusterCount.y);
            count = Random.Range(min, max + 1);
        }

        float spacingX = entry != null ? Mathf.Max(0f, entry.clusterSpacingX) : 0f;
        if (Mathf.Approximately(spacingX, 0f))
        {
            spacingX = Mathf.Max(0f, clusterSpacingX);
        }

        float yJitter = 0f;
        if (!isGround)
        {
            yJitter = (count <= 1)
                ? Mathf.Max(0f, entry.singleVerticalJitter)
                : Mathf.Max(0f, entry.clusterVerticalJitter);
        }

        for (int i = 0; i < count; i++)
        {
            float x = basePos.x + (i * spacingX);
            float y = basePos.y + UnityEngine.Random.Range(-yJitter, yJitter);
            if (isGround) y = groundY;
            SpawnSingle(prefab, new Vector3(x, y, basePos.z), entry);
        }
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}
