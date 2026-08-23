using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BiuBiu.Core
{
    /// <summary>
    /// 程序化大地图生成器（设计文档 3 章：tile 单场景 + 边界墙 + 场景内障碍墙）。
    /// 墙壁使用运行时生成的大理石纹理贴图。
    /// </summary>
    public class MapGenerator2D : MonoBehaviour
    {
        private static Texture2D marbleTexture;
        private static Texture2D groundTexture;
        /// <summary>
        /// 生成整张地图（Grid + 地面 Tilemap + 四面边界墙）。
        /// </summary>
        /// <param name="groundVariants">地面 tile 精灵（[0]=基础格 [1]=变体格；仅 1 张时全铺基础格）。为空或元素缺失时回退到运行时生成的地面纹理，不依赖外部美术资产。</param>
        public void Generate(Sprite[] groundVariants)
        {
            // ---- 地面：Grid + Tilemap（像素基线 PPU 32 → cell 尺寸恰为 1 tile） ----
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(transform, false);
            gridGo.AddComponent<Grid>();

            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform, false);
            var ground = groundGo.AddComponent<Tilemap>();
            var groundRenderer = groundGo.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = 0; // 地面在一切之下（血迹 1 / 墙 2 / 单位 10+）

            // 解析有效地面 sprite（场景注入若因资源删除而缺失则回退运行时生成）
            Sprite baseSprite = ResolveGroundSprite(groundVariants, 0);
            Sprite variantSprite = ResolveGroundSprite(groundVariants, 1);

            // 运行时创建 Tile（Tile 本质 ScriptableObject，可运行时实例化——免 .asset 落盘）
            var baseTile = CreateTile(baseSprite);
            var variantTile = variantSprite != null ? CreateTile(variantSprite) : null;

            // 铺满 80×80：变体按撒点比例随机点缀（GameBalance.GroundVariantRatio ~15%）
            int size = GameBalance.MapSizeTiles;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool useVariant = variantTile != null && Random.value < GameBalance.GroundVariantRatio;
                    ground.SetTile(new Vector3Int(x, y, 0), useVariant ? variantTile : baseTile);
                }
            }
            ground.CompressBounds(); // 收紧 Tilemap 包围盒（渲染剔除优化）

            // ---- 边界墙：四面墙带（厚度 BorderWallThickness=2；坐标覆盖 0~80 世界范围） ----
            float t = GameBalance.BorderWallThickness;
            float s = GameBalance.MapSizeTiles;
            BuildWall("Wall_Bottom", new Vector2(s * 0.5f, t * 0.5f), new Vector2(s, t));
            BuildWall("Wall_Top", new Vector2(s * 0.5f, s - t * 0.5f), new Vector2(s, t));
            BuildWall("Wall_Left", new Vector2(t * 0.5f, s * 0.5f), new Vector2(t, s));
            BuildWall("Wall_Right", new Vector2(s - t * 0.5f, s * 0.5f), new Vector2(t, s));

            // ---- 场景内障碍墙（数值文档 v2.4 第12章） ----
            GenerateObstacles();
        }

        /// <summary>已生成的障碍中心列表（供 EnemySpawner2D 避让查询）</summary>
        public List<Vector2> ObstacleCenters { get; private set; } = new List<Vector2>();

        /// <summary>已生成的障碍尺寸列表（与 ObstacleCenters 一一对应）</summary>
        public List<Vector2> ObstacleSizes { get; private set; } = new List<Vector2>();

        /// <summary>
        /// 撒障碍墙（数值文档第12章）：40 处 2×1 tile 横/竖随机石墙段。
        /// 约束：出生点半径 5 tile 禁区 / 距边界墙 ≥3 tile / 两障碍中心间距 ≥4 tile。
        /// </summary>
        private void GenerateObstacles()
        {
            ObstacleCenters.Clear();
            ObstacleSizes.Clear();

            Vector2 spawn = GameBalance.PlayerSpawnPoint;
            float clearR = GameBalance.ObstacleSpawnClearRadius;
            float borderMargin = GameBalance.ObstacleBorderMargin;
            float minSpacing = GameBalance.ObstacleMinSpacing;
            float t = GameBalance.BorderWallThickness;
            int count = GameBalance.ObstacleCount;
            int maxAttempts = count * 5; // 总尝试上限防死循环

            int placed = 0;
            int attempts = 0;
            while (placed < count && attempts < maxAttempts)
            {
                attempts++;
                // 随机位置（边界墙内缩 borderMargin）
                float minX = t + borderMargin;
                float maxX = GameBalance.MapSizeTiles - t - borderMargin;
                float minY = t + borderMargin;
                float maxY = GameBalance.MapSizeTiles - t - borderMargin;
                Vector2 pos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

                // 出生点禁区检查
                if ((pos - spawn).sqrMagnitude < clearR * clearR) continue;

                // 与已有障碍间距检查
                bool tooClose = false;
                foreach (var c in ObstacleCenters)
                {
                    if ((pos - c).sqrMagnitude < minSpacing * minSpacing) { tooClose = true; break; }
                }
                if (tooClose) continue;

                // 横/竖随机
                bool horizontal = Random.value < 0.5f;
                Vector2 size = horizontal ? new Vector2(2f, 1f) : new Vector2(1f, 2f);

                BuildObstacle($"Obstacle_{placed}", pos, size);
                ObstacleCenters.Add(pos);
                ObstacleSizes.Add(size);
                placed++;
            }
            Debug.Log($"[MapGenerator2D] 障碍墙：放置 {placed}/{count} 处（尝试 {attempts} 次）");
        }

        /// <summary>建一段障碍墙：大理石纹理方块 + BoxCollider2D</summary>
        private void BuildObstacle(string name, Vector2 center, Vector2 size)
        {
            var obs = GreyBoxFactory.MakeBox(name, false, Color.white, size);
            obs.transform.SetParent(transform, false);
            obs.transform.position = center;
            var obsSr = obs.GetComponent<SpriteRenderer>();
            obsSr.sprite = CreateMarbleSprite();
            obsSr.color = Color.white;
            obsSr.sortingOrder = 2;
            var col = obs.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        /// <summary>运行时生成大理石纹理 Sprite（32×32，白灰纹路+暗色脉络）</summary>
        private static Sprite CreateMarbleSprite()
        {
            if (marbleTexture == null)
            {
                int size = 32;
                marbleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                marbleTexture.filterMode = FilterMode.Point;
                var px = new Color32[size * size];
                var rnd = new System.Random(42);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // 基底：浅灰白
                        byte baseVal = (byte)(200 + rnd.Next(0, 30));
                        // 脉络：几条暗色曲线
                        float nx = (float)x / size;
                        float ny = (float)y / size;
                        float vein = Mathf.Abs(Mathf.Sin(nx * 12f + ny * 3f) * Mathf.Cos(ny * 8f));
                        if (vein < 0.15f)
                        {
                            baseVal = (byte)(baseVal * 0.5f); // 暗脉络
                        }
                        // 噪点
                        baseVal = (byte)Mathf.Clamp(baseVal + rnd.Next(-8, 8), 0, 255);
                        px[y * size + x] = new Color32(baseVal, (byte)(baseVal * 0.97f), (byte)(baseVal * 0.93f), 255);
                    }
                }
                marbleTexture.SetPixels32(px);
                marbleTexture.Apply();
            }
            return Sprite.Create(marbleTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>运行时创建 Tile（sprite 注入；Tile 即 ScriptableObject 实例）</summary>
        private static Tile CreateTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            return tile;
        }

        /// <summary>
        /// 取地面 sprite：优先用场景注入的有效 sprite；缺失（如美术资源已删除）时回退运行时生成的绿灰地表纹理。
        /// </summary>
        private static Sprite ResolveGroundSprite(Sprite[] variants, int index)
        {
            if (variants != null && index < variants.Length && variants[index] != null)
                return variants[index];
            return CreateGroundSprite();
        }

        /// <summary>运行时生成地面纹理 Sprite（32×32，绿灰地表+暗脉络，与大理石墙区分）</summary>
        private static Sprite CreateGroundSprite()
        {
            if (groundTexture == null)
            {
                int size = 32;
                groundTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                groundTexture.filterMode = FilterMode.Point;
                var px = new Color32[size * size];
                var rnd = new System.Random(7);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // 基底：暗绿灰
                        byte g = (byte)(90 + rnd.Next(0, 26));
                        byte r = (byte)(g * 0.78f);
                        byte b = (byte)(g * 0.7f);
                        // 脉络：少量暗色斑点
                        float nx = (float)x / size;
                        float ny = (float)y / size;
                        float vein = Mathf.Abs(Mathf.Sin(nx * 9f + ny * 5f));
                        if (vein < 0.12f) { r = (byte)(r * 0.6f); g = (byte)(g * 0.6f); b = (byte)(b * 0.6f); }
                        px[y * size + x] = new Color32(r, g, b, 255);
                    }
                }
                groundTexture.SetPixels32(px);
                groundTexture.Apply();
            }
            return Sprite.Create(groundTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>建一段墙：大理石纹理方块 + BoxCollider2D</summary>
        private void BuildWall(string name, Vector2 center, Vector2 size)
        {
            var wall = GreyBoxFactory.MakeBox(name, false, Color.white, size);
            wall.transform.SetParent(transform, false);
            wall.transform.position = center;
            var wallSr = wall.GetComponent<SpriteRenderer>();
            wallSr.sprite = CreateMarbleSprite();
            wallSr.color = Color.white;
            wallSr.sortingOrder = 2; // 压地面(0)，被单位(10+)盖住
            wall.AddComponent<BoxCollider2D>();
        }
    }
}
