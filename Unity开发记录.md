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

### 问题9：音频播放延迟

**现象**：游戏中触发音效时，声音有明显延迟，不是即时播放。

**原因**：Unity 音频导入器默认 `Preload Audio Data = False`。音频文件在场景加载时不会被解码到内存，而是在**首次播放时**才实时解压加载。MP3/Vorbis 格式的解码需要时间，造成延迟。

**解决方案**：选中音频文件 → Inspector → 勾选 **Preload Audio Data** → Apply。

或在代码中修改：

```csharp
var importer = AssetImporter.GetAtPath("Assets/xxx.mp3") as AudioImporter;
var s = importer.defaultSampleSettings;
s.preloadAudioData = true;
importer.defaultSampleSettings = s;
importer.SaveAndReimport();
```

**Preload Audio Data 开启/关闭对比**：

| | 开启 (true) | 关闭 (false) |
|---|---|---|
| 播放延迟 | 无 | 首次播放有延迟 |
| 内存占用 | 高（解压后驻留） | 低（按需加载） |
| 场景加载时间 | 略长 | 快 |
| 适用场景 | 短音效 (SFX) | 长音频 (BGM) |

**原则**：
- **短音效**（1-3秒）：始终开启，内存开销可忽略
- **长音频**（BGM、几十秒以上）：关闭 preload，改用 **Streaming** 流式加载，边播边读，避免一次性占用大量内存

### 问题10：Unity 组件一对一原则

**结论**：一个 GameObject 上**同类型的组件只能有一个**。

**体现**：
- `GetComponent<HealthBar>()` 直接用类型获取，不需要索引或名称区分
- `AddComponent<HealthBar>()` 如果已存在同类型组件，返回已有的，不会新增
- 因此脚本设计时，一个组件类代表一种职责，挂在同一 GameObject 上只此一份

**基类的特殊情况**：`Collider` 是基类，`BoxCollider`、`SphereCollider`、`CapsuleCollider` 是不同子类型，可以同时挂在一个 GameObject 上。但 `GetComponent<Collider>()` 只返回找到的第一个。

**设计启示**：
- 需要多个同功能实例时（如多个 AudioSource），应该用**子 GameObject** 拆分，而不是在一个 GameObject 上重复挂载
- 本项目 AudioManager 就是这种做法：8 个子 GameObject 各挂一个 AudioSource，父对象 AudioManager 脚本统一管理

### 问题11：Unity 空方法体的性能影响

**结论**：没有额外性能消耗，可忽略。

**原理**：Unity 生命周期回调（`Start`、`Update`、`FixedUpdate`、`OnTriggerEnter` 等）通过**反射**在脚本初始化时检测一次，在原生层缓存"该脚本有哪些回调"的列表：

- **有该方法（含空方法体）**：注册回调，每帧从原生层调用一次空方法，开销几乎为零
- **无该方法**：不注册，零开销

**性能对比**：

| 情况 | 每帧开销 | 实际影响 |
|---|---|---|
| 无该方法 | 0 | 无 |
| 空方法体 | ~微秒级 | 可忽略 |
| 大量空 Update（如 1000+） | 可能可测量 | 才需关注 |

**建议**：
- 不需要担心空方法体的性能
- 但如果脚本确实不需要某个回调，删掉空方法是好习惯，保持代码整洁
- 真正的性能瓶颈在方法体内的逻辑（如 `GetComponent`、`Find`、`Instantiate`），不在于方法本身是否存在

### 问题12：GetComponent 性能

**问题**：`GetComponent<T>()` 内部遍历组件列表，在 `Update`/`OnCollisionStay` 等高频回调中每帧调用会产生开销。

**优化**：在 `Start()` 中获取一次，缓存到私有字段。本项目 PlayerController 原来在 `OnCollisionStay` 和 `OnTriggerEnter` 中每帧 `GetComponent<HealthBar>()` / `GetComponent<ExpBar>()`，已改为 `Start()` 中缓存。

**优先级**：Inspector 序列化引用 > Start 缓存 > 每帧 GetComponent

### 问题13：GameObject.Find 性能

**问题**：`GameObject.Find()` 遍历整个场景层级，O(n) 复杂度，不适合每帧调用。

**优化手段优先级**：

