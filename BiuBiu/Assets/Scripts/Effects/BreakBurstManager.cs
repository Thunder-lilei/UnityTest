using BiuBiu.Core;
using UnityEngine;

namespace BiuBiu.Effects
{
    /// <summary>
    /// 击碎破碎粒子管理器（轻量静态入口）。
    /// 负责碎片模板的惰性创建/持有（热重载后自愈——工程纪律），并提供 SpawnBreakBurst 触发爆发。
    /// 碎片 GameObject 全部经 Core.ObjectPool 获取/回收，禁止裸 Instantiate（性能预算：红档满蓄力本就低频）。
    /// </summary>
    public static class BreakBurstManager
    {
        /// <summary>碎片模板（含 SpriteRenderer + BreakShard 组件；禁用态 + DontDestroyOnLoad 以适配对象池）</summary>
        private static GameObject shardTemplate;

        /// <summary>
        /// 取碎片模板（惰性创建）。
        /// 模板复用现有像素块（GreyBoxFactory.Square 提供的 1×1 纯色方块 sprite），不引入新美术资产。
        /// 模板须保持禁用 + 跨场景不销毁，保证 ObjectPool 以它为键正确池化。
        /// </summary>
        private static GameObject GetTemplate()
        {
            if (shardTemplate == null)
            {
                // 复用现有像素块 sprite（与敌人/玩家方块同源，纯色几何最终形态）
                Sprite boxSprite = GreyBoxFactory.Square;
                shardTemplate = new GameObject("[BreakShardTemplate]");
                var sr = shardTemplate.AddComponent<SpriteRenderer>();
                sr.sprite = boxSprite;
                sr.sortingLayerName = "Effects"; // 特效叠加层（工程约定）
                sr.sortingOrder = 5;
                shardTemplate.AddComponent<BreakShard>();
                shardTemplate.SetActive(false);           // 模板统一禁用态
                Object.DontDestroyOnLoad(shardTemplate);  // 跨场景/热重载保留
            }
            return shardTemplate;
        }

        /// <summary>
        /// 在命中点生成一组破碎碎片（仅红档击碎档调用）。
        /// </summary>
        /// <param name="pos">命中点（世界坐标）</param>
        /// <param name="hitDir">弹丸飞行方向（用于放射基准；内部归一化）</param>
        /// <param name="enemyColor">被命中敌人主色（取自 EnemyBase2D.MainColor）</param>
        public static void SpawnBreakBurst(Vector2 pos, Vector2 hitDir, Color enemyColor)
        {
            GameObject template = GetTemplate();

            Vector2 baseDir = hitDir.sqrMagnitude > 0.0001f ? hitDir.normalized : Vector2.up;

            // ---- 普通碎片：细碎高速迸溅（数量多、飞得远，营造“碎屑四溅”） ----
            int count = GameBalance.BreakShardCount; // 每发碎片数（数值文档口径）
            for (int i = 0; i < count; i++)
            {
                // 以命中方向为基准向四周放射（±半圈随机分布，front-biased 更符合“击碎迸溅”观感）
                float spread = GameBalance.BreakShardSpread; // 放射张角（弧度，半角）
                float angle = Mathf.Atan2(baseDir.y, baseDir.x) + Random.Range(-spread, spread);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // 初速范围（tile/s，数值文档口径）
                float speed = Random.Range(GameBalance.BreakShardSpeedMin, GameBalance.BreakShardSpeedMax);
                Vector2 vel = dir * speed;

                // 像素尺寸范围（数值文档口径）→ 用于 scale 初始化
                float px = Random.Range(GameBalance.BreakShardSizeMin, GameBalance.BreakShardSizeMax);

                // 错开生成，避免所有碎片完全重叠（细微抖动提升“迸溅”层次）
                Vector3 spawnPos = (Vector3)pos + (Vector3)(dir * Random.Range(0f, 0.15f));

                GameObject shard = Core.ObjectPool.Get(template, spawnPos, Quaternion.identity);
                BreakShard bs = shard.GetComponent<BreakShard>();
                bs.Initialize(enemyColor, GameBalance.BreakShardLife, vel, px);
            }

            // ---- 大块碎片：少量大而慢的碎块（强化“崩解冲击”，让满蓄力击碎更夸张） ----
            int chunkCount = GameBalance.BreakChunkCount;
            for (int i = 0; i < chunkCount; i++)
            {
                float spread = GameBalance.BreakShardSpread;
                float angle = Mathf.Atan2(baseDir.y, baseDir.x) + Random.Range(-spread, spread);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float speed = Random.Range(GameBalance.BreakChunkSpeedMin, GameBalance.BreakChunkSpeedMax);
                Vector2 vel = dir * speed;

                float px = Random.Range(GameBalance.BreakChunkSizeMin, GameBalance.BreakChunkSizeMax);

                Vector3 spawnPos = (Vector3)pos + (Vector3)(dir * Random.Range(0f, 0.15f));

                GameObject chunk = Core.ObjectPool.Get(template, spawnPos, Quaternion.identity);
                BreakShard bs = chunk.GetComponent<BreakShard>();
                bs.Initialize(enemyColor, GameBalance.BreakChunkLife, vel, px);
            }
        }
    }
}
