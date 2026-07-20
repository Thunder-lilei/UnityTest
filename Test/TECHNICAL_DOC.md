# 技术文档 — 迷你游戏 (Roll-a-Ball)

> **项目名称**: Test
> **产品名称**: 迷你游戏
> **引擎版本**: Unity 2022.3.62t11 (Tuanjie / Unity 中国版 1.9.3)
> **渲染管线**: Universal Render Pipeline (URP)
> **目标平台**: Windows Standalone (x86_64)
> **文档日期**: 2026-07-10 (更新: 2026-07-18, v1.0)

---

## 1. 项目配置

### 1.1 引擎与平台

| 配置项 | 值 |
|---|---|
| Unity 版本 | 2022.3.62t11 (Tuanjie) |
| Bundle Identifier | `com.DefaultCompany.Test` |
| 目标平台 | Standalone Windows |
| 分辨率 | 1920 × 1080 |
| 全屏模式 | 全屏 |
| 色彩空间 | Linear |
| 渲染路径 | URP Forward |
| 后台运行 | 开启 |
| Graphics Jobs | 开启 |

### 1.2 标签 (Tags)

| 标签名 | 用途 |
|---|---|
| `PickUp` | 可收集物品 |
| `Enemy` | 敌人实体 |
| `HealthPotion` | 血瓶拾取物 |

### 1.3 层级 (Layers)

仅使用 Unity 内置层级 (Default, TransparentFX, Ignore Raycast, Water, UI)，未添加自定义层级。

### 1.4 构建配置

| 场景 | 路径 | 是否参与构建 |
|---|---|---|
| 迷你游戏 | `Assets/Scenes/迷你游戏.scene` | ✅ |
| SampleScene | `Assets/Scenes/SampleScene.scene` | ❌ |

### 1.5 依赖包 (manifest.json)

| 包名 | 版本 | 用途 |
|---|---|---|
| `cn.tuanjie.codely.bridge` | 1.0.68 | Codely AI 桥接 |
| `com.unity.ai.navigation` | 1.1.5 | NavMesh AI 寻路 |
| `com.unity.collab-proxy` | 2.12.4 | 版本控制 |
| `com.unity.feature.development` | 1.0.1 | 开发工具集 |
| `com.unity.render-pipelines.universal` | 14.2.0-t1 | URP 渲染管线 |
| `com.unity.textmeshpro` | 3.0.9 | UI 文本渲染 |
| `com.unity.visualeffectgraph` | 14.2.0-t1 | VFX Graph 粒子特效 |
| `com.unity.timeline` | 1.7.7 | 时间线 |
| `com.unity.ugui` | 1.0.0 | uGUI 框架 |
| `com.unity.visualscripting` | 1.9.4 | 可视化脚本 |

---

## 2. 脚本架构

项目共包含 **15 个 C# 脚本**，均位于 `Assets/Scripts/` 目录下，无自定义命名空间。

```
Assets/Scripts/
├── CameraController.cs    — 摄像机跟随
├── PlayerController.cs    — 玩家控制、动画驱动、脚印、火球攻击、经验/血量、游戏状态
├── EnemyMovement.cs       — 敌人 AI 寻路 + 死亡掉落经验方块和血瓶
├── EnemySpawner.cs        — 敌人持续生成（屏幕外刷新，NavMesh 采样）
├── FireBall.cs            — 火球飞行与碰撞（对象池模式）
├── Footprint.cs           — 脚印渐隐消失（MaterialPropertyBlock + 对象池模式）
├── ObjectPool.cs          — 通用对象池（Spawn/Despawn 复用 + IPooledObject 接口）
├── MagnetDetector.cs      — 磁吸范围检测（满血时不吸取血瓶）
├── PickupItem.cs          — 拾取物被吸引飞行（MoveTowards）
├── AudioManager.cs        — 音效管理器（单例，10 种音效）
├── HealthBar.cs           — 血量条 UI（100 HP，扣血/回血/增加上限，死亡判定）
├── ExpBar.cs              — 经验条 UI + 升级系统（while 循环跨多级升级）
├── UpgradeSystem.cs       — 升级选择系统（暂停/四选三/应用效果）
├── UpgradeCard.cs         — 升级卡片 UI（悬浮高亮/点击回调）
└── Rotator.cs             — 收集品/血瓶旋转动画（unscaledDeltaTime）
```

