using System.Collections;
using UnityEngine;

/// <summary>敌人死亡溶解特效：替换材质 → 动画 DissolveAmount 0→1 → 销毁</summary>
public class DissolveEffect : MonoBehaviour
{
    [Tooltip("溶解材质")]
    public Material dissolveMaterial;

    [Tooltip("溶解持续时间（秒）")]
    public float dissolveDuration = 1.0f;

    private SkinnedMeshRenderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    /// <summary>启动溶解：替换材质 → 动画 → 销毁</summary>
    public void StartDissolve()
    {
        // 1. 禁用碰撞体和寻路（敌人已死，不再参与碰撞和移动）
        GetComponent<Collider>().enabled = false;
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // 2. 禁用动画：不禁用的话，溶解过程中 Animator 会继续播放 Crawl 动画，
        //    敌人身体一边溶解一边扭动，视觉上很违和。禁用后角色冻结在死亡瞬间的姿势
        var anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        // 3. 替换所有 SkinnedMeshRenderer 的材质为溶解材质
        foreach (var smr in renderers)
        {
            // 为每个 renderer 创建材质实例（避免共享材质互相影响）
            Material matInstance = new Material(dissolveMaterial);

            // 传入原始贴图和颜色
            if (smr.sharedMaterial != null && smr.sharedMaterial.mainTexture != null)
            {
                matInstance.SetTexture("_BaseMap", smr.sharedMaterial.mainTexture);
                matInstance.SetColor("_BaseColor", smr.sharedMaterial.color);
            }

            smr.material = matInstance;
        }

        // 4. 启动溶解动画
        StartCoroutine(DissolveCoroutine());
    }

    /// <summary>逐帧增加 DissolveAmount 从 0 到 1，完成后销毁</summary>
    /// <remarks>
    /// IEnumerator 是 Unity 协程的固定返回类型。协程可以在多帧内逐步执行，
    /// 配合 yield return null 实现"每帧推进一步"的动画效果。
    /// 执行完毕后协程自动结束，不需要手动停止。
    /// </remarks>
    IEnumerator DissolveCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            float t = elapsed / dissolveDuration;

            foreach (var smr in renderers)
            {
                if (smr.material != null)
                    smr.material.SetFloat("_DissolveAmount", t);
            }

            elapsed += Time.deltaTime;
            // yield return null：暂停执行，下一帧从这里继续
            // 这是"逐帧"实现的核心：不是一次性改到 1，而是分多帧慢慢推进
            yield return null;
        }

        // 确保 DissolveAmount 到达 1（完全消失）
        foreach (var smr in renderers)
        {
            if (smr.material != null)
                smr.material.SetFloat("_DissolveAmount", 1f);
        }

        // 延迟一帧后销毁（确保最后一帧渲染完成）
        yield return null;
        Destroy(gameObject);
    }
}
