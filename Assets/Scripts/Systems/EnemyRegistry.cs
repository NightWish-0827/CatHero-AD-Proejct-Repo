using System.Collections.Generic;
using UnityEngine;

public static class EnemyRegistry
{
    private static readonly List<IEnemy> ActiveEnemies = new List<IEnemy>();

    public static IReadOnlyList<IEnemy> Enemies => ActiveEnemies;

    public static void Register(IEnemy enemy)
    {
        if (enemy != null && !ActiveEnemies.Contains(enemy))
        {
            ActiveEnemies.Add(enemy);
        }
    }

    public static void Unregister(IEnemy enemy)
    {
        ActiveEnemies.Remove(enemy);
    }

    public static IEnemy GetNearest(Vector3 position)
    {
        IEnemy nearest = null;
        float minSqrDist = float.MaxValue;

        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            var e = ActiveEnemies[i];
            if (e == null || !e.IsAlive)
            {
                ActiveEnemies.RemoveAt(i);
                continue;
            }

            var tr = (e as MonoBehaviour)?.transform;
            if (tr == null) continue;

            float sqrDist = (tr.position - position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = e;
            }
        }

        return nearest;
    }
}
