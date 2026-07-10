# 技术文档 — 迷你游戏 (Roll-a-Ball)

> **项目名称**: Test  
> **产品名称**: 迷你游戏  
> **引擎版本**: Unity 2022.3.62t11 (Tuanjie / Unity 中国版 1.9.3)  
> **渲染管线**: Built-in Render Pipeline  
> **目标平台**: Windows Standalone (x86_64)  
> **文档日期**: 2026-07-10

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
| 渲染路径 | Forward |
| 后台运行 | 开启 |
| Graphics Jobs | 开启 |

### 1.2 标签 (Tags)

| 标签名 | 用途 |
|---|---|
| `PickUp` | 可收集物品 |
| `Enemy` | 敌人实体 |

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
| `com.unity.textmeshpro` | 3.0.9 | UI 文本渲染 |
| `com.unity.timeline` | 1.7.7 | 时间线 |
| `com.unity.ugui` | 1.0.0 | uGUI 框架 |
| `com.unity.visualscripting` | 1.9.4 | 可视化脚本 |

---

## 2. 脚本架构

项目共包含 **4 个 C# 脚本**，均位于 `Assets/Scripts/` 目录下，无自定义命名空间。

```
Assets/Scripts/
├── CameraController.cs    — 摄像机跟随
├── PlayerController.cs    — 玩家控制 & 游戏状态
├── EnemyMovement.cs       — 敌人 AI 寻路
└── Rotator.cs             — 收集品旋转动画
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

**职责**: 玩家输入、物理移动、碰撞检测、得分管理、胜负判定。

**依赖**: `UnityEngine`, `TMPro` (TextMeshPro)

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `speed` | `float` | public | 移动力度倍率，场景中设为 `10` |
| `countText` | `TextMeshProUGUI` | public | 得分 UI |
| `winTextObject` | `GameObject` | public | 胜负提示 UI |
| `rb` | `Rigidbody` | private | 物理刚体引用 |
| `count` | `int` | private | 当前收集数量 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | 获取 `Rigidbody`，初始化 `count = 0`，隐藏胜负文本 |
| `FixedUpdate()` | 物理帧 | 读取 `Input.GetAxis("Horizontal/Vertical")`，通过 `rb.AddForce()` 施加 X/Z 轴力 |
| `OnTriggerEnter(Collider)` | 碰撞回调 | 碰到 `PickUp` 标签对象 → 禁用该对象，`count++`，更新 UI |
| `OnCollisionEnter(Collision)` | 碰撞回调 | 碰到 `Enemy` 标签对象 → 销毁玩家，显示"失败!" |
| `SetCountText()` | 自定义 | 更新得分为"得分: N"；当 `count >= 4` → 显示"胜利!"，销毁所有 Enemy |

**核心逻辑流**:

```
Input → FixedUpdate → AddForce → 物理引擎模拟移动
                                        ↓
                              OnCollisionEnter(Enemy) → 失败
                              OnTriggerEnter(PickUp)  → count++
                                  └── count >= 4 → 胜利，销毁 Enemy
```

### 2.3 EnemyMovement.cs

**职责**: 敌人 AI 追踪行为。

**依赖**: `UnityEngine`, `UnityEngine.AI`

| 字段 | 类型 | 可见性 | 说明 |
|---|---|---|---|
| `player` | `Transform` | public | 追踪目标 (玩家) |
| `navMeshAgent` | `NavMeshAgent` | private | NavMesh 代理 |

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Start()` | 初始化 | 获取 `NavMeshAgent` 组件 |
| `Update()` | 每帧 | 若 `player` 不为空，调用 `navMeshAgent.SetDestination(player.position)` |

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

### 2.4 Rotator.cs

**职责**: 装饰性旋转动画，应用于收集品。

| 方法 | 生命周期 | 逻辑 |
|---|---|---|
| `Update()` | 每帧 | `transform.Rotate(new Vector3(15, 30, 45) * Time.deltaTime)` |

**设计要点**: 旋转速度为 `(15, 30, 45)` 度/秒，三轴非均匀旋转产生视觉趣味。乘以 `Time.deltaTime` 确保帧率无关。

