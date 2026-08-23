using BiuBiu.Drops;
using BiuBiu.Enemies;
using BiuBiu.Player;
using BiuBiu.UI;
using BiuBiu.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BiuBiu.Core
{
    /// <summary>
    /// Main 场景运行时装配器（M1-9；设计文档 14 章工程架构的运行时落点）。
    /// 场景内只需本组件（含序列化引用）与主相机——地图/玩家/系统/UI 全部运行时构建：
    /// - 重载 Main（再战/重开/回标题再进）= 全新装配一局（重开零成本，无中途存档）；
    /// - 持久化引用（输入资产/材质/tile 精灵）由场景序列化注入本组件，装配时分发给运行时对象
    ///   （团结引擎纪律：材质一律持久化 .mat 资产注入，运行时不动态创建）；
    /// - 玩家构建走「先禁用 → 注入引用 → 再激活」：保证各组件 Awake 一次拿到完整引用
    ///   （AddComponent 即刻触发 Awake，先注入后激活可避开空引用）。
    /// 装配顺序：相机兜底 → 地图 → 玩家 → 系统 → UI；Start 末调 GameBootstrap.OnMainSceneReady 开局。
    /// </summary>
    public class RuntimeSceneBuilder : MonoBehaviour
    {
        [Header("场景序列化引用（构建时分发给运行时对象）")]
        [Tooltip("输入资产（Settings/PlayerControls.inputactions）——注入 PlayerController")]
        [SerializeField] private InputActionAsset actions;

        [Tooltip("玩家闪白材质（Mat_SpriteFlash）——受击闪白 shader 载体（MPB 写 _FlashAmount）")]
        [SerializeField] private Material playerFlashMaterial;

        [Tooltip("地面 tile 精灵（[0] 基础格 / [1] 变体格）")]
        [SerializeField] private Sprite[] groundVariants;

        private void Awake()
        {
            EnsureCamera();
            BuildMap();
            BuildPlayer();
            BuildSystems();
            BuildUi();
        }

        private void Start()
        {
            // 装配完成 → 开局（重载 Main=全新一局；直接 Play Main 调试时 EnsureInstance 兜底创建引导者）
            GameBootstrap.EnsureInstance().OnMainSceneReady();
        }

        /// <summary>
        /// 相机兜底：场景未摆相机时程序化构建（正交 Size=9 像素基线 + 跟随 + 震屏）。
        /// 正常流程 Main 场景已由编辑器脚本摆好相机，本方法仅在缺位时接管。
        /// </summary>
        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera"; // Camera.main 依赖此 tag
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f; // 像素风基线（开发文档工程规范）
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.09f, 0.06f); // 地图外深色底
            go.transform.position = new Vector3(GameBalance.PlayerSpawnPoint.x, GameBalance.PlayerSpawnPoint.y, -10f);
            go.AddComponent<AudioListener>();
            go.AddComponent<CameraFollow>();
            go.AddComponent<CameraTrauma>();
        }

        /// <summary>地图：80×80 地面 + 边界墙（MapGenerator2D）</summary>
        private void BuildMap()
        {
            var mapRoot = new GameObject("MapRoot");
            mapRoot.transform.SetParent(transform, false);
            mapRoot.AddComponent<MapGenerator2D>().Generate(groundVariants);
        }

        /// <summary>
        /// 玩家：灰盒圆点占位 + 弹弓蓄力武器。
        /// 构建在地图正中央（GameBalance.PlayerSpawnPoint）。
        /// </summary>
        private void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.SetActive(false); // 先禁用：AddComponent 的 Awake 不立即触发
            player.transform.position = GameBalance.PlayerSpawnPoint;

            // 视觉：灰盒圆点
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = GreyBoxFactory.Circle;
            sr.color = new Color(0.96f, 0.9f, 0.72f);
            sr.sortingOrder = 10;
            if (playerFlashMaterial != null) sr.sharedMaterial = playerFlashMaterial;

            player.AddComponent<HitFlash>();

            // 物理碰撞（主角与敌人可碰撞，碰撞受伤）
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = player.AddComponent<CircleCollider2D>();
            col.radius = GameBalance.PlayerCollisionRadius;
            col.isTrigger = true; // 触发器：不阻挡敌人移动，但触发碰撞事件

            // 输入与武器（弹弓蓄力武器）
            var controller = player.AddComponent<PlayerController>();
            controller.actions = actions;
            player.AddComponent<SlingWeapon>();

            player.SetActive(true);
        }

        /// <summary>系统层：血迹管理 / 刷怪器（EnemyData 走 Resources 兜底加载）</summary>
        private void BuildSystems()
        {
            var systems = new GameObject("Systems");
            systems.transform.SetParent(transform, false);
            systems.AddComponent<DropManager>();
            systems.AddComponent<EnemySpawner2D>();
        }

        /// <summary>UI 层：HUD 与 ESC 暂停（升级卡/战报面板为惰性单例，无需摆放）</summary>
        private void BuildUi()
        {
            var ui = new GameObject("UI");
            ui.transform.SetParent(transform, false);
            ui.AddComponent<GameHud>();
            ui.AddComponent<PauseMenu>();
        }
    }
}