### 2.1 CameraController.cs

**职责**: 第三人称跟随摄像机。

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `player` | `GameObject` | public | 跟随目标 |
| `offset` | `Vector3` | private | 摄像机与玩家的初始偏移 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | 计算 `offset = transform.position - player.transform.position` |
| `LateUpdate()` | 每帧 | 设置 `transform.position = player.transform.position + offset` |

**设计要点**: 使用 `LateUpdate` 而非 `Update`，确保摄像机在玩家移动完成后跟随，避免抖动。

### 2.2 PlayerController.cs

**职责**: 玩家输入、恒定速度移动、动画驱动、脚印生成、火球攻击、经验收集、血量管理、胜负判定。

**依赖**: `UnityEngine`, `TMPro` (TextMeshPro), `UnityEngine.SceneManagement`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `speed` | `float` | public | 移动速度，场景中设为 `10` |
| `gameOverPanel` | `GameObject` | public | 游戏结束面板 |
| `resultText` | `TextMeshProUGUI` | public | 胜负结果文本 |
| `footprintPrefab` | `GameObject` | public | 脚印 Prefab 引用 |
| `footprintSpacing` | `float` | public | 脚印间距，默认 `1` |
| `foot` | `GameObject` | public | 脚印父物体（Foot 子对象） |
| `skill` | `GameObject` | public | 技能特效父物体 |
| `fireballPrefab` | `GameObject` | public | 火球 Prefab 引用 |
| `mainCamera` | `Camera` | public | 主摄像机引用 |
| `rb` | `Rigidbody` | private | 物理刚体引用 |
| `animator` | `Animator` | private | 动画控制器引用 |
| `lastFootprintPos` | `Vector3` | private | 上一个脚印位置 |
| `isLeftFoot` | `bool` | private | 左右脚交替标记 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | 获取 `Rigidbody` 和 `Animator`，隐藏面板，记录初始位置，获取主摄像机 |
| `Update()` | 每帧 | 检测鼠标左键 → `FireFireball()` |
| `FixedUpdate()` | 物理帧 | 读取输入 → `rb.velocity` 恒定速度移动（保留 Y 轴） → `animator.SetFloat("Speed")` → 朝向移动方向 → 生成脚印 |
| `OnTriggerEnter(Collider)` | 碰撞回调 | 碰到 `PickUp` 标签 → `Destroy` + `ExpBar.AddExp(10f)` + 音效；碰到 `HealthPotion` 标签 → 若血量未满则 `Destroy` + `HealthBar.Heal(30f)` + 音效 |
| `OnCollisionStay(Collision)` | 碰撞回调 | 碰到 `Enemy` 标签 → `HealthBar.TakeDamage(20f * Time.deltaTime)` → 播放受伤音效 → 血量归零则销毁玩家 + `ShowGameOver()` |
| `ShowGameOver()` | 自定义 | 显示游戏结束面板，播放游戏结束音效，`Time.timeScale = 0` 暂停 |
| `RestartGame()` | public | `SceneManager.LoadScene()` 重新开始 |
| `QuitGame()` | public | `Application.Quit()` 退出游戏 |
| `FireFireball()` | 自定义 | 鼠标位置射线投射到地面 → 计算方向 → 在角色前方生成火球实例 → 播放发射音效 |

**核心逻辑流**:

