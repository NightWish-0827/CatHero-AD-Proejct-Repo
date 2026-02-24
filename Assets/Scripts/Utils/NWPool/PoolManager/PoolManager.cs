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
                actionOnGet: (obj) => OnTakeFromPool(obj),
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
        if (instanceToPrefabMap.TryGetValue(instance, out GameObject prefab))
        {
            poolDictionary[prefab].Release(instance);
            instanceToPrefabMap.Remove(instance);
        }
        else
        {
            Debug.LogWarning($"[PoolManager] Not registered prefab - {instance.name}");
            Destroy(instance);
        }
    }

    // --- Callback ---
    private void OnTakeFromPool(GameObject obj)
    {
        obj.SetActive(true);

        var poolable = obj.GetComponentInChildren<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }
    }

    private void OnReturnedToPool(GameObject obj)
    {
        var poolable = obj.GetComponentInChildren<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }
        obj.SetActive(false);
    }
}
// Pool Util Manager