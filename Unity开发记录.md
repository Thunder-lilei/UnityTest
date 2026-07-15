# Unity 开发学习记录

> 项目路径：`UnityTest/Test/`
> 引擎版本：
> 开始日期：2026-07-10

---

## 学习日志

### 2026-07-10

- 创建 Test 学习项目
- 完成编辑器汉化
  - 通过团结 Hub → 安装 → ⚙️齿轮 → 添加模块 → Language packs (Preview) → 勾选简体中文安装
  - 安装后 Edit → Preferences → General → Language 切换为 Chinese
- 开始学习 Roll-a-Ball 教程（learn.unity.com/project/roll-a-ball）

### 2026-07-11

- 使用 Meshy AI (TJGenerators 插件) 生成 Humanoid 角色模型
  - 提示词：a young adventurer character wearing casual explorer outfit
  - 生成耗时约 5 分 45 秒，包含模型 + 贴图 + Idle/Walk/Run 三套动画
  - 自动创建 Animator Controller（Speed 驱动状态过渡，Action 触发特殊动作）
- 将 Player 从球体替换为 AI 生成的角色 Prefab

### 2026-07-13

- PlayerController 重构：AddForce → rb.velocity 恒定速度移动
- 新增角色朝向移动方向（Quaternion.Slerp 平滑转向）
- 新增脚印系统：移动时左右交替生成脚印，3 秒渐隐消失
- 刚体 Freeze Rotation X/Z 防止角色翻倒
- 补充 NavMeshSurface 烘焙分析文档

---

## 问题与解决

### 问题1：Input System 版本不兼容

**问题**：Roll-a-Ball 教程基于 **Unity 6.3**，使用 **新版 Input System**，脚本中引用了 `UnityEngine.InputSystem` 命名空间和 `InputValue` 类型。而本地使用的是 **团结引擎 2022.3.62t11**，默认使用 **经典 Input Manager**，未安装 Input System 包，导致编译报错：

```
error CS0234: The type or namespace name 'InputSystem' does not exist in the namespace 'UnityEngine'
error CS0246: The type or namespace name 'InputValue' could not be found
```

**原因**：

| | 教程（Unity 6.3） | 本地（团结 2022.3） |
|---|---|---|
| 输入系统 | 新版 Input System | 经典 Input Manager |
| 命名空间 | `UnityEngine.InputSystem` | 不需要额外引用 |
| 输入获取方式 | `OnMove(InputValue)` 回调 | `Input.GetAxis()` |
| 是否需安装包 | 内置 | 需通过 Package Manager 安装 |

**解决方案**：无需安装额外包，将脚本改为经典 Input Manager 方式：

```csharp
// ❌ 删除
using UnityEngine.InputSystem;
void OnMove(InputValue movementValue) { ... }

// ✅ 替换为
void FixedUpdate()
{
    float movementX = Input.GetAxis("Horizontal"); // A/D、←→
    float movementY = Input.GetAxis("Vertical");   // W/S、↑↓
    Vector3 movement = new Vector3(movementX, 0.0f, movementY);
    rb.AddForce(movement * speed);
}
```

> **提示**：如果确实想用新版 Input System，可在 `Window > Package Manager` 安装 `Input System` 包，并在 `Edit > Project Settings > Player > Active Input Handling` 中切换为 "Both" 或 "Input System Package (New)"。但学习阶段用经典方式即可。

### 问题2：材质面板找不到 Base Map 选项

**问题**：Roll-a-Ball 教程中让设置材质的 **Base Map** 属性，但在材质 Inspector 面板中找不到该选项。

**原因**：

| | 教程（Unity 6.3） | 本地（团结 2022.3） |
|---|---|---|
| 渲染管线 | URP（Universal Render Pipeline） | Built-in Render Pipeline |
| 材质属性名 | **Base Map** | **Albedo** |

Unity 6 默认使用 URP 渲染管线，材质面板属性命名与内置渲染管线不同。

