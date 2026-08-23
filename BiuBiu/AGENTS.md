# AGENTS.md — BiuBiu 工作区须知

2D 像素风俯视角**幸存者类街机动作**（大地图波次刷怪、弹弓蓄力、每轮自动变强）。**团结引擎 TuanjieEditor 1.9.3**（内核 2022.3.62t11，C#/包体系兼容 Unity 生态），Built-in 管线，目标平台 Windows PC，PPU=32，正交相机 Size=9，Input System（支持运行时改键）。当前阶段：M1/M2 已完成（v3.0 战斗重构+旧设计清理完毕，2026-08-23），下一步 M3 系统扩展（大蜘蛛/精英/三丧尸差异化的灰盒表现——Spine 接入路线已取消，2026-08-23）。

## 必读文档（改代码前先改文档）

`Docs/` 下两份中文策划文档是开发决策唯一出处，均中文命名：
- `设计文档.md` — 设计决策（改设计先改这里）
- `开发文档.md` — 环境、目录、规范、里程碑拆解
- `数值文档.md` — **全部游戏数值唯一出处**

（原 `文案文档.md` 与 `素材需求文档.md` 已于 2026-08-23 删除：文案就地维护于代码/UI；无正式素材计划，纯色几何+少量 AI 生成素材即最终形态。）

纪律：设计/数值改动 → 先改对应文档，再动代码。另见根目录 `CODELY.md`（另一工具生成的项目上下文，部分进度信息可能滞后，以 git log 为准）。

## 目录结构

- `Assets/Scripts/{Core,Player,Weapons,Enemies,Drops,UI,Data,Editor}/` — 按模块划分；项目自身无 asmdef
- `Assets/Art/` — 自产美术（Sprites/Spine/Tilemaps/UI/VFX/Shaders）
- `Assets/Resources/Data/Enemies/` — EnemyData SO 实例（Resources 兜底加载）
- `Assets/Spine/`、`Assets/Spine Examples/` — spine-unity 4.3 第三方运行时与示例，**勿混入自产资产**
- `Docs/`、`Tools/`（含 `AssetQC.py` 素材质检脚本）

## 关键架构约定

- `Core/GameBalance.cs` 静态类与数值文档一一对应；**禁止代码内散落数值魔法数**
- 一切受击统一走 `IDamageable`（无敌模式在此拦截）
- 数据驱动：敌人用 `EnemyData` ScriptableObject（`Data/EnemyData.cs` + Resources 实例）
- 唯一武器=弹弓蓄力（`Weapons/SlingWeapon.cs` 三档蓄力：白速射/黄击飞/红击碎穿透，`PlayerProjectile.cs`）
- 波次制刷怪（`EnemySpawner2D`：1→2→4→8 翻倍；每轮全灭→PlayerStats 微增+回满血）；**无经验/升级/血瓶/掉落拾取**（2026-08-23 已清理）
- 对象池：丧尸、弹丸、投掷物、血迹等全池化
- 存档：单一永久档（PlayerPrefs，`Core/SaveSystem.cs`）=最高纪录（存活/轮次/击杀）+累计统计；无中途档
- 局内进度口径=「轮次」（等级概念已退役）
- 自定义 shader：`BiuBiu/SpriteFlash`、`BiuBiu/SpineSkeletonFlash`（受击闪白，用 MaterialPropertyBlock 写 `_FlashAmount`）
- 排序层：地面(-10)→血迹(-5)→影子(-4)→角色(0)→特效(5)→UI

## 构建与运行

- 编辑器运行：打开 `Assets/Scenes/` 下场景 → Play（Boot.unity → 自动进 Main.unity；Main 由 `RuntimeSceneBuilder` 运行时装配）
- 无自定义构建脚本；batchmode：
  `"C:/Unity1.9.3/2022.3.62t11/Editor/Tuanjie.exe" -batchmode -quit -projectPath . -logFile`
- Unity Test Framework 已装，暂无测试文件

## 开发约定

- 脚本/预制体/资产文件：PascalCase；技术文档：中文命名
- 代码注释用中文；不提其他游戏名（代码/注释/文档统一遵守）
- 提交规范：`类型: 摘要`（feat/fix/art/balance/doc）；里程碑节点打 tag
- 贴图导入基线：Filter Mode=Point、Compression=None、Mip Maps 关
- Git：`.meta` 文件必须纳入版本控制（删 .cs 必须连删 .cs.meta）；忽略 `Library/ Temp/ Logs/ UserSettings/ *.csproj *.sln` 等
- 文件安全：shell 删除命令禁止通配符与 `-Recurse` 连用；删除目录前必须先确认内容已安全转移

## 已知坑

- 团结引擎场景扩展名历史上为 `.scene`，但当前仓库实际是 `.unity`（Boot.unity/Main.unity）——以文件系统实际为准
- 团结引擎同物体禁多个 LineRenderer（多段视觉各用子物体承载）
- Play 热重载会清空普通 C# 对象引用——外部引用一律判 null 惰性自愈（工程纪律，见 GameBootstrap/PlayerController）
- spine-unity 相关 asmdef（spine-csharp 等）是第三方的，不要改
- spine-unity 4.3 编辑器代码实例化骨架会抛 NRE：走「示例场景 Additive 打开 + Instantiate」路线
