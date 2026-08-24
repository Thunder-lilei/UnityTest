# UnityTest

Unity 开发学习仓库，存储学习过程中的所有产出。仓库本身是一个多项目实验体，未来将包含多个独立的 Unity 子项目，每个子项目对应不同的学习主题或玩法实验。

## 仓库结构

```
UnityTest/
├── Test/                      # 子项目1：基于 Roll-a-Ball 的 3D 生存游戏
├── Wuxia/                     # 子项目2：武侠动作游戏 Demo（俯视角）
├── BiuBiu/                    # 子项目3：2D 俯视角幸存者类街机动作游戏（弹弓蓄力/灰盒美术）
└── README.md                  # 仓库总览
```

| 子项目 | 状态 | 学习主题 |
|--------|------|----------|
| [Test](./Test) | 进行中 | Roll-a-Ball 教程扩展 → 3D 生存/战斗/升级游戏 |
| [Wuxia](./Wuxia) | 进行中 | 武侠动作游戏 → C# · Lua · VFX Graph · 动作游戏开发 |
| [BiuBiu](./BiuBiu) | 进行中 | 2D 俯视角幸存者 → 弹弓三段蓄力 · 敌人 AI（近战横扫/远程直线/精英八方向投掷/Boss八方向扇形横扫+冲撞）· 按轮次登场波次成长 · 纯灰盒美术 |

> 后续新增子项目时，在此表中追加一行，并在 `UnityTest/` 根目录下新建对应文件夹。

---

## 子项目：Test

