using UnityEngine;

public class Footprint : MonoBehaviour, IPooledObject
{
    public float lifetime = 2f;            // 脚印存活时间（秒）
    private Renderer rend;                // 脚印渲染器
    private MaterialPropertyBlock mpb;     // 材质属性块（避免创建实例材质）
    private float timer = 0f;             // 计时器
    private ObjectPool pool;               // 所属对象池引用

    public void OnSpawn()
    {
        timer = 0f;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", new Color(1, 1, 1, 1));
        rend.SetPropertyBlock(mpb);
    }

    public void SetPool(ObjectPool pool)
    {
        this.pool = pool;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = 1f - (timer / lifetime);
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
        rend.SetPropertyBlock(mpb);

        if (timer >= lifetime && pool != null)
            pool.Despawn(gameObject);
    }
}