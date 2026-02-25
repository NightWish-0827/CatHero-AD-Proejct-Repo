using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// Pool Util Manager
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    private Dictionary<GameObject, IObjectPool<GameObject>> poolDictionary = new Dictionary<GameObject, IObjectPool<GameObject>>();
    private Dictionary<GameObject, GameObject> instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (obj) => OnTakeFromPool(obj, prefab),
                actionOnRelease: (obj) => OnReturnedToPool(obj),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true, 
                defaultCapacity: 50,
                maxSize: 1000 
            );
        }

        GameObject instance = poolDictionary[prefab].Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        
        instanceToPrefabMap[instance] = prefab;

        return instance;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null) return;

        // 이미 풀로 돌아가 비활성화된 오브젝트를 중복 Despawn하는 경우가 있습니다.
        // 이때는 경고/부작용 없이 조용히 무시합니다.
        if (!instance.activeInHierarchy && !instanceToPrefabMap.ContainsKey(instance)) return;

        // 1) direct match
        if (instanceToPrefabMap.TryGetValue(instance, out GameObject prefab))
        {
            poolDictionary[prefab].Release(instance);
            instanceToPrefabMap.Remove(instance);
        }
        else
        {
            // 2) child object passed? try root
            var root = instance.transform != null ? instance.transform.root.gameObject : null;
            if (root != null && root != instance && instanceToPrefabMap.TryGetValue(root, out prefab))
            {
                poolDictionary[prefab].Release(root);
                instanceToPrefabMap.Remove(root);
                return;
            }

            // 3) fallback: try find registered parent up the chain
            Transform t = instance.transform;
            while (t != null)
            {
                var go = t.gameObject;
                if (instanceToPrefabMap.TryGetValue(go, out prefab))
                {
                    poolDictionary[prefab].Release(go);
                    instanceToPrefabMap.Remove(go);
                    return;
                }
                t = t.parent;
            }

            // last resort: avoid hard break in gameplay. disable and warn once.
            Debug.LogWarning($"[PoolManager] Not registered prefab - {instance.name}");
            instance.SetActive(false);
        }
    }

    // --- Callback ---
    private void OnTakeFromPool(GameObject obj, GameObject prefab)
    {
        if (prefab != null)
        {
            obj.transform.localScale = prefab.transform.localScale;
        }
        obj.SetActive(true);

        var poolable = obj.GetComponentInChildren<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();

            // Allow despawning by child(poolable) object too.
            var mb = poolable as MonoBehaviour;
            if (mb != null && mb.gameObject != obj)
            {
                instanceToPrefabMap[mb.gameObject] = prefab;
            }
        }
    }

    private void OnReturnedToPool(GameObject obj)
    {
        var poolable = obj.GetComponentInChildren<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();

            var mb = poolable as MonoBehaviour;
            if (mb != null && mb.gameObject != obj)
            {
                instanceToPrefabMap.Remove(mb.gameObject);
            }
        }
        obj.SetActive(false);
    }
}
// Pool Util Manager