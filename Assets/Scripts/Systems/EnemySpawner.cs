using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
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

    public void StartSpawning(Transform playerTarget)
    {
        this.playerTarget = playerTarget;
        if (isRunning) return;

        isRunning = true;
        cts?.Cancel();
        cts = new CancellationTokenSource();
        currentWave = 0;
        nextIntervalSpawnAt = Time.time;

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
            // Z-세미 사이클(Interval) 방식:
            // - Interval 시간은 유지(최소 간격)한다.
            // - 하지만 이전 Interval의 적을 모두 제거하지 못했다면 다음 Interval 스폰을 "지연"한다. (누적 스폰 방지)
            await UniTask.WaitUntil(
                () => Time.time >= nextIntervalSpawnAt && EnemyRegistry.Enemies.Count == 0,
                cancellationToken: token);

            currentWave++;
            GameEvents.OnWaveStarted.OnNext(currentWave);

            SpawnIntervalPack();
            nextIntervalSpawnAt = Time.time + spawnInterval;

            await UniTask.WaitUntil(
                () => EnemyRegistry.Enemies.Count == 0,
                cancellationToken: token);

            GameEvents.OnWaveCleared.OnNext(currentWave);
        }
    }

    private void SpawnIntervalPack()
    {
        if (playerTarget == null || PoolManager.Instance == null) return;

        bool hasTable = spawnTable != null && spawnTable.Entries != null && spawnTable.Entries.Count > 0;
        if (!hasTable && enemyPrefab == null) return;

        int count = Mathf.Max(1, maxEnemiesPerWave);
        Vector3 basePos = GetBaseSpawnPos();
        float spacingX = Mathf.Max(0f, clusterSpacingX);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = basePos + new Vector3(i * spacingX, 0f, 0f);
            if (hasTable) SpawnFromTable(pos);
            else SpawnSingle(enemyPrefab, pos);
        }
    }

    private Vector3 GetBaseSpawnPos()
    {
        if (playerTarget == null) return Vector3.zero;

        // 플레이어가 좌→우로 진행하므로, 스폰은 항상 플레이어 전방(+X)에서 발생.
        // 지상 적이 플레이어의 Y(중심점)를 추적해서 "뜸" 현상이 생기지 않도록 기본 Y는 지면 고정값 사용.
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

        // 지상/공중 타입에 따라 Y 고정 처리
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
            // 폴백: 기존 프리팹(지상)을 groundY에 고정
            enemyBase.ConfigureVerticalLock(true, groundY);
        }
    }

    private void SpawnFromTable(Vector3 basePos)
    {
        var entry = ChooseEntry(spawnTable);
        if (entry == null) return;

        GameObject prefab = entry.prefab != null ? entry.prefab : enemyPrefab;
        if (prefab == null) return;

        // 4분류: (지상/공중) x (단일/군집)
        bool isGround = entry.surface == EnemySpawnSurface.Ground;
        float baseY = isGround ? groundY : (groundY + entry.spawnYOffset);
        Vector3 typedBasePos = new Vector3(basePos.x, baseY, basePos.z);

        switch (entry.pattern)
        {
            case EnemySpawnPattern.Cluster:
                SpawnCluster(prefab, typedBasePos, entry);
                break;
            default:
                float yJitter = isGround ? 0f : Mathf.Max(0f, entry.singleVerticalJitter);
                float y = typedBasePos.y + UnityEngine.Random.Range(-yJitter, yJitter);
                SpawnSingle(prefab, new Vector3(typedBasePos.x, y, typedBasePos.z), entry);
                break;
        }
    }

    private void SpawnCluster(GameObject prefab, Vector3 basePos, EnemySpawnEntry entry)
    {
        int min = Mathf.Max(2, entry.clusterCount.x);
        int max = Mathf.Max(min, entry.clusterCount.y);
        int count = Random.Range(min, max + 1);

        float spacingX = Mathf.Max(0f, entry.clusterSpacingX);
        bool isGround = entry.surface == EnemySpawnSurface.Ground;
        float yJitter = isGround ? 0f : Mathf.Max(0f, entry.clusterVerticalJitter);

        for (int i = 0; i < count; i++)
        {
            float x = basePos.x + (i * spacingX);
            float y = basePos.y + UnityEngine.Random.Range(-yJitter, yJitter);
            SpawnSingle(prefab, new Vector3(x, y, basePos.z), entry);
        }
    }

    private static EnemySpawnEntry ChooseEntry(EnemySpawnTableSO table)
    {
        if (table == null || table.Entries == null || table.Entries.Count == 0) return null;

        float total = 0f;
        for (int i = 0; i < table.Entries.Count; i++)
        {
            var e = table.Entries[i];
            if (e == null) continue;
            total += Mathf.Max(0f, e.weight);
        }

        if (total <= 0f)
        {
            for (int i = 0; i < table.Entries.Count; i++)
            {
                if (table.Entries[i] != null) return table.Entries[i];
            }
            return null;
        }

        float roll = UnityEngine.Random.value * total;
        float acc = 0f;

        for (int i = 0; i < table.Entries.Count; i++)
        {
            var e = table.Entries[i];
            if (e == null) continue;

            acc += Mathf.Max(0f, e.weight);
            if (roll <= acc) return e;
        }

        return table.Entries[table.Entries.Count - 1];
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}