**解决方案**：在 Inspector 面板的 **Main Maps** 区域，点击 **Albedo** 右侧的颜色色块，在弹出的取色器中选择目标颜色即可。功能与教程中的 Base Map 完全一致，仅名称不同。

> **提示**：后续教程中如再遇到属性名对不上的情况，通常都是渲染管线差异导致。常见对照：
> - Base Map → Albedo
> - Base Color → Albedo（颜色色块）
> - Smoothness 在两者中名称一致
> - Metallic 在两者中名称一致

### 问题3：Scene Gizmo 在哪里

**问题**：教程提到切换俯视视图，但找不到 Gizmo 位置。

**解答**：场景视图右上角有一个由三根彩色箭头组成的坐标轴手柄，这就是 Scene Gizmo：

```
        Y (绿)
        |
        |
        +——— X (红)
       /
      Z (蓝)
```

- 红色 = X 轴（右）
- 绿色 = Y 轴（上）
- 蓝色 = Z 轴（前）

**切换俯视视图步骤**：
1. 找到场景视图右上角的彩色坐标轴手柄
2. 点击绿色的 Y 轴（顶部那根）→ 切换到俯视视图
3. 手柄下方有一个小方块图标，点击它可以切换透视/正交模式

**其他方法**：
- 快捷键：按 F2（顶视图）
- 点击手柄中心的方块：会在轴测视图和正交视图之间切换

### 问题4：PickUp 已经能触发碰撞消失，为什么还要加 Rigidbody

**问题**：教程要求给 PickUp 预制体添加 Rigidbody 组件，但在上一步将 Collider 设为 Trigger 后，碰撞消失效果已经实现了，疑惑为什么还要多加一步。

**原因**：PickUp 挂有 `Rotator` 脚本会持续旋转，属于**移动的 Collider**。

| 状态 | 分类 | 性能影响 |
|---|---|---|
| 无 Rigidbody + 不动 | 静态碰撞器 | ✅ 正常 |
| 无 Rigidbody + **移动/旋转** | 静态碰撞器 | ⚠️ Unity 每帧重新计算物理空间分区，性能开销大 |
| 有 Kinematic Rigidbody + 移动 | 运动学刚体 | ✅ 高效，不参与物理模拟但 Trigger 检测正常 |

当前能工作是因为 **Player 身上有 Rigidbody**，`OnTriggerEnter` 可以正常触发。

**解决方案**：给 PickUp 添加 Rigidbody，并勾选 **Is Kinematic**：
- **Is Kinematic** = true → 不受物理力影响（不会掉落、不会因碰撞弹开）
- 但仍被 Unity 识别为"可移动物体"，物理空间分区不会每帧重算
- Trigger 检测功能不受影响

**结论**：不加也能用，但这是 Unity 物理系统的**最佳实践**：移动的 Trigger Collider 应搭配 Kinematic Rigidbody 以保证性能，尤其当场景中同类物体较多时。

### 问题5：TextMeshPro 输入中文显示空白或方块

**问题**：使用 TextMeshPro (TMP) 创建的 UI 文本输入中文后无法显示，显示为空白或方块。

**原因**：TextMeshPro 的默认字体资产 `LiberationSans SDF` 只包含拉丁字母、数字和常见符号的字符字形（glyph），**不包含中文字符集**。TMP 与 Legacy Text 不同，它使用自己的字体资产格式（SDF Atlas），不能直接使用 .ttf/.ttc/.otf 字体文件，必须先生成 TMP Font Asset。

| 组件 | 默认字体 | 中文支持 |
|---|---|---|
| Legacy Text | Arial | ❌ 需替换 Font |
| TextMeshPro | LiberationSans SDF | ❌ 需生成并替换 Font Asset |

**解决方案**：
1. **导入中文字体文件**：将系统字体（如微软雅黑 `msyh.ttc`）从系统字体目录复制到 `Assets/TextMesh Pro/Fonts/` 目录下
2. **生成 TMP Font Asset**：在 Project 窗口右键该字体 → `Create > TextMeshPro > Font Asset`
3. **替换字体资产**：选中 TMP 文本物体 → Inspector → **Font Asset** 字段 → 将默认的 `LiberationSans SDF` 替换为新生成的中文字体资产