```
Input → FixedUpdate → rb.velocity (恒定速度，保留Y轴)
                       ├── animator.SetFloat("Speed") → 动画状态机驱动
                       ├── Quaternion.Slerp → 朝向移动方向 (magnitude > 0.1f)
                       └── 脚印生成 (距离间隔 + 左右交替 + 旋转修正)
Update → 鼠标左键 → FireFireball() → 生成火球 (VFX + 碰撞) + 音效
                                        ↓
                              OnCollisionStay(Enemy) → HealthBar.TakeDamage → 音效
                                  └── IsDead → Destroy(Player) → ShowGameOver (失败)
                              OnTriggerEnter(PickUp) → Destroy + ExpBar.AddExp(10f) + 音效
```

### 2.3 EnemyMovement.cs

**职责**: 敌人 AI 追踪行为 + 死亡掉落经验方块。

**依赖**: `UnityEngine`, `UnityEngine.AI`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `player` | `Transform` | public | 追踪目标 (玩家) |
| `pickUpPrefab` | `GameObject` | public | 死亡掉落的经验方块 Prefab |
| `healthPotionPrefab` | `GameObject` | public | 死亡掉落的血瓶 Prefab |
| `dropChance` | `float` | public | 血瓶掉落概率，默认 `0.3` |
| `navMeshAgent` | `NavMeshAgent` | private | NavMesh 代理 |
| `isQuitting` | `bool` | private | 是否正在退出应用（防止退出时误触发掉落） |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | 获取 `NavMeshAgent` 组件 |
| `Update()` | 每帧 | 若 `player` 不为空，调用 `navMeshAgent.SetDestination(player.position)` |
| `OnApplicationQuit()` | 应用退出 | 设置 `isQuitting = true` |
| `OnDestroy()` | 销毁回调 | 若非退出应用 → 在死亡位置生成经验方块（左侧偏移0.5）+ 概率掉落血瓶（右侧偏移0.5，Y轴+0.5） |

**NavMeshAgent 场景参数**:

| 参数 | 值 |
|---|---|
| Speed | 2.5 |
| Acceleration | 8 |
| Angular Speed | 120 |
| Stopping Distance | 0 |
| Height | 1 |
| Base Offset | 0.5 |
| 避障质量 | High Quality (4) |

### 2.4 FireBall.cs

**职责**: 火球飞行、碰撞检测与敌人扣血。

**依赖**: `UnityEngine`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `speed` | `float` | public | 飞行速度，默认 `20` |
| `lifetime` | `float` | public | 存活时间，默认 `3` 秒 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `OnSpawn()` | 对象池激活 | 重置计时器 |
| `Update()` | 每帧 | 前移 + 计时器超时回收 |
| `OnTriggerEnter(Collider)` | 碰撞回调 | 碰到 `Enemy` → `enemy.TakeDamage(1f)`（不再直接 Destroy）+ 播放命中音效 + 回收火球 |

**设计要点**: 火球命中敌人后调用 `TakeDamage(1f)` 扣血，由敌人自行判断是否死亡。Layer 矩阵屏蔽 Player/PickUp/其他火球碰撞。

### 2.5 EnemySpawner.cs

**职责**: 敌人持续生成系统，从屏幕外刷新敌人。

**依赖**: `UnityEngine`, `UnityEngine.AI`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `enemyPrefab` | `GameObject` | public | 敌人 Prefab |
| `player` | `Transform` | public | 玩家 Transform（传递给生成的敌人） |
| `maxCount` | `int` | public | 最大敌人数，默认 `30` |
| `spawnInterval` | `float` | public | 生成间隔，默认 `0.5` 秒 |
| `spawnMargin` | `float` | public | 屏幕外边距，默认 `2` |
| `enemyGo` | `GameObject` | public | 敌人父物体 |
| `mainCamera` | `Camera` | private | 主摄像机 |
| `timer` | `float` | private | 计时器 |
| `enemies` | `List<GameObject>` | private | 已生成敌人列表 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | 获取主摄像机，初始化计时器 |
| `Update()` | 每帧 | 累加计时器，达到间隔 → `SpawnEnemy()` |
| `SpawnEnemy()` | 自定义 | 清理空引用，检查上限 → `GetSpawnPositionOutsideViewport()` → 实例化敌人 → 设置追踪目标 |
| `GetSpawnPositionOutsideViewport()` | 自定义 | 计算摄像机视口四角的世界坐标 → 在视口外边缘随机选点 → `NavMesh.SamplePosition` 采样 |

