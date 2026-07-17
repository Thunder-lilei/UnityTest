using UnityEngine;

public class Footprint : MonoBehaviour
{
    public float lifetime = 2f;            // 脚印存活时间（秒）
    private Renderer rend;                // 脚印渲染器
    private MaterialPropertyBlock mpb;     // 材质属性块（避免创建实例材质）
    private float timer = 0f;             // 计时器

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", new Color(1, 1, 1, 1));
        rend.SetPropertyBlock(mpb);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = 1f - (timer / lifetime);
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
        rend.SetPropertyBlock(mpb);

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}