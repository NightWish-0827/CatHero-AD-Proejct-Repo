using System.Collections.Generic;
using UnityEngine;

public static class EnemyRegistry
{
    private static readonly List<IEnemy> ActiveEnemies = new List<IEnemy>();

    public static IReadOnlyList<IEnemy> Enemies
    {
        get
        {
            Cleanup();
            return ActiveEnemies;
        }
    }

    public static void Register(IEnemy enemy)
    {
        if (enemy == null) return;
        if ((enemy as UnityEngine.Object) == null) return;

        Cleanup();
        if (!ActiveEnemies.Contains(enemy))
        {
            ActiveEnemies.Add(enemy);
        }
    }

    public static void Unregister(IEnemy enemy)
    {
        ActiveEnemies.Remove(enemy);
    }

    private static void Cleanup()
    {
        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            var e = ActiveEnemies[i];
            var uo = e as UnityEngine.Object;
            if (uo == null)
            {
                ActiveEnemies.RemoveAt(i);
                continue;
            }

            if (e is MonoBehaviour mb && !mb.gameObject.activeInHierarchy)
            {
                ActiveEnemies.RemoveAt(i);
                continue;
            }
        }
    }

    public static IEnemy GetFrontMostInRange(Vector3 origin, float range, bool onlyAhead = true)
    {
        Cleanup();

        IEnemy best = null;
        float bestDx = float.MaxValue;
        float bestSqrDist = float.MaxValue;

        float rangeSqr = range * range;

        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            var e = ActiveEnemies[i];
            if (e == null) continue;
            if (!e.IsAlive) continue;

            var tr = (e as MonoBehaviour)?.transform;
            if (tr == null)
            {
                ActiveEnemies.RemoveAt(i);
                continue;
            }

            Vector3 pos = tr.position;
            float dx = pos.x - origin.x;
            if (onlyAhead && dx < 0f) continue;

            float sqrDist = (pos - origin).sqrMagnitude;
            if (sqrDist > rangeSqr) continue;

            if (dx < bestDx || (Mathf.Approximately(dx, bestDx) && sqrDist < bestSqrDist))
            {
                best = e;
                bestDx = dx;
                bestSqrDist = sqrDist;
            }
        }

        return best;
    }

    public static IEnemy GetNearest(Vector3 position)
    {
        Cleanup();

        IEnemy nearest = null;
        float minSqrDist = float.MaxValue;

        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            var e = ActiveEnemies[i];
            if (e == null) continue;
            if (!e.IsAlive) continue;

            var tr = (e as MonoBehaviour)?.transform;
            if (tr == null)
            {
                ActiveEnemies.RemoveAt(i);
                continue;
            }

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