**设计要点**: 使用 `Camera.ViewportPointToRay` 将屏幕四角投射到地面，计算可视范围外边缘。通过 `NavMesh.SamplePosition` 确保敌人生成在 NavMesh 上。

### 2.6 AudioManager.cs

**职责**: 音效管理器（单例模式），统一管理所有游戏音效。

**依赖**: `UnityEngine`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `Instance` | `AudioManager` | public static | 单例实例 |
| `fireballLaunch` | `AudioSource` | public | 火球发射音效 |
| `fireballHit` | `AudioSource` | public | 火球命中音效 |
| `enemyDeath` | `AudioSource` | public | 敌人死亡音效 |
| `playerHurt` | `AudioSource` | public | 玩家受伤音效 |
| `playerDeath` | `AudioSource` | public | 玩家死亡音效 |
| `pickupExp` | `AudioSource` | public | 拾取经验音效 |
| `levelUp` | `AudioSource` | public | 升级音效 |
| `gameOver` | `AudioSource` | public | 游戏结束音效 |
| `healthPotionPickup` | `AudioSource` | public | 拾取血瓶音效 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Awake()` | 初始化 | 单例模式：若 Instance 为空则赋值，否则销毁自身 |
| `Play*()` | public | 9 个播放方法，各自检查 AudioSource 不为空后播放 |

**设计要点**: 单例模式 (`Instance`)，全局可通过 `AudioManager.Instance?.PlayXxx()` 调用。9 种音效覆盖所有游戏事件。

### 2.7 HealthBar.cs

**职责**: 血量条 UI 管理。

**依赖**: `UnityEngine`, `UnityEngine.UI`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `fillRect` | `RectTransform` | public | 血量条填充矩形 |
| `maxHealth` | `float` | public | 最大血量，默认 `100` |
| `currentHealth` | `float` | private | 当前血量 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | `currentHealth = maxHealth`，`UpdateFill()` |
| `TakeDamage(float)` | public | 扣血 `currentHealth = Max(0, currentHealth - damage)`，`UpdateFill()` |
| `Heal(float)` | public | 回血 `currentHealth = Min(maxHealth, currentHealth + amount)`，`UpdateFill()` |
| `IsDead()` | public | 返回 `currentHealth <= 0` |
| `IsFull()` | public | 返回 `currentHealth >= maxHealth` |
| `UpdateFill()` | 自定义 | 设置 `fillRect.anchorMax.x = currentHealth / maxHealth` |

### 2.8 ExpBar.cs

**职责**: 经验条 UI + 升级系统。

**依赖**: `UnityEngine`, `TMPro`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `fillRect` | `RectTransform` | public | 经验条填充矩形 |
| `levelText` | `TextMeshProUGUI` | public | 等级文本 |
| `maxExp` | `float` | public | 升级所需经验，初始 `100` |
| `currentExp` | `float` | private | 当前经验 |
| `level` | `int` | public | 当前等级，初始 `1` |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | `currentExp = 0`，`UpdateFill()`，`UpdateLevelText()` |
| `AddExp(float)` | public | 增加经验，若溢出则升级：`level++`，`maxExp += 20`，播放升级音效 |
| `UpdateFill()` | 自定义 | 设置 `fillRect.anchorMax.x = currentExp / maxExp` |
| `UpdateLevelText()` | 自定义 | 设置文本为 `"Lv. " + level` |

### 2.9 Footprint.cs

**职责**: 脚印渐隐消失效果（URP 适配版）。

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `lifetime` | `float` | public | 脚印存活时间，默认 `2` 秒 |
| `mat` | `Material` | private | 材质引用 |
| `timer` | `float` | private | 计时器 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | 获取 Renderer 材质，设置 `_BaseColor` alpha = 1 |
| `Update()` | 每帧 | 累加计时器，按比例递减材质 `_BaseColor` alpha；超时后 `Destroy(gameObject)` |

**URP 适配**: 材质属性从 Built-in 的 `_Color` 改为 URP 的 `_BaseColor`。

### 2.10 Rotator.cs

**职责**: 装饰性旋转动画，应用于收集品和血瓶。

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Update()` | 每帧 | `transform.Rotate(new Vector3(15, 30, 45) * Time.unscaledDeltaTime)` |