**注意事项**：
- 可选的中文字体：`msyh.ttc`（微软雅黑）、`simhei.ttf`（黑体）、`simkai.ttf`（楷体）等，均在系统字体目录下
- 生成 TMP Font Asset 时文件会比较大，因为中文字符集包含数万个字形

### 问题6：Add Component 中找不到 NavMeshSurface 组件

**问题**：教程要求给地面添加 NavMeshSurface 组件，但在 Add Component 菜单中找不到该组件。

**原因**：NavMeshSurface 组件来自 AI Navigation 包，这是一个可选包，不会随 Unity 编辑器默认安装。教程基于 Unity 6.3（可能预装了该包），而团结引擎 2022.3 需要手动安装。

| 导航方式 | 来源 | 是否默认安装 |
|---|---|---|
| NavMesh（旧版，通过 Navigation 窗口烘焙） | 内置 com.unity.modules.ai | ✅ 是 |
| NavMeshSurface（新版，组件式） | com.unity.ai.navigation 包 | ❌ 需手动安装 |

**解决方案**：通过 Package Manager 安装 com.unity.ai.navigation 包：

`Window > Package Manager > 搜索 "AI Navigation" > Install`

或在 manifest.json 中添加依赖。安装后即可在 Add Component 中搜索到 NavMeshSurface。

### 问题7：角色站立转圈问题

**现象**：角色停止移动后，缓慢绕 Y 轴旋转（转圈）。

**原因**：Rigidbody 的 `freezeRotation` 未勾选（`false`）。角色静止时，CapsuleCollider 与地面 MeshCollider 接触产生微小摩擦扭矩，物理引擎将扭矩施加到 Y 轴旋转上，导致角色缓慢转动。

`angularVelocity.y = -0.061` 的数据证实了这一点 — 角色有持续的 Y 轴角速度。

**为什么之前没问题**：早期版本用的是 `SphereCollider`，球体与平面接触时不会产生旋转扭矩。换成 `CapsuleCollider` 后，胶囊体的接触面更大，摩擦扭矩更明显。

**解决方案**：将 Rigidbody 的 `freezeRotation` 设为 `true`。

```
Rigidbody.freezeRotation = true
```

**补充说明**：`freezeRotation = true` 会冻结所有三个轴的旋转。脚本中通过 `Quaternion.Slerp` 控制角色朝向（仅 Y 轴），不受 `freezeRotation` 影响 — 因为 Slerp 直接修改 `transform.rotation`，绕过了物理引擎的旋转计算。

**相关配置**：

| 配置项 | 值 | 说明 |
|--------|-----|------|
| Rigidbody.freezeRotation | true | 防止物理扭矩导致旋转 |
| Rigidbody.constraints | FreezeRotationX, FreezeRotationZ | 已通过 constraints=80 冻结 X 和 Z，但 Y 未冻结 |
| PlayerController 朝向逻辑 | `movement.magnitude > 0.1f` | 只有真正有输入时才旋转，过滤微小残留 |

> 注意：`constraints = 80`（即 `FreezeRotationX | FreezeRotationZ`）只冻结了 X 和 Z 轴，Y 轴未冻结。`freezeRotation = true` 补充冻结了 Y 轴，彻底解决问题。

### 问题8：Prefab 无法引用场景对象

**现象**：在 `EnemyMovement.cs` 中添加了 `public GameObject pickup;`，想在 Inspector 中把场景中的 `PickUp Parent` 对象拖入该字段，但无法操作。

**原因**：`EnemyMovement` 脚本挂载在 **Prefab 资产**（`Assets/Quaternius/.../zombie_basic.prefab`）上。

**Prefab 是磁盘上的资产，只能引用其他项目资产**（如 `.prefab`、`.mat`、`.png` 等）。而场景中的对象（如 `PickUp Parent`）只在运行时存在，不在磁盘上，因此 Prefab 无法引用它。

