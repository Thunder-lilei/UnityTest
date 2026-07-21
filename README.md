# UnityTest

Unity 开发学习仓库，存储学习过程中的所有产出。仓库本身是一个多项目实验体，未来将包含多个独立的 Unity 子项目，每个子项目对应不同的学习主题或玩法实验。

## 仓库结构

```
UnityTest/
├── Test/                      # 子项目1：基于 Roll-a-Ball 的 3D 生存游戏
└── README.md                  # 仓库总览
```

| 子项目 | 状态 | 学习主题 |
|--------|------|----------|
| [Test](./Test) | 进行中 | Roll-a-Ball 教程扩展 → 3D 生存/战斗/升级游戏 |

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
│   ├── Audio/SFX/             # AI 生成音效（11 种 SFX）
│   ├── Sprites/UI/            # 升级图标+脚印贴图+冲刺图标
│   ├── Models/                # 3D 模型（角色/僵尸/血瓶/Boss）
│   ├── Prefabs/               # 预制体（PickUp/DynamicBox/Quad/FireBall/HealthPotion/EnemyHealthBar/EnemyFast/EnemyTank/EnemyBoss）
│   ├── Scenes/
│   │   └── 迷你游戏.scene       # 主关卡（含 NavMesh 烘焙数据）
│   ├── Scripts/                # C# 脚本
│   │   ├── PlayerController.cs # 玩家控制、动画、脚印、火球、经验/血量
│   │   ├── CameraController.cs # 摄像机跟随
│   │   ├── EnemyMovement.cs    # 敌人 NavMesh 追逐 + 死亡掉落
│   │   ├── EnemySpawner.cs     # 敌人持续生成（屏幕外刷新）
│   │   ├── FireBall.cs         # 火球飞行与碰撞
│   │   ├── Footprint.cs        # 脚印渐隐消失
│   │   ├── AudioManager.cs     # 音效管理器（单例）
│   │   ├── HealthBar.cs        # 血量条 UI
│   │   ├── ExpBar.cs           # 经验条 UI + 升级系统
│   │   ├── UpgradeSystem.cs    # 升级选择系统（暂停/四选三/应用效果）
│   │   ├── UpgradeCard.cs      # 升级卡片 UI（悬浮高亮/点击回调）
│   │   ├── MagnetDetector.cs   # 磁吸范围检测
│   │   ├── PickupItem.cs       # 拾取物被吸引飞行
│   │   ├── ObjectPool.cs       # 通用对象池（Spawn/Despawn 复用）
│   │   └── Rotator.cs          # 收集物旋转动画
│   ├── Settings/              # URP 渲染配置
│   ├── Effects/                # VFX 特效（FireBall.vfx）
├── Packages/
└── ProjectSettings/
```

### 游戏玩法

- **WASD / 方向键**：控制角色移动（恒定速度，非物理力驱动）
- **鼠标左键**：朝鼠标指向方向发射火球攻击敌人
- **空格**：闪避冲刺，含 0.2 秒无敌帧，2 秒冷却
- 角色自动朝向移动方向，行走时留下渐隐脚印
- 收集经验方块升级，火球消灭敌人也会掉落经验
- 敌人被消灭后有概率掉落血瓶，拾取可恢复血量
- 敌人持续从屏幕外刷新，碰到玩家扣血，血量归零则失败
- 胜利或失败后弹出面板，可选择**重新开始**或**退出游戏**

### 扩展功能（相对原版教程）

| 功能 | 说明 |
|------|------|
| AI 生成角色 | 通过 Meshy AI + TJGenerators 插件生成 Humanoid 角色（含 Idle/Walk/Run/Motion 动画） |
| Animator 状态机 | Speed 参数驱动 Idle ↔ Walk ↔ Run 过渡，Action 触发特殊动作 |
| 恒定速度移动 | 使用 rb.velocity 替代 AddForce，避免加速感；保留 Y 轴速度避免穿模 |
| 朝向移动方向 | Quaternion.Slerp 平滑转向 |
| 火球攻击 | 鼠标左键朝鼠标指向方向发射火球，VFX Graph 粒子特效，命中敌人即消灭 |
| 闪避系统 | 空格冲刺，0.2 秒无敌帧，2 秒冷却，UI 冷却图标扇形恢复 |
| 血量系统 | HealthBar：100 HP，敌人接触持续扣血，归零则失败 |
| 血瓶掉落 | 敌人死亡 30% 概率掉落血瓶，拾取恢复 30 HP，血量满时不可拾取 |
| 经验/升级 | ExpBar：收集经验方块 +10 EXP，满 100 升级，每级 maxExp +20 |
| 升级选择系统 | 升级时暂停游戏，四选三随机：增加最大血量/移动速度/火球数量/吸取范围，含悬浮高亮和图标 |
| 敌人持续生成 | EnemySpawner：屏幕外刷新，最多 30 个，0.5s 间隔，NavMesh 采样 |
| 难度递增 | 每10秒：生成更快（-0.02s）、上限更高（+2）、血量更高（+1） |
| 敌人血量 | 僵尸初始2血，火球不再一击必杀，头顶显示血条 |
| 多种敌人类型 | 普通僵尸 / 快速僵尸(HP1,Speed5) / 坦克僵尸(HP6,Speed1.5,1.5x体型) |
| 敌人死亡掉落 | 敌人被火球消灭后在死亡位置生成经验方块 |
| 音效系统 | AudioManager 单例：11 种音效（火球发射/命中/敌人死亡/受伤/死亡/拾取经验/拾取血瓶/升级/升级确认/闪避/游戏结束） |
| 脚印系统 | 移动时左右交替生成脚印，2 秒渐隐消失，程序化贴图+URP透明材质 |
| 敌人追逐 | 使用 NavMesh 实现敌人自动寻路追踪玩家 |
| 对象池 | 火球和脚印预创建实例复用，减少 GC 压力 |
| Layer 碰撞矩阵 | Player(8)/FireBall(9)/PickUp(10)，替代逐对 Physics.IgnoreCollision |
| Boss 敌人 | 每10秒生成一个 Boss（2.5x体型，HP20+，必掉血瓶，掉3个经验） |
| 计时器 | 右上角显示游戏存活时间（mm:ss） |
| 游戏结束面板 | 胜利/失败时弹出 UI 面板，暂停游戏（Time.timeScale = 0） |
| 重新开始 | 通过 SceneManager 重新加载当前场景 |
| 退出游戏 | Application.Quit() |
| URP 渲染管线 | 从 Built-in 迁移至 Universal Render Pipeline |
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

## 许可证

本仓库仅供学习用途。