**设计要点**: 旋转速度为 `(15, 30, 45)` 度/秒，三轴非均匀旋转产生视觉趣味。使用 `Time.unscaledDeltaTime` 确保升级暂停时仍继续旋转。

### 2.11 UpgradeSystem.cs

**职责**: 升级选择系统核心逻辑：暂停游戏、随机选项、应用升级效果。

**依赖**: `UnityEngine`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `upgradePanel` | `GameObject` | public | 升级面板 UI |
| `cards` | `UpgradeCard[]` | public | 三张卡片引用 |
| `icons` | `Sprite[]` | public | 三种升级图标（按 UpgradeType 顺序） |

| 方法 | 逻辑 |
|---|---|
| `ShowUpgrades()` | 随机打乱三种 UpgradeType → 填充卡片内容/图标/回调 → `Time.timeScale = 0` → 显示面板 |
| `SelectUpgrade(type)` | 根据 type 应用效果（MaxHealth+20/Speed+2/FireballCount+1）→ 播放确认音效 → `Time.timeScale = 1` → 隐藏面板 |

### 2.12 UpgradeCard.cs

**职责**: 单张升级卡片 UI，处理悬浮高亮和点击回调。

**依赖**: `UnityEngine`, `UnityEngine.UI`, `TMPro`, `UnityEngine.EventSystems`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `titleText` | `TextMeshProUGUI` | public | 卡片标题文本 |
| `descText` | `TextMeshProUGUI` | public | 卡片描述文本 |
| `icon` | `Image` | public | 卡片图标 |
| `button` | `Button` | public | 卡片按钮 |
| `normalColor` | `Color` | public | 默认背景色 |
| `hoverColor` | `Color` | public | 悬浮背景色 |

| 方法 | 逻辑 |
|---|---|
| `Start()` | 获取 `Image` 组件，设置默认背景色 |
| `SetData(type, title, desc)` | 设置升级类型、标题、描述 |
| `SetupCallback(system)` | 补获取 bgImage → 注册 button.onClick → 调用 `system.SelectUpgrade` |
| `OnPointerEnter()` | 变色为 hoverColor + 放大 1.05 倍 |
| `OnPointerExit()` | 恢复 normalColor + 恢复 1.0 倍 |

---

## 3. 脚本关系图

```
┌─────────────────────────────────────────────────────┐
│                   PlayerController                   │
│  ┌─────────────────────────────────────────────┐    │
│  │  FixedUpdate: Input → rb.velocity (恒定速度，保留Y轴) │    │
│  │  Animator.SetFloat("Speed") → 状态机驱动     │    │
│  │  Quaternion.Slerp → 朝向移动方向             │    │
│  │  Footprint 生成 (间距 + 左右交替 + 旋转修正)  │    │
│  │  Update: 鼠标左键 → FireFireball()          │    │
│  │  OnTriggerEnter(PickUp) → count++ → UI       │    │
│  │  OnCollisionEnter(Enemy) → ShowGameOver      │    │
│  │  count >= 4 → Victory, Destroy Enemy         │    │
│  └─────────────────────────────────────────────┘    │
│         │           │            │                   │
│    引用 ↓      引用 ↓       引用 ↓                   │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────┐    │
│  │ TMP UGUI │ │ Animator │ │  Enemy (Tag)     │    │
│  │ CountText│ │ Speed    │ │  → EnemyMovement │    │
│  │ ResultTxt│ │ Action   │ │    → NavMeshAgent│    │
│  │ GamePanel│ │          │ │    → SetDestination│   │
│  └──────────┘ └──────────┘ └──────────────────┘    │
│                                                     │
│  FireBall.cs ← fireballPrefab (FireBall + VFX)      │
│    → 飞行 (20 speed) → OnTriggerEnter(Enemy) → 消灭  │
│  Footprint.cs ← footprintPrefab (Quad)              │
│    → 渐隐消失 (2s, URP _BaseColor)                   │
│  CameraController                                   │
│    → 引用 Player (跟随偏移)                          │
│  Rotator                                            │
│    → 挂载于 PickUp Prefab                            │
└─────────────────────────────────────────────────────┘
```