---

## 3. 脚本关系图

```
┌─────────────────────────────────────────────────┐
│                   PlayerController              │
│  ┌─────────────────────────────────────────┐    │
│  │  FixedUpdate: Input → AddForce          │    │
│  │  OnTriggerEnter(PickUp) → count++ → UI   │    │
│  │  OnCollisionEnter(Enemy) → Destroy self │    │
│  │  count >= 4 → Victory, Destroy Enemy    │    │
│  └─────────────────────────────────────────┘    │
│         │                    │                   │
│    引用 ↓               引用 ↓                   │
│  ┌──────────┐      ┌──────────────────┐         │
│  │ TMP UGUI │      │  Enemy (Tag)     │         │
│  │ CountText│      │  → EnemyMovement │         │
│  │ WinText   │      │    → NavMeshAgent│         │
│  └──────────┘      │    → SetDestination
│                    └──────────────────┘         │
│  CameraController                                │
│    → 引用 Player (跟随偏移)                       │
│  Rotator                                        │
│    → 挂载于 PickUp Prefab                        │
└─────────────────────────────────────────────────┘
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
├── Player                [SphereCollider, Rigidbody, PlayerController]
├── wall                  [父对象]
│   ├── West Wall        [BoxCollider, Cube Mesh]
│   ├── East Wall        [BoxCollider, Cube Mesh]
│   ├── North Wall       [BoxCollider, Cube Mesh (旋转90°)]
│   └── South Wall       [BoxCollider, Cube Mesh (旋转90°)]
├── PickUp Parent         [父对象]
│   ├── PickUp           [Prefab 实例]
│   ├── PickUp (1)       [Prefab 实例]
│   ├── PickUp (2)       [Prefab 实例]
│   └── PickUp (3)       [Prefab 实例]
├── Canvas               [Canvas, CanvasScaler, GraphicRaycaster]
│   ├── CountText        [TextMeshProUGUI]
│   └── WinText          [TextMeshProUGUI]
├── EventSystem           [EventSystem, StandaloneInputModule]
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

### 5.1 PickUp.prefab

| 组件 | 配置 |
|---|---|
| Transform | Position (4, 0.5, 0), Scale (0.5, 0.5, 0.5) |
| MeshFilter | Cube (内置网格) |
| MeshRenderer | PickUp Material (金色) |
| BoxCollider | **isTrigger = true** |
| Rotator (脚本) | 旋转动画 |
| Rigidbody | isKinematic = true, useGravity = false |
| Tag | `PickUp` |

### 5.2 DynamicBox.prefab

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
| Player | 青色 (0, 0.86, 1) | 0.75 | Player 球体 |
| Enemy | 红色 (1, 0, 0) | 0.50 | EnemyBody 方块 |
| PickUp | 金色 (1, 0.78, 0) | 0.25 | 收集品方块 |
| wall | 深灰 (0.31, 0.31, 0.31) | 0.25 | 墙壁、地面、静态障碍 |
| Dynamic Obstacle | 黑色 (0, 0, 0) | 0.50 | 动态方块 |
| Background | 灰色 (0.51, 0.51, 0.51) | 0.25 | (备用) |

所有材质均使用内置 Standard Shader，Metallic = 0，不透明渲染模式，无贴图。

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
| 代码规模 | 4 个脚本，约 150 行代码，结构简洁清晰 |
| 架构模式 | 经典 MonoBehaviour 组件模式，脚本间通过 Inspector 引用耦合 |
| 物理系统 | Rigidbody + AddForce 力驱动移动；BoxCollider 碰撞；SphereCollider 玩家球体 |
| AI 系统 | NavMeshAgent 寻路 + NavMeshObstacle 动态避障 |
| UI 系统 | uGUI Canvas + TextMeshPro |
| 输入系统 | 旧版 Input Manager (GetAxis) |
| 音频系统 | 无 |
| 动画系统 | 仅脚本驱动的旋转 (无 Animation / Animator) |
| 资源管理 | 所有网格为内置图元，无外部模型/贴图 |
| 持久化 | 无 (无存档系统) |