**解决方案**：不在 Inspector 中手动指定场景对象，改为运行时用 `GameObject.Find()` 查找：

```csharp
// ❌ 不可行：Prefab 无法引用场景对象
public GameObject pickup;
Instantiate(pickUpPrefab, transform.position, Quaternion.identity, pickup.transform);

// ✅ 正确：运行时查找场景对象
Transform parent = GameObject.Find("PickUp Parent")?.transform;
Instantiate(pickUpPrefab, transform.position, Quaternion.identity, parent);
```

> **关键原则**：**Prefab 资产只能引用项目资产，不能引用场景对象。** 如果需要引用场景中的对象，应在运行时通过 `GameObject.Find()` 或 `FindWithTag()` 等方式查找。

---

## 知识点积累

### 刚体 Freeze Rotation 防止角色翻倒

Rigidbody 组件中勾选 **Constraints → Freeze Rotation** 的 **X** 和 **Z** 轴，可以防止角色在移动或碰撞时翻倒，同时保留 **Y** 轴旋转（用于转向）。

- **冻结 X、Z 旋转**：角色不会因物理力向前/向后倾倒或侧翻
- **保留 Y 旋转**：角色仍可正常转向

这是第三人称/第一人称角色控制的常见处理方式，避免用 AddForce 移动时角色因碰撞而东倒西歪。

---

## AI 角色生成 — Animated Character (Meshy AI)

### 生成原理

通过 Unity TJGenerators 插件调用 **Meshy AI**（meshy.ai）云端 API，由 AI 从自然语言文字描述自动生成带骨骼绑定的 Humanoid 3D 角色模型及配套动画，全自动导入 Unity 并组装为可用的 Prefab。

### 生成参数

| 参数 | 值 |
|---|---|
| 生成工具 | Meshy AI (meshy-animation) |
| 任务 ID | `animated_character_1_639193847565592610` |
| 文字提示词 | `a young adventurer character wearing casual explorer outfit, brown boots, sturdy jacket, ready for adventure, friendly appearance, stylized game character` |
| 开始时间 | 2026-07-11 16:39:16 |
| 结束时间 | 2026-07-11 16:45:02 |
| 总耗时 | 345 秒（约 5 分 45 秒） |
| 模型格式 | FBX |
| 骨骼类型 | Humanoid |

### 生成流程

```
用户提交文字描述
      │
      ▼
┌──────────────────┐
│  Meshy AI 云端    │
│  1. 生成角色模型   │ ──→ ae749bf5fe5aaf00.fbx (含骨骼 + 网格)
│  2. 生成贴图纹理   │ ──→ texture_0.png (2048×2048)
│  3. 生成 Idle 动画 │ ──→ ae749bf5fe5aaf00_animation.fbx
│  4. 生成 Walk 动画 │ ──→ ae749bf5fe5aaf00_walking.fbx
│  5. 生成 Run 动画  │ ──→ ae749bf5fe5aaf00_running.fbx
└──────────────────┘
      │
      ▼
┌──────────────────┐
│  Unity 导入 & 配置 │
│  1. FBX 导入为 Humanoid Rig
│  2. 提取 AnimationClip
│  3. 创建 Animator Controller (状态机 + 过渡 + 参数)
│  4. 组装 Prefab (模型 + Animator + CapsuleCollider)
└──────────────────┘
      │
      ▼
  Assets/Characters/Player.prefab
```

### 生成资源清单