---

## 4. 场景结构

### 4.1 主场景 — 迷你游戏.scene

```
迷你游戏.scene
├── Main Camera          [Camera, AudioListener, CameraController]
├── Directional Light     [Light (Directional)]
├── Ground                [MeshFilter(Plane), MeshRenderer, MeshCollider, NavMeshSurface]
│   ├── Cube             [静态障碍物]
│   ├── Cube (1)         [倾斜坡道]
│   ├── Cube (2)         [倾斜坡道]
│   ├── Cube (3)         [静态障碍物 (旋转90°)]
│   ├── Cube (4)         [大型斜面障碍物]
│   ├── DynamicBox       [4 个 DynamicBox Prefab 堆叠]
│   ├── DynamicBox (1)   [4 个 DynamicBox Prefab 堆叠]
│   └── DynamicBox (2)   [4 个 DynamicBox Prefab 堆叠]
├── Player                [CapsuleCollider, Rigidbody(freezeRotation=true), Animator, PlayerController, HealthBar, ExpBar]
│   ├── Foot              [脚印父物体]
│   ├── Skill             [技能特效父物体]
│   └── GeneratedModel    [MeshFilter, MeshRenderer, CapsuleCollider]
├── wall                  [父对象]
│   ├── West Wall        [BoxCollider, Cube Mesh]
│   ├── East Wall        [BoxCollider, Cube Mesh]
│   ├── North Wall       [BoxCollider, Cube Mesh (旋转90°)]
│   └── South Wall       [BoxCollider, Cube Mesh (旋转90°)]
├── PickUp Parent         [父对象]
│   └── PickUp           [Prefab 实例（动态生成）]
├── Canvas               [Canvas, CanvasScaler, GraphicRaycaster]
│   ├── HealthBar        [RectTransform fillRect]
│   ├── ExpBar           [RectTransform fillRect, TextMeshProUGUI levelText]
│   ├── GameOverPanel    [GameObject, ResultText, RestartButton, QuitButton]
│   └── AudioManager     [AudioSource x8]
├── EventSystem           [EventSystem, StandaloneInputModule]
├── EnemySpawner          [EnemySpawner]
└── Enemy                 [父对象]
    └── EnemyBody        [NavMeshAgent, EnemyMovement, BoxCollider]
```

### 4.2 场地布局 (俯视图)

```
           North Wall (z=10)
    ┌──────────────────────────┐
    │                          │
    │   ★PickUp(2)   ★PickUp    │
    │                  ★PickUp(3)│
    │    [障碍]               [E]│  ← Enemy 起始位置
    │         [坡道]            │     (-7.9, 6.96)
    │    [障碍]    [斜面]       │
    │                          │
    │  ★PickUp(1)   ●Player    │
    │            (0,0)         │
W   │                          │   E
wall│     [DynamicBox堆]        │  wall
    │                          │
    │   [DynamicBox堆]   [DynamicBox堆]│
    │                          │
    └──────────────────────────┘
           South Wall (z=-10)
```

### 4.3 摄像机配置