1. **序列化引用**（Inspector 拖入）— 零运行时开销
2. **单例模式**（`AudioManager.Instance`）— 全局唯一对象
3. **FindWithTag**（`GameObject.FindWithTag("Player")`）— 比 Find 快
4. **Transform.Find**（`transform.Find("ChildName")`）— 只搜子层级
5. **GameObject.Find** — 最后手段，仅在 `Start()` 中调用一次

**本项目优化**：FireBall.cs 原来在 `Start()` 中用 `GameObject.Find("Player")` + `FindObjectsOfType<Fireball>()` 做碰撞忽略，已改用 Layer 矩阵替代，完全删除。

### 问题14：Physics.IgnoreCollision vs Layer 碰撞矩阵

**问题**：`Physics.IgnoreCollision` 逐对设置碰撞忽略，对象多时需要 N² 次调用。

**Layer 碰撞矩阵方案**：

- `Project Settings → Physics → Layer Collision Matrix` 中勾选/取消 Layer 间的碰撞
- 引擎层处理，零代码开销
- 新增对象自动生效，无需逐对调用

**本项目实施**：

1. 新建 3 个 Layer：Player(8)、FireBall(9)、PickUp(10)
2. 碰撞矩阵配置：
   - FireBall × FireBall = ❌（火球不互相碰撞）
   - FireBall × Player = ❌（火球不碰玩家）
   - FireBall × PickUp = ❌（火球不碰拾取物）
3. FireBall.cs 的 `Start()` 中所有 `Physics.IgnoreCollision` 和 `FindObjectsOfType` 代码已删除
4. `OnTriggerEnter` 中 `if (PickUp/HealthPotion) return` 也已删除（Layer 已屏蔽）

**对比**：

| 方案 | 优点 | 缺点 |
|---|---|---|
| Physics.IgnoreCollision | 精确控制单个对象对 | N² 次调用；需代码维护 |
| Layer 碰撞矩阵 | 零代码开销；自动生效 | 需提前规划 Layer；同 Layer 行为一致 |

### 问题15：材质实例泄漏

**问题**：访问 `Renderer.material`（get）时，Unity 会复制一份 `sharedMaterial` 作为独立实例。这个实例不会自动销毁，需手动 `Destroy(material)`，否则内存泄漏。

**三种方案对比**：

| 方案 | 独立颜色 | 内存泄漏 | 适用场景 |
|---|---|---|---|
| `.material` | ✅ 每实例独立 | ⚠️ 需手动销毁 | 少量对象 |
| `.sharedMaterial` | ❌ 全部共享 | ✅ 无 | 所有对象颜色相同 |
| `MaterialPropertyBlock` | ✅ 每实例独立 | ✅ 无 | 大量对象需独立颜色 |

**MaterialPropertyBlock 原理**：数据容器，传给 GPU 的 per-object 常量缓冲区，不修改材质资产本身，不创建副本。

**本项目优化**：Footprint.cs 已从 `.material` 改为 `MaterialPropertyBlock`。

---

## 知识点积累

### 刚体 Freeze Rotation 防止角色翻倒

Rigidbody 组件中勾选 **Constraints → Freeze Rotation** 的 **X** 和 **Z** 轴，可以防止角色在移动或碰撞时翻倒，同时保留 **Y** 轴旋转（用于转向）。

- **冻结 X、Z 旋转**：角色不会因物理力向前/向后倾倒或侧翻
- **保留 Y 旋转**：角色仍可正常转向

这是第三人称/第一人称角色控制的常见处理方式，避免用 AddForce 移动时角色因碰撞而东倒西歪。

### Null 条件运算符 `?.`

**含义**：`?.` 在调用前自动判空。如果左侧为 null，整个表达式直接返回 null（跳过后续调用）；不为 null 则正常执行。

**语法**：

```csharp
对象?.方法()
对象?.属性
```

**等价写法**：

```csharp
AudioManager.Instance?.PlayPlayerHurt();

// 等价于
if (AudioManager.Instance != null)
    AudioManager.Instance.PlayPlayerHurt();
```

**适用场景**：
- **单例访问**：`AudioManager.Instance` 可能尚未初始化
- **事件调用**：`事件?.Invoke()` 避免 null delegate 异常
- **链式调用**：`player?.transform?.position` 逐级判空

**注意**：
- `?.` 只对**引用类型**（class、interface、delegate）有效，值类型（int、struct）不行
- 如果方法有返回值，`?.` 返回的是**可空类型**，如 `int?`

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
