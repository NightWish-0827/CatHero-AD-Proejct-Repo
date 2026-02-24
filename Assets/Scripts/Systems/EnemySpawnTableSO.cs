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
    [Tooltip("에디터에서 식별용")]
    public string id;

    public GameObject prefab;

    [Min(0f)]
    public float weight = 1f;

    [Header("Type")]
    public EnemySpawnSurface surface = EnemySpawnSurface.Ground;

    public EnemySpawnPattern pattern = EnemySpawnPattern.Single;

    [Header("Spawn Height")]
    [Tooltip("스포너의 지면 기준 Y(groundY)에 더해지는 오프셋. Air 타입은 여기서 높이를 올리면 됩니다. Ground 타입은 무시됩니다.")]
    public float spawnYOffset = 0f;

    [Tooltip("스폰 이후 Y를 고정할지 여부. Ground 타입은 항상 true로 강제됩니다.")]
    public bool lockYAfterSpawn = true;

    [Header("Single")]
    [Min(0f)]
    public float singleVerticalJitter = 0f;

    [Header("Cluster")]
    public Vector2Int clusterCount = new Vector2Int(3, 6);

    [Min(0f)]
    public float clusterSpacingX = 0.8f;

    [Min(0f)]
    public float clusterVerticalJitter = 0.35f;
}
