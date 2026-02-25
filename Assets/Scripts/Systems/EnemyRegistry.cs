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
        if ((enemy as UnityEngine.Object) == null) return; // destroyed/unloaded Unity object

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

            // 비활성화(풀 반환 등)된 개체는 더 이상 '활성 적'으로 취급하지 않습니다.
            if (e is MonoBehaviour mb && !mb.gameObject.activeInHierarchy)
            {
                ActiveEnemies.RemoveAt(i);
                continue;
            }
        }
    }

    /// <summary>
    /// 플레이어가 좌→우로 진행하는 게임에서 "앞줄(플레이어에 가장 가까운 전방)"을 우선 타겟팅하기 위한 API.
    /// 기본값(onlyAhead=true)일 때, origin.x 보다 큰(전방) 적들 중 origin 기준 x 방향으로 가장 가까운 적을 반환합니다.
    /// </summary>
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
                // interface 참조는 Unity null semantics가 적용되지 않아 유령 엔트리가 남을 수 있으므로 여기서 정리
                ActiveEnemies.RemoveAt(i);
                continue;
            }

            Vector3 pos = tr.position;
            float dx = pos.x - origin.x;
            if (onlyAhead && dx < 0f) continue;

            float sqrDist = (pos - origin).sqrMagnitude;
            if (sqrDist > rangeSqr) continue;

            // "전방(앞줄)부터 제거"를 위해 x방향으로 가장 가까운 적을 우선.
            // 동률이면 전체 거리(제곱)로 타이브레이크.
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
