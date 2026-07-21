using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;              // 池中对象 Prefab
    public int initialSize = 10;           // 初始预创建数量
    public Transform parent;               // 生成对象父物体

    private Queue<GameObject> pool = new Queue<GameObject>();

    /// <summary>预创建指定数量的对象放入池中</summary>
    void Start()
    {
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    /// <summary>从池中取出对象并激活，池空时自动创建新实例</summary>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成旋转</param>
    /// <returns>激活的 GameObject</returns>
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

    /// <summary>将对象失活并放回池中</summary>
    /// <param name="obj">要回收的 GameObject</param>
    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}

/// <summary>对象池对象接口，Spawn 时调用 OnSpawn 初始化</summary>
public interface IPooledObject
{
    /// <summary>从池中取出时调用，用于重置状态</summary>
    void OnSpawn();
}
