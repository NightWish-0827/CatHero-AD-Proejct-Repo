using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnTable", menuName = "CatHero/Enemy Spawn Table")]
public class EnemySpawnTableSO : ScriptableObject
{
    [SerializeField] private List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();
    public IReadOnlyList<EnemySpawnEntry> Entries => entries;
}

public enum EnemySpawnSurface
{
    Ground,
    Air
}

public enum EnemySpawnPattern
{
    Single,
    Cluster
}

[Serializable]
public class EnemySpawnEntry
{
    public string id;

    public GameObject prefab;

    [Min(0f)]
    public float weight = 1f;

    public EnemySpawnSurface surface = EnemySpawnSurface.Ground;

    public EnemySpawnPattern pattern = EnemySpawnPattern.Single;

    public float spawnYOffset = 0f;

    public bool lockYAfterSpawn = true;

    [Min(0f)]
    public float singleVerticalJitter = 0f;

    [Min(1)]
    public int groupCount = 1;

    [SerializeField] public List<Vector2> groupOffsets = new List<Vector2>();

    public Vector2Int clusterCount = new Vector2Int(3, 6);

    [Min(0f)]
    public float clusterSpacingX = 0.8f;

    [Min(0f)]
    public float clusterVerticalJitter = 0.35f;
}