基于 [Roll-a-Ball](https://learn.unity.com/project/roll-a-ball) 教程的 Unity 学习项目，在官方教程基础上扩展了 AI 生成角色、火球攻击、血量/经验系统、血瓶掉落、升级选择系统、敌人持续生成、音效系统和脚印系统。

### 环境要求

- **引擎**：团结引擎 2022.3.62t11（兼容 Unity 2022.3 LTS）
- **渲染管线**：Universal Render Pipeline (URP)
- **输入系统**：经典 Input Manager

### 项目结构

```
Test/
├── Assets/
│   ├── Audio/SFX/             # 音效（EnemyDeath/FireballLaunch/MonsterHit + 旧 SFX）
│   ├── Sprites/UI/            # 升级图标+脚印贴图+冲刺图标
│   ├── Models/                # 3D 模型（角色/僵尸/血瓶）
│   ├── Kaykit/                # KayKit 低多边形素材包（岩石/树木/火把/宝箱）
│   ├── Kenney/                # Kenney 低多边形素材包（自然/建筑/栅栏）
│   ├── Quaternius/            # Quaternius 素材包（僵尸/自然/建筑/RTS）
│   ├── Prefabs/               # 预制体（PickUp/DynamicBox/FireBall/HealthPotion/EnemyHealthBar/EnemyTank/EnemyBoss/SlashEffect/Tiles）
│   ├── Scenes/
│   │   └── 迷你游戏.scene       # 主关卡（含 NavMesh 烘焙数据 + 瓦片地图）
│   ├── Scripts/                # C# 脚本
│   │   ├── PlayerMovement.cs  # 玩家移动+朝向+闪避+脚印
│   │   ├── PlayerCombat.cs    # 火球自动发射（OverlapSphere索敌+定时）
│   │   ├── PlayerHealth.cs    # 血量管理+受击DPS上限+死亡+游戏结束
│   │   ├── PlayerInteraction.cs # 拾取经验/血瓶
│   │   ├── EnemyData.cs       # ScriptableObject 敌人配置
│   │   ├── EnemyMovement.cs    # 敌人 NavMesh 追逐 + 死亡掉落 + 溶解
│   │   ├── EnemySpawner.cs     # 敌人持续生成（数据驱动配置+难度递增）
│   │   ├── MeleeCombat.cs     # 近战斩击（自动半圆范围攻击+Dot检测）
│   │   ├── SlashEffect.cs     # 斩击粒子特效（定时销毁）
│   │   ├── FireBall.cs         # 火球飞行与碰撞
│   │   ├── DissolveEffect.cs   # 死亡溶解特效（材质替换+协程动画）
│   │   ├── Footprint.cs        # 脚印渐隐消失
│   │   ├── AudioManager.cs     # 音效管理器（单例+音量控制）
│   │   ├── HealthBar.cs        # 血量条 UI
│   │   ├── ExpBar.cs           # 经验条 UI + 升级系统
│   │   ├── UpgradeSystem.cs    # 升级选择系统（Lua驱动+暂停/应用效果）
│   │   ├── UpgradeCard.cs      # 升级卡片 UI（图标+悬浮高亮+点击回调）
│   │   ├── LuaManager.cs      # xLua 运行时管理器（LuaEnv生命周期）
│   │   ├── PauseMenu.cs       # 暂停菜单（ESC+面板互斥+鼠标管理）
│   │   ├── SettingsManager.cs # 设置管理（音量Slider+PlayerPrefs持久化）
│   │   ├── MagnetDetector.cs   # 磁吸范围检测
│   │   ├── PickupItem.cs       # 拾取物被吸引飞行
│   │   ├── ObjectPool.cs       # 通用对象池（Spawn/Despawn 复用）
│   │   ├── CameraController.cs # 摄像机跟随
│   │   ├── Rotator.cs          # 收集物旋转动画
│   │   ├── ETFGRotation.cs    # 斩击特效旋转脚本
│   │   ├── ShockwaveEffect.cs # 升级冲击波（伤害+击退+扩散特效）
│   │   ├── ShockwaveVFX.cs    # 冲击波视觉特效（圆环扩散+渐隐）
│   │   └── Game.asmdef        # 主程序集定义
│   ├── Shaders/               # 自定义 Shader（Dissolve.shader + Dissolve.mat）
│   ├── Effects/                # VFX 特效（FireBallEnhanced.vfx + Slash/ + ShockwavePrefab + ShockwaveMesh + ShockwaveMat）
│   ├── Tilemaps/              # 瓦片地图（RuleTiles + Sprites + Textures）
│   ├── Resources/Icons/       # 升级图标（7张AI生成透明PNG）
│   ├── Lua/                   # Lua 配置文件（upgrades.lua.txt）
│   ├── XLua/                  # xLua 框架源码
│   ├── Codely/Fonts/          # Noto Sans SC TMP fallback 字体
│   ├── Settings/              # URP 渲染配置
│   ├── Tests/                 # EditMode 测试（9个文件+Tests.asmdef+RunTestsEditor）
│   ├── TextMesh Pro/         # TMP 资源（微软雅黑+Noto Sans SC）
├── Packages/
└── ProjectSettings/
```

### 游戏玩法

- **WASD / 方向键**：控制角色移动（恒定速度，非物理力驱动）
- **空格**：闪避冲刺，含 0.2 秒无敌帧，2 秒冷却
- **ESC**：暂停菜单（继续/设置/重新开始/退出）
- 火球和斩击**自动释放**，无需手动操作（类似吸血鬼幸存者）
- 角色自动朝向移动方向，行走时留下渐隐脚印
- 收集经验方块升级，火球消灭敌人也会掉落经验
- 敌人被消灭后有概率掉落血瓶，拾取可恢复血量
- 敌人持续从屏幕外刷新，碰到玩家扣血，血量归零则失败
- 失败后弹出面板，可选择**重新开始**或**退出游戏**

### 扩展功能（相对原版教程）

| 功能 | 说明 |
|------|------|
| AI 生成角色 | 通过 Meshy AI + TJGenerators 插件生成 Humanoid 角色（含 Idle/Walk/Run/Motion 动画） |
| Animator 状态机 | Speed 参数驱动 Idle ↔ Walk ↔ Run 过渡，Action 触发特殊动作 |
| 恒定速度移动 | 使用 rb.velocity 替代 AddForce，避免加速感；保留 Y 轴速度避免穿模 |
| 朝向移动方向 | Quaternion.Slerp 平滑转向 |
| 火球攻击 | 自动定时发射火球（OverlapSphere 索敌，锁定最近敌人），VFX Graph 粒子特效，升级可缩短间隔 |
| 近战斩击 | 自动半圆范围攻击（Dot 检测前方扇形），SlashEffect 粒子特效，升级可提升伤害/缩短冷却 |
| 闪避系统 | 空格冲刺，0.2 秒无敌帧，2 秒冷却，UI 冷却图标扇形恢复 |
| 血量系统 | HealthBar：200 HP，敌人接触扣血（DPS 上限 40/s 防秒杀），归零则失败 |
| 血瓶掉落 | 敌人死亡 30% 概率掉落血瓶，拾取恢复 30 HP，血量满时不可拾取 |
| 经验/升级 | ExpBar：收集经验方块 +10 EXP，满 100 升级，每级 maxExp +20 |
| 升级选择系统 | 升级时暂停游戏，五选三随机：最大血量/移动速度/火球数量/吸取范围/火球冷却/斩击伤害/斩击冷却，含图标和悬浮高亮 |
| xLua 热更 | 升级数据从 Lua 文件加载（upgrades.lua.txt），新增升级只需改 Lua 不改 C# 代码 |
| 瓦片地图 | 96×96=9216 片瓦片，5 种地形（草地/河流/石子路/河岸/路边），程序化纹理变体 |
| 设置界面 | 暂停菜单（ESC）+ 音量控制（主音量/音效 Slider + PlayerPrefs 持久化） |
| 敌人持续生成 | EnemySpawner：屏幕外刷新，最多 500 个，难度递增（100+lv×20） |
| 难度递增 | 每10秒：生成更快（0.3-lv×0.015）、上限更高（+20）、血量更高（+1） |
| 敌人血量 | 僵尸初始2血，火球不再一击必杀，头顶显示血条 |
| 多种敌人类型 | 普通僵尸 / 快速僵尸(HP1,Speed5,运行时设置差异属性) / 坦克僵尸(HP6,Speed1.5,1.5x体型,Zombie_Chubby) / Boss(HP20+,2.5x,Zombie_Ribcage) |
| 敌人死亡掉落 | 敌人被火球消灭后在死亡位置生成经验方块 |
| 音效系统 | AudioManager 单例：11 种音效（火球发射/命中/敌人死亡/受伤/死亡/拾取经验/拾取血瓶/升级/升级确认/闪避/游戏结束） |
| 脚印系统 | 移动时左右交替生成脚印，2 秒渐隐消失，程序化贴图+URP透明材质 |
| 敌人追逐 | 使用 NavMesh 实现敌人自动寻路追踪玩家 |
| 对象池 | 火球和脚印预创建实例复用，减少 GC 压力 |
| Layer 碰撞矩阵 | Player(8)/FireBall(9)/PickUp(10)，替代逐对 Physics.IgnoreCollision |
| Boss 敌人 | 每30秒生成一个 Boss（2.5x体型，HP20+，必掉血瓶，掉3个经验） |
| 计时器 | 右上角显示游戏存活时间（mm:ss） |
| 游戏结束面板 | 胜利/失败时弹出 UI 面板，暂停游戏（Time.timeScale = 0） |
| 重新开始 | 通过 SceneManager 重新加载当前场景 |
| 退出游戏 | Application.Quit() |
| URP 渲染管线 | 从 Built-in 迁移至 Universal Render Pipeline |
| 溶解 Shader | 手写 URP HLSL Shader（噪声驱动 AlphaClip + 边缘发光），敌人死亡时触发溶解消散 |
| ScriptableObject | 敌人数据驱动配置（.asset 文件），Inspector 调参数不改代码 |
| 事件解耦 | C# Action 事件系统（OnDashStateChanged/OnPlayerDied），组件间无直接引用 |
| 命名空间 | 脚本按模块分组（Game.Player/Enemy/Combat/Audio/UI/Systems）+ asmdef 程序集 |
| EditMode 测试 | 66 个自动化测试用例（ObjectPool/HealthBar/ExpBar/EnemyData/PlayerCombat/PlayerHealth/PlayerMovement/PauseMenu/MeleeCombat/ShockwaveEffect） |
| 升级冲击波 | 升级时先播放 1 秒冲击波（蓝色圆环扩散+伤害+NavMeshAgent 击退），播放完毕再弹出升级选择面板 |
| 中文支持 | TextMeshPro 使用微软雅黑字体资产 |

### 版本差异说明

本项目基于团结引擎 2022.3 开发，与教程使用的 Unity 6.3 存在以下差异：

| 差异项 | 教程（Unity 6.3） | 本项目（团结 2022.3） |
|--------|-------------------|----------------------|
| 输入系统 | 新版 Input System（OnMove 回调） | 经典 Input Manager（Input.GetAxis） |
| 材质属性 | Base Map | Base Color（已迁移至 URP） |
| NavMeshSurface | 预装 AI Navigation 包 | 需手动安装 |
| TMP 中文 | - | 需生成中文字体资产 |
| 渲染管线 | URP（默认） | URP（从 Built-in 迁移） |

### 更新日志

#### v1.8 (2026-08-19)

- 升级冲击波系统：升级时先播放 1 秒蓝色圆环扩散特效（从玩家位置向外扩散），对范围内敌人造成伤害并击退
- 冲击波时序调整：先触发冲击波（timeScale=1 保证物理/动画正常）→ 等 1 秒播放完毕 → 再暂停弹出升级面板
- 从 upgrades.lua.txt 移除冲击波伤害/范围两个升级选项（冲击波是升级附带效果，不属于主角个人能力）
- 击退实现改为 NavMeshAgent 位移（临时禁用 Agent → Lerp 移动 → NavMesh.SamplePosition 确保落点 → 重新启用），兼容无 Rigidbody 的敌人
- ShockwaveVFX 协程使用 Time.unscaledDeltaTime 防止 timeScale=0 时动画不推进
- UpgradeSystem 等待方式改为手动 unscaledDeltaTime 计时器 + Mathf.Min(0.1) 限制单帧上限
- 冲击波材质修复：Hidden/Internal-Colored → URP/Unlit（Hidden shader 不渲染）
- 冲击波 Mesh 修复：三角形绕序反转使法线朝上 + 顶点 Y 轴加 0.1 厚度避免视锥剔除
- ShockwavePrefab 重建：正确引用 ShockwaveMesh.asset + ShockwaveMat.mat
- ShockwaveEffectTests 测试更新：knockbackForce → knockbackDistance + knockbackDuration

#### v1.6 (2026-08-18)

- 瓦片地图系统：96×96=9216 片瓦片铺满 120×120 地面，5 种地形（草地/河流/石子路/河岸/路边），程序化纹理变体
- 设置界面：暂停菜单（ESC）+ 音量控制（主音量/音效 Slider + PlayerPrefs 持久化）
- 升级图标：7 张 AI 生成透明 PNG 图标（Resources/Icons/），UpgradeCard 用 Image + Resources.Load
- 升级面板鼠标显示：ShowUpgrades 时 Cursor.visible=true，SelectUpgrade 时恢复隐藏
- EnemyMovement：NavMeshAgent.isOnNavMesh 检查防止 SetDestination 报错
- Ground 碰撞体修复：瓦片地图替换后丢失地面，重建 120×120 MeshCollider
- 目录深度清理：删除 TJGenerators/旧 SFX/旧 Sprite/重复目录，temple prefab 迁移到 Quaternius/
- xLua 热更升级系统：UpgradeSystem 改为 Lua 驱动（upgrades.lua.txt），7 个升级选项数据驱动

#### v1.5 (2026-08-14)

- 火球改为自动定时发射（Physics.OverlapSphere 索敌，间隔 1s 可升级缩短到 0.1s）
- 近战斩击技能（MeleeCombat.cs 自动半圆范围攻击 + SlashEffect.prefab 粒子特效）
- 500 敌人数值平衡：maxCount=Min(500,100+lv*20)、受击 DPS 上限 40/s、Boss 间隔 30s
- 目录清理：删除旧角色模型/重复目录/旧 SFX/旧 Sprite 共 20+ 项

#### v1.4 (2026-08-13)

- 架构重构：PlayerController 拆分为 4 个组件（PlayerMovement/PlayerCombat/PlayerHealth/PlayerInteraction），C# Action 事件解耦
- ScriptableObject 敌人数据驱动配置（EnemyData.cs + 4 个 .asset 文件，Inspector 调参数不改代码）
- 20 个脚本添加命名空间分组（Game.Player/Enemy/Combat/Audio/UI/Systems）+ Game.asmdef 程序集定义
- 17 个 EditMode 测试用例（ObjectPool/HealthBar/ExpBar/EnemyData）+ 测试文档
- 材质命名统一（PascalCase 规范）
- 新增金属场景道具（火把/灯笼/宝箱）

#### v1.3 (2026-08-13)

- 新增敌人死亡溶解特效：手写 URP HLSL Shader（Dissolve.shader，ShaderLab+HLSL，AlphaToMask+噪声+边缘发光）
- 新增 DissolveEffect.cs 脚本：运行时替换材质+协程动画 DissolveAmount 0→1，逐帧推进溶解进度
- 三个敌人 Prefab（Zombie_Basic/EnemyTank/EnemyBoss）已添加 DissolveEffect 组件
- 主角模型替换为 Mixamo Y Bot（灰色人偶），新建 PlayerController.controller（Idle↔Walk↔Run）
- 设计文档新增 6.2 节 Shader/材质/物体关系说明
- 开发记录新增 C# 特性(Attribute)、协程(IEnumerator)知识点

#### v1.2 (2026-08-12)

- 清理 27 个未使用资产（~84MB）：旧角色动画 FBX、旧材质（Background/EnemyBoss/EnemyFast）、旧 Prefab（EnemyFast/SampleScene）、旧特效（FireBall.vfx）
- 火球特效升级：FireBall.vfx → FireBallEnhanced.vfx（自定义 FireParticle.png 贴图，80 粒子/秒，5 段颜色渐变，尺寸先膨胀后收缩）
- 修复火球 bug：忽略非敌人触发器（MagnetDetector）和 Player 层，VFX 对象池复用时 Reinit+Play 重启
- EnemyTank/Boss 模型替换：Tank→Zombie_Chubby，Boss→Zombie_Ribcage（Quaternius Zombie Apocalypse Kit 同系列）
- EnemyFast.prefab 废弃（bones[] 运行时丢失），Spawner 直接引用原始模型 prefab + 运行时设置差异属性
- 场景自然素材重新布局：138 个实例统一放在 NatureDecoration 父对象下（围墙外密集森林环带+远景稀疏树林+散布岩石+灌木藤蔓+地形装饰+围墙栅栏）
- Ground 材质改为 GroundGrass.mat（纯色草绿 URP Lit），320×320 大平面
- 新增 Kaykit/Kenney/Quaternius 低多边形素材包（CC0 资产）
- 旧 SFX 替换为命名清晰的音效文件（EnemyDeath.wav/FireballLaunch.wav/MonsterHit.wav）
- 所有敌人 SkinnedMeshRenderer 设为 updateWhenOffscreen=True
- 设计文档改名 DESIGN_DOC.md → 设计文档.md
- .gitignore 新增 mem-log/ 排除规则

#### v1.1 (2026-07-20)

- 新增 Boss 敌人系统：每10秒生成一个 Boss（独立 EnemyBoss.prefab，2.5x，HP20+难度，红色材质，必掉血瓶，掉3经验）
- 新增计时器 UI（右上角，mm:ss 格式）
- EnemyMovement：新增 isBoss 标记 + Boss 掉落3个经验块 + 血条高度按缩放调整 + 屏幕外隐藏血条
- EnemySpawner：新增 bossTimer + SpawnBoss() + FormatTime()
- 全部动画 clip 开启 loopTime
- 修复敌人不可见问题：SkinnedMeshRenderer 设置 updateWhenOffscreen=true + Animator cullingMode=AlwaysAnimate + applyRootMotion=false
- 修复 Boss 无动画问题：zombie_arm 控制器默认状态从 Death 改为 Idle
- 修复脚印透明渲染：FootprintMat 改为 URP/Unlit + _SURFACE_TRANSPARENCY + 实例材质
- 修复碰撞体过大问题：敌人 BoxCollider 缩小至 (0.6,1,0.6)，NavMeshAgent radius=0.3
- 修复火球超时不回收问题：pool 为空时兜底 Destroy
- 全部15个脚本补充函数级 XML 文档注释
- 资源目录重组完成：Audio/SFX、Sprites/UI、Models、Settings

#### v1.0 (2026-07-18)

- 新增闪避系统：空格冲刺，无敌帧0.2秒，冷却2秒
- 新增冷却图标 UI（灰色层+蓝色覆盖层，Radial360 扇形恢复）
- 新增闪避音效 + 冲刺图标 Sprite（蓝色粗箭头）
- 新增敌人血量系统：初始2血，火球不再一击必杀
- 新增难度递增：每10秒生成更快/上限更高/血量更高（移速不变）
- 新增敌人头顶血条（World Space Canvas，受伤显示，死亡销毁）
- 新增多种敌人类型：普通僵尸/快速僵尸(HP1,Speed5)/坦克僵尸(HP6,Speed1.5,1.5x体型)
- 新增自动吸取功能：Player 周围3米自动吸引 PickUp 和血瓶（满血不吸取血瓶）
- 升级系统新增第4选项：增加吸取范围（四选三随机展示）
- 脚印系统修复：生成脚印形状贴图，URP透明材质，MaterialPropertyBlock改为实例材质
- 资源目录重组：Audio/SFX、Sprites/UI、Models、Settings 规范化
- 清理冗余资产：旧模型包、未使用下载资产、metadata 等
- FootprintMat 透明渲染修复（URP/Unlit + _SURFACE_TRANSPARENCY 关键词）

#### v0.9 (2026-07-17)

- 新增敌人血量系统：僵尸初始2血（需2发火球），不再一击必杀
- 新增敌人头顶血条（World Space Canvas，受伤后显示，死亡时销毁）
- 新增 EnemyHealthBar.prefab：暗红底+红色填充，面向摄像机
- EnemyMovement：新增 maxHealth/TakeDamage/Die()，掉落逻辑移至 Die()
- FireBall：命中改为 TakeDamage(1f)，不再直接 Destroy
- 新增难度递增：每10秒敌人生成更快（-0.02s）、上限更高（+2）、血量更高（+1）
- 移速保持不变

#### v0.8 (2026-07-17)

- 新增自动吸取功能：Player 周围 3 米内自动吸引 PickUp 和血瓶
- 新增 MagnetDetector.cs：磁吸范围检测，满血时不吸取血瓶
- 新增 PickupItem.cs：被吸引时朝玩家飞行（MoveTowards）
- 升级系统新增第4种选项：增加吸取范围（+1 半径），四选三随机展示
- 新增磁铁图标 Sprite（AI 生成，蓝色磁铁，透明背景）
- PickUp/HealthPotion Prefab 添加 PickupItem 组件

#### v0.7 (2026-07-17)

- 新增对象池系统（ObjectPool）：火球和脚印预创建实例复用，减少 GC 压力
- 新增 ObjectPool.cs 通用对象池脚本（IPooledObject 接口）
- FireBall/Footprint 改为对象池模式，Instantiate+Destroy 替换为 Spawn+Despawn
- 新增 Layer 碰撞矩阵：Player(8)/FireBall(9)/PickUp(10)，替代 Physics.IgnoreCollision
- FireBall.cs 删除所有碰撞忽略代码和 FindObjectsOfType，由 Layer 矩阵处理
- PlayerController：缓存 HealthBar/ExpBar 引用，消除每帧 GetComponent
- PlayerController：用 isPaused 标志位替代 Time.timeScale > 0 判断输入
- UpgradeSystem：缓存 PlayerController/HealthBar 引用，调用 SetPaused()
- 僵尸爬行动画 loopTime 重新设为 true
- 全部脚本补充类属性注释

#### v0.6 (2026-07-17)

- 新增升级选择系统：升级时暂停游戏，三张卡片三选一（增加最大血量/移动速度/火球数量）
- 新增 UpgradeSystem.cs：暂停/恢复、随机打乱选项、应用升级效果
- 新增 UpgradeCard.cs：悬浮高亮（变色+放大）、点击回调
- 新增升级选择确认音效（AI 生成 SFX，共 10 种音效）
- 新增 3 个升级图标 Sprite（AI 生成，透明背景，扁平风格：心形/闪电/火焰）
- PlayerController：新增 fireballCount 字段，火球改为多发扇形发射（-15°~+15°）
- HealthBar：新增 IncreaseMaxHealth() 方法
- ExpBar：升级时调用 UpgradeSystem.ShowUpgrades()，while 循环支持跨多级升级
- FireBall：忽略火球间互相碰撞，忽略 PickUp/HealthPotion 碰撞
- Rotator：改用 Time.unscaledDeltaTime，暂停时继续旋转
- Footprint：改用 MaterialPropertyBlock，消除材质内存泄漏
- CameraController：Start() 加 null 检查
- EnemySpawner：浮点比较改为 Mathf.Abs < 0.001f
- 全部 10 个脚本补充类属性注释
- 导入 KayKit 和 Kenney 模型资产包
- Ground 材质从 wall.mat 改为 Background.mat
- 清理 7 个冗余资产

#### v0.5 (2026-07-16)

- 新增血瓶系统：敌人死亡 30% 概率掉落血瓶，拾取恢复 30 HP，血量满时不可拾取
- 新增 HealthPotion Prefab（KayKit 药水瓶 3D 模型，红色材质，Rotator 旋转）
- 新增拾取血瓶音效（AI 生成 SFX，共 9 种音效）
- HealthBar 新增 `Heal()` 和 `IsFull()` 方法
- EnemyMovement 新增 `healthPotionPrefab` 和 `dropChance` 字段，掉落物左右错开生成
- 火球发射改为朝鼠标指向方向（ScreenPointToRay + Physics.Raycast）
- 导入 KayKit 和 Kenney 模型资产包
- Ground 材质从 wall.mat 改为 Background.mat
- 清理 7 个冗余资产（旧 Player/Enemy 材质、未使用的下载模型）

#### v0.4 (2026-07-15)

- 新增血量系统（HealthBar）：100 HP，敌人接触持续扣血，归零则失败
- 新增经验/升级系统（ExpBar）：收集经验 +10 EXP，满 100 升级，每级 maxExp +20
- 新增敌人持续生成（EnemySpawner）：屏幕外刷新，最多 30 个，0.5s 间隔，NavMesh 采样
- 新增音效系统（AudioManager 单例）：8 种 AI 生成 SFX（火球/受伤/升级/游戏结束等）
- 新增敌人死亡掉落经验方块（EnemyMovement.OnDestroy）
- 导入 Quaternius 3D 敌人模型资源
- PlayerController：OnCollisionEnter → OnCollisionStay 持续扣血，PickUp 改为 Destroy + AddExp
- FireBall：新增敌人死亡和火球命中音效

#### v0.3 (2026-07-14)

- 渲染管线从 Built-in 迁移至 URP，所有材质已适配
- 新增火球攻击系统（鼠标左键发射，VFX Graph 粒子特效）
- 新增 FireBall.cs 脚本（飞行、碰撞消灭敌人、自毁）
- PlayerController：velocity 保留 Y 轴避免穿模，朝向阈值 0.1f 过滤微小残留
- Footprint：适配 URP 材质属性（`_BaseColor`），lifetime 调整为 2s
- 脚印旋转修正（`* Quaternion.Euler(90, 0, 0)`）
- AI 生成角色新增 Motion 动画

#### v0.2 (2026-07-13)

- AI 生成角色替换球体（Meshy AI + TJGenerators）
- PlayerController 重构：AddForce → rb.velocity 恒定速度移动
- 新增脚印系统（左右交替，渐隐消失）
- 新增游戏结束面板（重新开始/退出）
- 刚体 freezeRotation 防止角色翻倒和转圈

#### v0.1 (2026-07-10)

- 初始项目：基于 Roll-a-Ball 教程
- NavMesh 敌人追逐 + 动态障碍物
- TMP 中文支持

### 如何运行

1. 用团结 Hub（或 Unity 2022.3 LTS）打开 `Test/` 目录
2. 打开 `Assets/Scenes/迷你游戏.scene`
3. 点击 Play 运行

---

## 子项目：Wuxia

俯视角 3D 武侠动作游戏学习项目。以可扩展攻击系统为核心，逐步实现移动、战斗、VFX 特效、敌人 AI、打击反馈、场景打磨、性能优化、镜头展示和 Lua 热更。

### 环境要求

- **引擎**：团结引擎 2022.3.62t11（Tuanjie 1.9.3）
- **渲染管线**：Universal Render Pipeline (URP)
- **输入系统**：Input System Package (New)

### 项目结构

```
Wuxia/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/           # 框架层：事件系统、接口定义（IDamageCalculator, ISkillLogic）
│   │   ├── Player/         # 角色控制、输入响应
│   │   ├── Combat/         # 攻击系统：配置、管理器
│   │   ├── VFX/            # VFX 触发与生命周期管理
│   │   ├── Camera/         # 镜头控制
│   │   ├── AI/             # 敌人 AI（后期）
│   │   └── Lua/            # xLua 接入与桥接层（中后期）
│   ├── Data/Attacks/       # 攻击配置 ScriptableObjects
│   ├── Prefabs/
│   │   ├── Player/         # 玩家预制体（Ranger）
│   │   ├── VFX/            # VFX 预制体
│   │   └── Environment/    # 环境预制体
│   ├── Model/              # 3D 模型素材（角色 FBX、动画、建筑、环境）
│   ├── Scenes/
│   │   └── SampleScene.scene  # 主场景（地面 + Player）
│   ├── Settings/           # URP 配置、Input Actions、AnimatorController
│   └── Art/                # 美术资源
├── 计划/                    # 分阶段开发计划（P0-P10）
├── 设计文档.md              # 项目设计文档
└── CODELY.md               # Codely AI 结构化记忆
```

### 开发阶段

| 阶段 | 内容 | 状态 |
|------|------|------|
| P0 · 基建 | URP 配置、Input Actions、目录结构、核心框架脚本 | ✅ 已完成 |
| P1 · 移动 | Rigidbody 移动 + 走跑跳 + 俯视角摄像机 | 🔨 进行中 |
| P2 · 攻击骨架 | AttackManager + AttackConfig + VFX 触发 | ⬜ 待开始 |
| P3 · 自研 VFX | VFX Graph 刀光/拳风 | ⬜ 待开始 |
| P4 · 音效 | AI 生成 SFX + BGM | ⬜ 待开始 |
| P5 · 敌人系统 | 简单 AI + 受击反馈 | ⬜ 待开始 |
| P6 · 打击反馈 | 顿帧、屏幕震动、闪白、残影 | ⬜ 待开始 |
| P7 · 场景打磨 | 竹林场景 + 灯光 + 后期处理 | ⬜ 待开始 |
| P8 · VFX 极限 | 万剑归宗 + Profiler 性能优化 | ⬜ 待开始 |
| P9 · 镜头展示 | Cinemachine 运镜 + 录制 | ⬜ 待开始 |
| P10 · Lua 接入 | xLua 框架 + 逻辑迁移 | ⬜ 待开始 |

### 当前进展（P0 · 基建）

- URP 渲染管线配置（URP-HighFidelity）
- Input System 包安装（v1.14.4-t3），Active Input Handling 切换为 New
- Input Actions 配置：3 个 Action Map（Movement / Combat / Camera），7 个 Action
- 完整目录结构创建（14 个文件夹）
- 核心框架脚本：IGameEvent、GameEventBus、IDamageCalculator、ISkillLogic

### 当前进展（P1 · 移动）

- KayKit Ranger 角色导入，FBX Rig 切换为 Humanoid
- AnimatorController：1D Blend Tree（Idle/Walk/Run）+ Jump 状态
- PlayerController 脚本：Rigidbody 移动 + 跳跃 + Animator 参数更新
- 角色放入场景，Rigidbody + CapsuleCollider 配置
- 待完成：InputReader 脚本、CameraFollow 脚本（用户手动编写）

### 如何运行

1. 用团结 Hub（或 Unity 2022.3 LTS）打开 `Wuxia/` 目录
2. 打开 `Assets/Scenes/SampleScene.scene`
3. 点击 Play 运行

---

## 许可证

本仓库仅供学习用途。
