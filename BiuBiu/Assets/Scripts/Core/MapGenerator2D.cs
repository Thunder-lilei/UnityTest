using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BiuBiu.Core
{
    /// <summary>
    /// 程序化大地图生成器：80×80 地面（Tilemap + 运行时纹理兜底）+ 四面边界墙（大理石纹理）+
    /// 俄罗斯方块（Tetromino）形状的内部障碍（由 1×1 单元拼成，可被满蓄力弹丸击碎）。
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class MapGenerator2D : MonoBehaviour
    {
        private static Texture2D marbleTexture;
        private static Texture2D groundTexture;

        /// <summary>所有障碍单元的世界中心（供敌人生成避让查询、碎墙移除判定）</summary>
        public List<Vector2> ObstacleUnits { get; private set; } = new List<Vector2>();

        // ===== 俄罗斯方块形状库（标准 7 种，单元坐标，原点在形状左上角） =====
        // 每个形状是一组 (col,row) 单元偏移；运行时随机旋转 0/90/180/270°
        private static readonly List<Vector2Int[]> TetrominoShapes = new List<Vector2Int[]>
        {
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) }, // I
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // O
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) }, // T
            new[] { new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // S
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) }, // Z
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) }, // J
            new[] { new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) }, // L
        };

        public void Generate(Sprite[] groundVariants)
        {
            BuildGround(groundVariants);
            BuildWall();
            GenerateObstacles();
        }

        // ===== 地面：Grid + Tilemap（像素基线 PPU 32 → cell 尺寸恰为 1 tile） =====
        private void BuildGround(Sprite[] groundVariants)
        {
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(transform, false);
            gridGo.AddComponent<Grid>();

            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform, false);
            var ground = groundGo.AddComponent<Tilemap>();
            var groundRenderer = groundGo.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = 0;

            Sprite baseSprite = ResolveGroundSprite(groundVariants, 0);
            Sprite variantSprite = ResolveGroundSprite(groundVariants, 1);

            var baseTile = CreateTile(baseSprite);
            var variantTile = variantSprite != null ? CreateTile(variantSprite) : null;

            int size = GameBalance.MapSizeTiles;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool useVariant = variantTile != null && Random.value < GameBalance.GroundVariantRatio;
                    ground.SetTile(new Vector3Int(x, y, 0), useVariant ? variantTile : baseTile);
                }
            }
            ground.CompressBounds();
        }

        // ===== 四面边界墙（不可破坏，仅阻挡 + 满蓄力反弹面） =====
        private void BuildWall()
        {
            float t = GameBalance.BorderWallThickness;
            float s = GameBalance.MapSizeTiles;
            BuildWall("Wall_Bottom", new Vector2(s * 0.5f, t * 0.5f), new Vector2(s, t));
            BuildWall("Wall_Top", new Vector2(s * 0.5f, s - t * 0.5f), new Vector2(s, t));
            BuildWall("Wall_Left", new Vector2(t * 0.5f, s * 0.5f), new Vector2(t, s));
            BuildWall("Wall_Right", new Vector2(s - t * 0.5f, s * 0.5f), new Vector2(t, s));
        }

        private void BuildWall(string name, Vector2 center, Vector2 size)
        {
            var wall = GreyBoxFactory.MakeBox(name, false, Color.white, size);
            wall.transform.SetParent(transform, false);
            wall.transform.position = center;
            var wallSr = wall.GetComponent<SpriteRenderer>();
            wallSr.sprite = CreateMarbleSprite();
            wallSr.color = Color.white;
            wallSr.sortingOrder = 2;
            wall.AddComponent<BoxCollider2D>();
        }

        // ===== 俄罗斯方块内部障碍（可破坏） =====
        private void GenerateObstacles()
        {
            ObstacleUnits.Clear();

            Vector2 spawn = GameBalance.PlayerSpawnPoint;
            float clearR = GameBalance.ObstacleSpawnClearRadius;
            float borderMargin = GameBalance.ObstacleBorderMargin;
            float minSpacing = GameBalance.ObstacleMinSpacing;
            float t = GameBalance.BorderWallThickness;
            int count = GameBalance.ObstacleCount;
            int maxAttempts = count * 30;

            int placed = 0;
            int attempts = 0;
            while (placed < count && attempts < maxAttempts)
            {
                attempts++;
                var shape = TetrominoShapes[Random.Range(0, TetrominoShapes.Count)];
                int rot = Random.Range(0, 4);
                var cells = Rotate(shape, rot);

                // 随机中心（保证整体在地图内）
                float minX = t + borderMargin + 1f;
                float maxX = GameBalance.MapSizeTiles - t - borderMargin - 1f;
                float minY = t + borderMargin + 1f;
                float maxY = GameBalance.MapSizeTiles - t - borderMargin - 1f;
                float cx = Random.Range(minX, maxX);
                float cy = Random.Range(minY, maxY);

                var worldCells = new List<Vector2>();
                bool ok = true;
                foreach (var c in cells)
                {
                    Vector2 p = new Vector2(cx + c.x, cy + c.y);
                    if (p.x < t + borderMargin || p.x > GameBalance.MapSizeTiles - t - borderMargin ||
                        p.y < t + borderMargin || p.y > GameBalance.MapSizeTiles - t - borderMargin)
                    { ok = false; break; }
                    if ((p - spawn).sqrMagnitude < clearR * clearR)
                    { ok = false; break; }
                    worldCells.Add(p);
                }
                if (!ok) continue;

                foreach (var p in worldCells)
                {
                    foreach (var u in ObstacleUnits)
                    {
                        if ((p - u).sqrMagnitude < minSpacing * minSpacing) { ok = false; break; }
                    }
                    if (!ok) break;
                }
                if (!ok) continue;

                PlaceObstacle(worldCells);
                placed++;
            }

            Debug.Log($"[MapGenerator2D] 障碍（俄罗斯方块）：放置 {placed}/{count} 处（尝试 {attempts} 次）");
        }

        private void PlaceObstacle(List<Vector2> worldCells)
        {
            var root = new GameObject("Obstacle_Tetro").transform;
            root.SetParent(transform, false);

            foreach (var p in worldCells)
            {
                ObstacleUnits.Add(p);

                var cell = GreyBoxFactory.MakeBox("ObstacleCell", false, Color.white, Vector2.one);
                cell.transform.SetParent(root, false);
                cell.transform.position = p;
                var sr = cell.GetComponent<SpriteRenderer>();
                sr.sprite = CreateMarbleSprite();
                sr.color = Color.white;
                sr.sortingOrder = 2;

                var col = cell.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;

                var destructible = cell.AddComponent<DestructibleObstacle>();
                destructible.Root = root;
            }
        }

        // ===== 运行时纹理 / Tile 工具 =====
        private static Sprite CreateMarbleSprite()
        {
            if (marbleTexture == null)
            {
                int size = 32;
                marbleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                marbleTexture.hideFlags = HideFlags.HideAndDontSave; // 跨场景重载存活（地图瓦片 static 缓存）
                marbleTexture.filterMode = FilterMode.Point;
                var px = new Color32[size * size];
                var rnd = new System.Random(42);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        byte baseVal = (byte)(200 + rnd.Next(0, 30));
                        float nx = (float)x / size;
                        float ny = (float)y / size;
                        float vein = Mathf.Abs(Mathf.Sin(nx * 12f + ny * 3f) * Mathf.Cos(ny * 8f));
                        if (vein < 0.15f) baseVal = (byte)(baseVal * 0.5f);
                        baseVal = (byte)Mathf.Clamp(baseVal + rnd.Next(-8, 8), 0, 255);
                        px[y * size + x] = new Color32(baseVal, (byte)(baseVal * 0.97f), (byte)(baseVal * 0.93f), 255);
                    }
                }
                marbleTexture.SetPixels32(px);
                marbleTexture.Apply();
            }
            var ms = Sprite.Create(marbleTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            ms.hideFlags = HideFlags.HideAndDontSave; // 与纹理一同跨场景存活
            return ms;
        }

        private static Tile CreateTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            return tile;
        }

        private static Sprite ResolveGroundSprite(Sprite[] variants, int index)
        {
            if (variants != null && index < variants.Length && variants[index] != null)
                return variants[index];
            return CreateGroundSprite();
        }

        private static Sprite CreateGroundSprite()
        {
            if (groundTexture == null)
            {
                int size = 32;
                groundTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                groundTexture.hideFlags = HideFlags.HideAndDontSave; // 跨场景重载存活（地图瓦片 static 缓存）
                groundTexture.filterMode = FilterMode.Point;
                var px = new Color32[size * size];
                var rnd = new System.Random(7);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        byte g = (byte)(90 + rnd.Next(0, 26));
                        byte r = (byte)(g * 0.78f);
                        byte b = (byte)(g * 0.7f);
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
            var gs = Sprite.Create(groundTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            gs.hideFlags = HideFlags.HideAndDontSave; // 与纹理一同跨场景存活
            return gs;
        }

        /// <summary>将形状旋转 rot×90°（围绕原点顺时针）</summary>
        private static List<Vector2Int> Rotate(Vector2Int[] shape, int rot)
        {
            var res = new List<Vector2Int>();
            foreach (var c in shape)
            {
                int x = c.x, y = c.y;
                for (int i = 0; i < (rot & 3); i++)
                {
                    int nx = y;
                    int ny = -x;
                    x = nx; y = ny;
                }
                res.Add(new Vector2Int(x, y));
            }
            return res;
        }
    }
}
