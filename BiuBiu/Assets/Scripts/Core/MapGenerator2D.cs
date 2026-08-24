using System.Collections.Generic;
using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 地图生成：80×80 地面 + 四面边界墙 + 俄罗斯方块（Tetromino）形状的内部障碍。
    /// 边界墙不可破坏；内部障碍由 1×1 单元拼成，可被满蓄力弹丸击碎。
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class MapGenerator2D : MonoBehaviour
    {
        // ===== 俄罗斯方块形状库（标准 7 种，单元坐标，原点在形状左上角） =====
        // 每个形状是一组 (col,row) 单元偏移；运行时随机旋转 0/90/180/270°
        private static readonly List<Vector2Int[]> TetrominoShapes = new List<Vector2Int[]>
        {
            // I
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) },
            // O
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            // T
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) },
            // S
            new[] { new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            // Z
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) },
            // J
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
            // L
            new[] { new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
        };

        /// <summary>所有障碍单元的世界中心（供敌人生成避让 Raycast 查询、碎墙移除判定）</summary>
        public List<Vector2> ObstacleUnits { get; private set; } = new List<Vector2>();

        public const float GridSize = 1f;            // 障碍单元尺寸
        public const float WallThickness = 2f;       // 边界墙厚度
        public const float MapSize = 80f;            // 地图边长（tile）
        private const float Half = MapSize / 2f;

        private EdgeCollider2D _edge;
        private readonly List<Vector2> _wallPts = new List<Vector2>();

        public void Generate(Sprite[] groundVariants)
        {
            _edge = GetComponent<EdgeCollider2D>();

            BuildGround(groundVariants);
            BuildWall();
            GenerateObstacles();
        }

        // ===== 地面 =====
        private void BuildGround(Sprite[] groundVariants)
        {
            int n = (int)MapSize;
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    var sr = new GameObject($"Ground_{x}_{y}").AddComponent<SpriteRenderer>();
                    sr.sprite = groundVariants[Random.Range(0, groundVariants.Length)];
                    sr.color = new Color(0.18f, 0.18f, 0.18f);
                    sr.transform.position = new Vector3(x + 0.5f, y + 0.5f, 1f);
                    sr.sortingOrder = -10;
                    sr.transform.SetParent(transform);
                }
            }
        }

        // ===== 四面边界墙（不可破坏，仅阻挡） =====
        private void BuildWall()
        {
            float h = Half + WallThickness;  // 外缘
            _wallPts.Add(new Vector2(-h, -h));
            _wallPts.Add(new Vector2(h, -h));
            _wallPts.Add(new Vector2(h, h));
            _wallPts.Add(new Vector2(-h, h));
            _wallPts.Add(new Vector2(-h, -h));
            _edge.SetPoints(_wallPts);

            // 四块物理墙体（BoxCollider2D，不可破坏，无 DestructibleObstacle 组件）
            SpawnWall(new Vector2(0, -Half - WallThickness / 2f), new Vector2(MapSize + WallThickness * 2, WallThickness));
            SpawnWall(new Vector2(0, Half + WallThickness / 2f), new Vector2(MapSize + WallThickness * 2, WallThickness));
            SpawnWall(new Vector2(-Half - WallThickness / 2f, 0), new Vector2(WallThickness, MapSize));
            SpawnWall(new Vector2(Half + WallThickness / 2f, 0), new Vector2(WallThickness, MapSize));
        }

        private void SpawnWall(Vector2 center, Vector2 size)
        {
            var wall = new GameObject("Wall") { layer = LayerMask.NameToLayer("Obstacle") };
            wall.transform.position = center;
            wall.transform.SetParent(transform);
            wall.AddComponent<BoxCollider2D>().size = size;
            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = GreyBoxFactory.Square;
            sr.color = Color.white;
            sr.size = size;
            sr.sortingOrder = -5;
        }

        // ===== 俄罗斯方块障碍 =====
        private void GenerateObstacles()
        {
            ObstacleUnits.Clear();
            int count = GameBalance.ObstacleCount;
            int placed = 0, attempts = 0;
            int maxAttempts = count * 30;

            while (placed < count && attempts < maxAttempts)
            {
                attempts++;
                var shape = TetrominoShapes[Random.Range(0, TetrominoShapes.Count)];
                int rot = Random.Range(0, 4);
                var cells = Rotate(shape, rot);

                // 随机中心（保证整体在地图内）
                float cx = Random.Range(-Half + 3f, Half - 3f);
                float cy = Random.Range(-Half + 3f, Half - 3f);

                // 计算所有单元世界坐标
                var worldCells = new List<Vector2>();
                bool ok = true;
                foreach (var c in cells)
                {
                    Vector2 p = new Vector2(cx + c.x, cy + c.y);
                    // 边界内检查（留 1 tile 余量）
                    if (p.x < -Half + 1f || p.x > Half - 1f || p.y < -Half + 1f || p.y > Half - 1f)
                    { ok = false; break; }
                    // 出生点留空
                    if (Vector2.Distance(p, Vector2.zero) < GameBalance.ObstacleSpawnClearRadius)
                    { ok = false; break; }
                    worldCells.Add(p);
                }
                if (!ok) continue;

                // 与已有障碍单元间距检查
                foreach (var p in worldCells)
                {
                    foreach (var u in ObstacleUnits)
                    {
                        if (Vector2.Distance(p, u) < GameBalance.ObstacleMinSpacing)
                        { ok = false; break; }
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
            root.SetParent(transform);

            foreach (var p in worldCells)
            {
                ObstacleUnits.Add(p);

                var cell = new GameObject("ObstacleCell") { layer = LayerMask.NameToLayer("Obstacle") };
                cell.transform.position = p;
                cell.transform.SetParent(root);

                var col = cell.AddComponent<BoxCollider2D>();
                col.size = Vector2.one * GridSize;

                var destructible = cell.AddComponent<DestructibleObstacle>();
                destructible.Root = root;

                // 视觉：白块
                var sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = GreyBoxFactory.Square;
                sr.color = Color.white;
                sr.sortingOrder = -5;
                sr.size = Vector2.one * GridSize;
            }
        }

        /// <summary>将形状旋转 rot×90°（围绕原点）</summary>
        private static List<Vector2Int> Rotate(Vector2Int[] shape, int rot)
        {
            var res = new List<Vector2Int>();
            foreach (var c in shape)
            {
                int x = c.x, y = c.y;
                for (int i = 0; i < (rot & 3); i++)
                {
                    // 90° 顺时针：(x,y) -> (y, -x)
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
