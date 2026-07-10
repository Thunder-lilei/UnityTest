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

---

## 知识点积累

<!-- 零散知识点速记 -->