| 参数 | 值 |
|---|---|
| 位置 | (0, 10, -10) |
| 旋转 | (45, 0, 0) — 俯视斜角 |
| FOV | 60° |
| 清除标志 | Skybox |
| 投影 | 透视 |

---

## 5. Prefab 详细

### 5.1 Player.prefab (AI 生成角色)

| 组件 | 配置 |
|---|---|
| Transform | 根物体 |
| Animator | Avatar: ae749bf5fe5aaf00, Controller: ae749bf5fe5aaf00_Controller, Apply Root Motion: False |
| CapsuleCollider | Radius=0.5, Height=2 |
| Rigidbody | useGravity=true, freezeRotation=true |
| PlayerController (脚本) | speed=10, footprintPrefab, footprintSpacing=1, fireballPrefab |

**子物体 GeneratedModel**:
- MeshFilter (角色网格)
- MeshRenderer (材质 texture_0, 2048×2048)

**Animator Controller 状态机**:
- 参数: Speed (Float), Action (Trigger)
- 状态: Idle / Walk / Run / Action
- 过渡: Idle↔Walk (Speed > 0.1), Walk↔Run (Speed > 0.5), Action→Walk (无条件)

### 5.2 FireBall.prefab (火球)

| 组件 | 配置 |
|---|---|
| Transform | 生成位置：角色前方 + Vector3.up |
| VFX Component | FireBall.vfx (VFX Graph 粒子特效) |
| SphereCollider | isTrigger = true |
| FireBall (脚本) | speed=20, lifetime=3s |

**设计要点**: 火球使用 VFX Graph 实现视觉效果，SphereCollider (Trigger) 检测碰撞。通过 `Physics.IgnoreCollision` 忽略发射者。

### 5.3 Quad.prefab (脚印)

| 组件 | 配置 |
|---|---|
| Transform | Scale (0.3, 0.3, 0.3) |
| MeshFilter | Quad (内置四边形) |
| MeshRenderer | FootprintMat (半透明, RenderQueue=3000) |
| Footprint (脚本) | lifetime=2s, URP _BaseColor 渐隐消失 |

### 5.4 PickUp.prefab

| 组件 | 配置 |
|---|---|
| Transform | Position (4, 0.5, 0), Scale (0.5, 0.5, 0.5) |
| MeshFilter | Cube (内置网格) |
| MeshRenderer | PickUp Material (金色) |
| BoxCollider | **isTrigger = true** |
| Rotator (脚本) | 旋转动画 |
| Rigidbody | isKinematic = true, useGravity = false |
| Tag | `PickUp` |

### 5.5 DynamicBox.prefab

| 组件 | 配置 |
|---|---|
| Transform | Position (0, 0.25, 0), Scale (0.25, 0.25, 0.25) |
| MeshFilter | Cube (内置网格) |
| MeshRenderer | Dynamic Obstacle Material (黑色) |
| BoxCollider | 标准碰撞体 |
| Rigidbody | mass = 0.1, useGravity = true, isKinematic = false |
| NavMeshObstacle | carve = true, carveOnlyStationary = true, timeToStationary = 0.5s |
| Tag | Untagged |

**设计要点**: DynamicBox 是可推动的物理方块。NavMeshObstacle 的 `carve = true` 使得静止后的方块会在 NavMesh 上挖洞，迫使敌人绕行，增加游戏策略性。

---

## 6. 材质清单

| 材质 | 颜色 (RGB) | Smoothness | 使用对象 |
|---|---|---|---|
| Player (AI 生成) | 贴图 (texture_0, 2048×2048) | - | AI 生成角色模型 |
| FootprintMat | 半透明 | - | 脚印 (RenderQueue=3000) |
| Enemy | 红色 (1, 0, 0) | 0.50 | EnemyBody 方块 |
| PickUp | 金色 (1, 0.78, 0) | 0.25 | 收集品方块 |
| HealthBar | 灰色 (0.51, 0.51, 0.51) | 0.25 | 地面 |
| wall | 深灰 (0.31, 0.31, 0.31) | 0.25 | 墙壁、静态障碍 |
| Dynamic Obstacle | 黑色 (0, 0, 0) | 0.50 | 动态方块 |
| HealthPotion | 红色 (0.8, 0.1, 0.1) | 0.50 | 血瓶模型 |