| 资源文件 | 类型 | 大小 | 说明 |
|---|---|---|---|
| `ae749bf5fe5aaf00.fbx` | 模型 | 8292.5 KB | 角色模型（含骨骼骨架 + 网格 + Avatar） |
| `ae749bf5fe5aaf00.fbm/texture_0.png` | 贴图 | 6849.8 KB | 角色漫反射贴图，2048×2048，DXT1 压缩，12 级 Mipmap |
| `ae749bf5fe5aaf00_animation.fbx` | 动画 | 8337.7 KB | Idle 待机动画 |
| `ae749bf5fe5aaf00_walking.fbx` | 动画 | 8316.8 KB | Walk 行走动画 |
| `ae749bf5fe5aaf00_running.fbx` | 动画 | 8308.4 KB | Run 奔跑动画 |
| `ae749bf5fe5aaf00_Controller.controller` | 控制器 | 10.2 KB | Animator Controller |
| `Player.prefab` | Prefab | 4.4 KB | 最终组装的角色 Prefab |

所有资源路径统一位于：`Assets/TJGenerators/History/Player/01/`，Prefab 输出到 `Assets/Characters/Player.prefab`。

### 模型导入配置

| 配置项 | 值 |
|---|---|
| Animation Type | Humanoid |
| Material Import Mode | ImportViaMaterialDescription |
| Material Naming | BasedOnTextureName |
| Material Search | Local |
| Use File Units | True |
| Bake Axis Conversion | False |

### 贴图信息

| 属性 | 值 |
|---|---|
| 尺寸 | 2048 × 2048 |
| 格式 | DXT1 (压缩) |
| Mipmap 层数 | 12 |

### 动画片段详情

| 动画 | 时长 | 帧率 | 循环模式 | Humanoid |
|---|---|---|---|---|
| Idle (`Backflip`) | 2.13s | 30 fps | Default | ✅ |
| Walk (`walking_man`) | 1.03s | 30 fps | Default | ✅ |
| Run (`running`) | 0.63s | 30 fps | Default | ✅ |

### Animator Controller 状态机

**层级**: 1 层 (Base Layer)

**参数**:

| 参数名 | 类型 | 用途 |
|---|---|---|
| `Speed` | Float | 驱动 Idle ↔ Walk ↔ Run 过渡 |
| `Action` | Trigger | 触发 Action 特殊动作 |

**状态 (4 个)**:

| 状态名 | 关联动画 | 说明 |
|---|---|---|
| Idle | `walking_man` | 待机状态（默认入口的相邻状态） |
| Walk | `walking_man` | 行走状态 |
| Run | `running` | 奔跑状态 |
| Action | `Backflip` | 特殊动作状态（默认状态） |

**过渡条件**:

| 来源 → 目标 | 条件 |
|---|---|
| Idle → Walk | `Speed > 0.1` |
| Idle → Run | `Speed > 0.5` |
| Walk → Idle | `Speed < 0.1` |
| Walk → Run | `Speed > 0.5` |
| Run → Walk | `Speed < 0.5` |
| Run → Idle | `Speed < 0.1` |
| Action → Walk | 无条件（自动过渡） |

**默认状态**: Action

### Prefab 结构

```
Player (GameObject)
├── Transform
├── Animator
│   ├── Avatar: ae749bf5fe5aaf00.fbx (Avatar)
│   ├── Controller: ae749bf5fe5aaf00_Controller.controller
│   ├── Apply Root Motion: False
│   ├── Culling Mode: Always Animate
│   └── Update Mode: Normal
└── GeneratedModel (子物体)
    ├── Transform
    ├── MeshFilter (模型网格)
    ├── MeshRenderer (材质 texture_0)
    └── CapsuleCollider (Radius=0.5, Height=2)
```

### 依赖关系

```
Player.prefab
├── ae749bf5fe5aaf00.fbx          (模型 + Avatar)
├── ae749bf5fe5aaf00.fbm/texture_0.png  (贴图)
├── ae749bf5fe5aaf00_animation.fbx (Idle 动画)
├── ae749bf5fe5aaf00_walking.fbx  (Walk 动画)
├── ae749bf5fe5aaf00_running.fbx  (Run 动画)
└── ae749bf5fe5aaf00_Controller.controller (状态机)
```

### API 认证方式

TJGenerators 插件内置 API 密钥，调用 Meshy AI 时不需用户手动登录或配置，提交文字描述后全自动完成生成 → 下载 → 导入 → 组装流程。
