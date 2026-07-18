using UnityEngine;

public class Footprint : MonoBehaviour, IPooledObject
{
    public float lifetime = 2f;            // 脚印存活时间（秒）
    private Renderer rend;                // 脚印渲染器
    private Material mat;                  // 实例材质（对象池复用，不会泄漏）
    private float timer = 0f;             // 计时器
    private ObjectPool pool;               // 所属对象池引用

    private static readonly Color FootprintColor = Color.white;  // 白色不染色，贴图自带颜色

    public void OnSpawn()
    {
        timer = 0f;
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        if (mat == null) mat = rend.material;
        mat.color = FootprintColor;
    }

    public void SetPool(ObjectPool pool)
    {
        this.pool = pool;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = Mathf.Max(0f, 1f - (timer / lifetime));
        mat.color = new Color(FootprintColor.r, FootprintColor.g, FootprintColor.b, alpha);

        if (timer >= lifetime && pool != null)
            pool.Despawn(gameObject);
    }

    void OnDisable()
    {
        if (mat != null)
            mat.color = FootprintColor;
    }
}