AI 生成角色使用贴图材质，其余材质已迁移至 URP Lit/Unlit Shader。

---

## 7. NavMesh 配置

| 参数 | 值 |
|---|---|
| 代理半径 | 0.5 |
| 代理高度 | 2 |
| 最大坡度 | 45° |
| 攀爬高度 | 0.4 |
| NavMeshSurface 尺寸 | 10 × 10 × 10 |
| NavMeshSurface 中心 | (0, 2, 0) |
| 烘焙状态 | 已烘焙 |

---

## 8. 构建产物

```
Builds/
├── Test.exe                      — 主程序
├── TuanjiePlayer.dll             — Tuanjie 运行时
├── TuanjieCrashHandler64.exe    — 崩溃处理器
├── Test_Data/
│   ├── boot.config               — 启动配置
│   ├── globalgamemanagers        — 全局管理器
│   ├── level0                    — 场景数据
│   ├── resources.assets           — 内置资源
│   ├── sharedassets0.assets       — 共享资源
│   └── Managed/
│       ├── Assembly-CSharp.dll   — 编译后游戏脚本
│       └── (50+ Unity 模块 DLL)
└── MonoBleedingEdge/             — Mono 运行时
```

---

## 9. 技术总结

| 维度 | 评估 |
|---|---|
| 代码规模 | 15 个脚本，约 900 行代码，结构清晰 |
| 架构模式 | 经典 MonoBehaviour 组件模式 + AudioManager 单例 |
| 渲染管线 | URP (从 Built-in 迁移)，VFX Graph 粒子特效 |
| 物理系统 | Rigidbody + velocity 恒定速度移动（保留 Y 轴）；freezeRotation=true；CapsuleCollider 玩家 |
| 战斗系统 | 鼠标左键发射火球，VFX Graph 视觉 + SphereCollider 碰撞检测 |
| 血量系统 | HealthBar：100 HP，OnCollisionStay 持续扣血，归零判定失败 |
| 经验系统 | ExpBar：收集 +10 EXP，满 100 升级，每级 maxExp +20，while 循环支持跨多级 |
| 升级选择系统 | UpgradeSystem：暂停游戏，四选三随机（MaxHealth/Speed/FireballCount/MagnetRange），悬浮高亮卡片 |
| 自动吸取 | MagnetDetector + PickupItem：半径3米内自动吸引，MoveTowards 飞行，满血不吸取血瓶 |
| 敌人生成 | EnemySpawner：屏幕外刷新，最多 30 个，0.5s 间隔，NavMesh 采样 |
| AI 系统 | NavMeshAgent 寻路 + NavMeshObstacle 动态避障 |
| UI 系统 | uGUI Canvas + TextMeshPro + 血量条 + 经验条 + 游戏结束面板 |
| 输入系统 | 旧版 Input Manager (GetAxis + GetMouseButtonDown) |
| 音频系统 | AudioManager 单例，11 种 AI 生成 SFX 音效 |
| 动画系统 | Animator Controller (Speed 驱动 Idle/Walk/Run 状态机) + AI 生成角色动画 |
| 资源管理 | AI 生成模型/贴图/动画/SFX (Meshy AI + TJGenerators)；Quaternius 3D 模型；VFX Graph 特效；ObjectPool 对象池复用 |
| 难度递增 | 每10秒：生成间隔-0.02s（最低0.15s）、上限+2（最高60）、血量+1（最高10） |
| 敌人血量 | EnemyMovement：maxHealth/currentHealth/TakeDamage/Die()，头顶 World Space Canvas 血条 |
| 持久化 | 无 (无存档系统) |
