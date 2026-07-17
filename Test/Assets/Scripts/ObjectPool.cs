using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;              // 池中对象 Prefab
    public int initialSize = 10;           // 初始预创建数量
    public Transform parent;               // 生成对象父物体

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Start()
    {
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, parent);
        }

        obj.SetActive(true);

        var pooled = obj.GetComponent<IPooledObject>();
        if (pooled != null)
            pooled.OnSpawn();

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}

public interface IPooledObject
{
    void OnSpawn();
}
