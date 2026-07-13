# Ground NavMesh 烘焙详细分析报告

> **数据来源**：场景文件 `Assets/Scenes/迷你游戏.scene` 原始序列化数据、Unity Editor 实时组件数据（instanceID: 25310）、AI Navigation 1.1.5 源码 `NavMeshSurface.cs` 与 `NavMeshSurfaceEditor.cs`、`ProjectSettings/NavMeshAreas.asset`、`Assets/Prefabs/DynamicBox.prefab`、`Assets/Scripts/EnemyMovement.cs`。

---

## 一、CollectObjects 枚举定义

来源：`NavMeshSurface.cs`

| 枚举名 | 整数值 | Inspector 显示名 | 说明 |
|---|---|---|---|
| All | 0 | All Game Objects | 收集场景中所有活跃物体 |
| Volume | 1 | Volume | 收集烘焙体积内的物体 |
| Children | 2 | Current Object Hierarchy | 收集当前 GameObject 及其所有子物体 |
| MarkedWithModifier | 3 | NavMeshModifier Component Only | 仅收集挂有 NavMeshModifier 的物体（需 Unity 2022.2+） |

**本项目实际值**：`m_CollectObjects = 2` → **Children**。烘焙时仅收集 Ground 自身及其子物体的几何体。

---

## 二、NavMeshCollectGeometry 枚举定义

来源：`UnityEngine.AI`（引擎内置 API）

| 枚举名 | 整数值 | 说明 |
|---|---|---|
| RenderMeshes | 0 | 使用 MeshRenderer 的渲染网格三角面 |
| PhysicsColliders | 1 | 使用 Collider 组件的物理形状 |

**本项目实际值**：`m_UseGeometry = 0` → **RenderMeshes**

---

## 三、NavMeshSurface 全部序列化字段详解

每个字段按三维度分类：**序列化值**（场景文件中的实际值）、**Inspector 可见性**（是否在自定义 Editor 中绘制）、**实际生效性**（是否影响烘焙行为）。

### 3.1 Agent 与区域设置

| 序列化字段 | 序列化值 | Inspector 可见 | 实际生效 | 说明 |
|---|---|---|---|---|
| `m_AgentTypeID` | 0 | ✅ 弹出菜单 | ✅ | 引用 NavMeshAreas.asset 中 ID=0 的 Agent（Humanoid）。决定烘焙时使用的 agentRadius/agentHeight/agentSlope/agentClimb |
| `m_DefaultArea` | 0 (Walkable) | ✅ 弹出菜单 | ✅ | 烘焙生成的 NavMesh 多边形默认标记为 Area 0（Walkable, cost=1） |
| `m_GenerateLinks` | false | ✅ 复选框 | ✅ | 是否自动生成 OffMeshLink。关闭，场景无需跳跃/攀爬链接 |
| `m_UseGeometry` | 0 (RenderMeshes) | ✅ 枚举下拉 | ✅ | 使用渲染网格而非碰撞体来提取三角面 |

### 3.2 对象收集设置

| 序列化字段 | 序列化值 | Inspector 可见 | 实际生效 | 说明 |
|---|---|---|---|---|
| `m_CollectObjects` | 2 (Children) | ✅ 枚举下拉 | ✅ | 决定几何体收集范围。Children 模式 = 收集 Ground 及其子物体 |
| `m_LayerMask` | 0xFFFFFFFF（全选） | ✅ 层级选择器 | ✅ | 32 位位掩码，全 1 = 所有 Layer 参与收集 |
| `m_Size` | (10, 10, 10) | ❌ 隐藏 | ❌ 不生效 | 仅在 `CollectObjects = Volume` 时显示且生效。当前 Children 模式下 bounds 从实际几何体自动计算 |
| `m_Center` | (0, 2, 0) | ❌ 隐藏 | ❌ 不生效 | 同 m_Size，仅在 Volume 模式下显示且生效 |

**m_Size / m_Center 不可见且不生效的原因**（源码确认）：

`NavMeshSurfaceEditor.cs` 条件渲染：
```csharp
if ((CollectObjects)m_CollectObjects.enumValueIndex == CollectObjects.Volume)
{
    EditorGUILayout.PropertyField(m_Size);
    EditorGUILayout.PropertyField(m_Center);
}
```

`NavMeshSurface.cs` bounds 计算：
```csharp
var surfaceBounds = new Bounds(m_Center, Abs(m_Size));
if (m_CollectObjects != CollectObjects.Volume)
{
    surfaceBounds = CalculateWorldBounds(sources); // 从实际几何体自动计算
}
```

### 3.3 过滤设置

| 序列化字段 | 序列化值 | Inspector 可见 | 实际生效 | 说明 |
|---|---|---|---|---|
| `m_IgnoreNavMeshAgent` | true | ❌ 隐藏 | ✅ | 烘焙时排除挂有 NavMeshAgent 的物体。`CollectSources()` 中 `sources.RemoveAll(x => x.component.gameObject.GetComponent<NavMeshAgent>() != null)` |
| `m_IgnoreNavMeshObstacle` | true | ❌ 隐藏 | ✅ | 烘焙时排除挂有 NavMeshObstacle 的物体（如 DynamicBox），使其不被静态烘焙进 NavMesh，而由运行时动态雕刻处理 |

**不可见的原因**：`NavMeshSurfaceEditor.cs` 的 `OnInspectorGUI()` 从未调用 `EditorGUILayout.PropertyField()` 绘制这两个字段。源码默认值为 `true`，用户只能通过脚本修改。

### 3.4 高级设置（"Advanced" 折叠组内）

| 序列化字段 | 序列化值 | Inspector 可见 | 实际生效 | 说明 |
|---|---|---|---|---|
| `m_OverrideVoxelSize` | false | ✅ 复选框 | ✅ | 是否手动指定 VoxelSize。false = 自动从 agentRadius 计算 |
| `m_VoxelSize` | 0.16666667 | ✅ 但灰显 | ⚠️ 间接生效 | 体素大小。OverrideVoxelSize=false 时，`OnValidate()` 自动计算为 `agentRadius / 3 = 0.5 / 3 ≈ 0.16667` |
| `m_OverrideTileSize` | false | ✅ 复选框 | ✅ | 是否手动指定 TileSize。false = 使用默认值 256 |
| `m_TileSize` | 256 | ✅ 但灰显 | ⚠️ 间接生效 | 瓦片大小。OverrideTileSize=false 时，`OnValidate()` 自动设为 256 |
| `m_MinRegionArea` | 2 | ✅ 数值输入 | ✅ | 最小可行走区域面积。面积小于 2 的孤立碎片被丢弃 |
| `m_BuildHeightMesh` | false | ✅ 复选框 | ✅ | 是否构建高度网格。关闭，平面场景不需要精确高度采样 |

**VoxelSize 自动计算来源**（`NavMeshSurface.cs` `OnValidate()`）：
```csharp
if (!m_OverrideVoxelSize)
    m_VoxelSize = settings.agentRadius / 3.0f;  // 0.5 / 3 = 0.16667
```

### 3.5 烘焙产物

| 序列化字段 | 序列化值 | Inspector 可见 | 说明 |
|---|---|---|---|
| `m_NavMeshData` | guid: c462a7eb5951e5e42804f2067935493c | ✅ 只读显示 | 引用 `Assets/Scenes/迷你游戏/NavMesh-Ground.asset`。当前已存在，说明**已烘焙完成** |

---

## 四、NavMesh Project Settings 详解

来源：`ProjectSettings/NavMeshAreas.asset`

### 4.1 Agent 定义（仅一个，名为 "Humanoid"）

| 参数 | 值 | 说明 |
|---|---|---|
| `agentRadius` | 0.5 | Agent 的半径。决定 Agent 与障碍物的最小间距，以及可通过的通道最小宽度（需 > 2 × radius = 1.0）。烘焙时用于体素膨胀 |
| `agentHeight` | 2.0 | Agent 的高度。上方有低于此高度的空间不会被烘焙为可行走 |
| `agentSlope` | 45° | Agent 能行走的最大坡度。超过 45° 的斜面被排除 |
| `agentClimb` | 0.75 | Agent 能攀爬的最大台阶高度。≤0.75 的台阶视为可行走，>0.75 视为障碍 |
| `ledgeDropHeight` | 0 | 允许跳下的台阶高度。0 = 不允许跳下 |
| `maxJumpAcrossDistance` | 0 | 允许跳跃的水平距离。0 = 不允许跳跃 |
| `minRegionArea` | 2 | 全局最小区域面积，与 NavMeshSurface 的 m_MinRegionArea 同步 |
| `cellSize` | 0.16666667 | 全局体素大小，与 NavMeshSurface 的 m_VoxelSize 同步 |
| `tileSize` | 256 | 全局瓦片大小，与 NavMeshSurface 的 m_TileSize 同步 |

### 4.2 Area 类型定义

| Index | 名称 | Cost | 说明 |
|---|---|---|---|
| 0 | Walkable | 1 | 默认可行走区域 |
| 1 | Not Walkable | 1 | 不可行走（永久障碍） |
| 2 | Jump | 2 | 跳跃区域（cost 更高，Agent 尽量避免） |
| 3-31 | (未定义) | 1 | 可自定义区域类型 |

> **什么是 Area Cost？** 寻路算法（A*）计算路径时累加路径经过的每个多边形区域的 cost 值。cost 越低，Agent 越倾向走这条路。例如沼泽地可设为 cost=3 让 Agent 绕开。

---

## 五、烘焙流程详解

### 5.1 完整流程图

```
[1] 用户点击 Inspector 中的 "Bake" 按钮
         │
         ▼
[2] 读取 Agent 参数
    NavMesh.GetSettingsByID(m_AgentTypeID=0)
    → agentRadius=0.5, agentHeight=2, agentSlope=45°, agentClimb=0.75
         │
         ▼
[3] 收集几何体（CollectSources）
    m_CollectObjects == Children
    → NavMeshBuilder.CollectSources(transform, ...)
    → 从 Ground 自身 + 所有子物体收集几何体
    → 按 m_LayerMask (全选) 过滤
    → 按 m_UseGeometry (RenderMeshes) 从 MeshFilter 提取三角面
         │
         ▼
[4] 排除特定组件的物体
    m_IgnoreNavMeshAgent=true
    → 移除 sources 中挂有 NavMeshAgent 的物体
    m_IgnoreNavMeshObstacle=true
    → 移除 sources 中挂有 NavMeshObstacle 的物体（如 DynamicBox）
         │
         ▼
[5] 计算 bounds
    m_CollectObjects != Volume
    → surfaceBounds = CalculateWorldBounds(sources)
    → 从实际收集的几何体自动计算包围盒
    → (m_Size / m_Center 被忽略)
         │
         ▼
[6] 获取构建设置
    GetBuildSettings()
    → m_OverrideVoxelSize=false → voxelSize = agentRadius/3 = 0.16667
    → m_OverrideTileSize=false → tileSize = 256
    → minRegionArea = 2
         │
         ▼
[7] 体素化（Voxelization）
    将 3D 空间按 voxelSize=0.16667 划分为 3D 体素网格
    对每个三角面，找到它覆盖的体素并标记为"占据"
    形成 3D 占据栅格
         │
         ▼
[8] 生成可行走面
    用 agentRadius=0.5 对体素场做膨胀（Dilation），约 3 层体素
    用 agentHeight=2.0 做高度检测
    用 agentSlope=45° 过滤过陡斜面
    用 agentClimb=0.75 检测台阶
         │
         ▼
[9] 瓦片化与轮廓提取
    按 tileSize=256 体素/瓦片划分空间
    每瓦片独立做轮廓提取 → 简化 → 三角化
    瓦片间做边界拼接
         │
         ▼
[10] 区域过滤
    计算每个可行走连通区域的面积
    丢弃面积 < minRegionArea=2 的碎片
    标记 Area Type = Walkable (0)
         │
         ▼
[11] 输出 NavMeshData
    NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, ...)
    序列化为 NavMesh-Ground.asset
    m_NavMeshData 引用此文件
```

### 5.2 关键概念深入

#### 什么是 LayerMask？

Unity 中每个 GameObject 都有一个 Layer 编号（0-31）。LayerMask 是一个 32 位整数，每一位对应一个 Layer：

```
Bit 31 30 29 28 ... 3  2  1  0
     1  1  1  1      1  1  1  1   = 0xFFFFFFFF = -1 (有符号) = 全选

如果只想选 Layer 0 (Default)：
     0  0  0  0      0  0  0  1   = 0x00000001 = 1

如果只想选 Layer 0 和 Layer 2：
     0  0  0  0      0  0  1  1   = 0x00000005 = 5
```

本项目 `m_LayerMask.m_Bits = 4294967295`（`0xFFFFFFFF`）→ 所有层级都参与烘焙。

在 `CollectSources()` 中，`NavMeshBuilder.CollectSources()` 收集几何体时检查每个 GameObject 的 Layer，只有 LayerMask 对应位为 1 的物体才会被收集。

**用途**：可将不需要参与烘焙的物体（如装饰物、触发器、特效）放到单独的 Layer，在 LayerMask 中排除该 Layer。

#### 什么是体素化（Voxelization）？

体素化是将连续的 3D 几何（三角面）转换为离散的 3D 栅格（体素 = Voxel）的过程：

```
连续三角面                    离散体素

  ┌──────────┐               ┌──────────┐
  │╱        ╱│               │■■■■■■■■■■│
  ├──────────┤      →        │■■■■■■■■■■│
  │         │               │■■■■■■■■■■│
  └──────────┘               └──────────┘
   精确表面                    栅格化近似
```

**为什么需要体素化？**

1. **降维简化**：直接在三角面上做寻路计算极其复杂。体素化后可以将 3D 问题降维为 2D——取体素顶面的投影作为可行走面
2. **膨胀操作**：可以用 Agent 的半径对体素场做形态学膨胀（Dilation），确保 Agent 中心走在 NavMesh 边缘时身体不会穿入障碍物
3. **精度可控**：VoxelSize 越小，精度越高但烘焙更慢、NavMesh 数据更大

**本项目的体素化参数**：
- VoxelSize = 0.16667（世界单位）
- AgentRadius = 0.5 → 膨胀层数 ≈ 0.5 / 0.16667 ≈ 3 个体素

膨胀效果示意：
```
原始可行走区域          膨胀后（缩小了 agentRadius）
┌────────────────┐    ┌────────────────┐
│################│    │                │
│################│    │   ############  │
│################│    │   ############  │  ← 边缘被"吃掉"
│################│    │   ############  │     agentRadius 的宽度
└────────────────┘    └────────────────┘
```

这样 Agent 通道的最低通过宽度 = `2 × agentRadius = 1.0`。

#### 什么是瓦片化（Tiling）？

NavMesh 生成不会一次性处理整个空间，而是将空间切分为多个独立的瓦片（Tile）：

```
┌──────┬──────┬──────┐
│Tile 0│Tile 1│Tile 2│   每个瓦片 = tileSize × voxelSize
├──────┼──────┼──────┤   = 256 × 0.16667 ≈ 42.67 世界单位
│Tile 3│Tile 4│Tile 5│
└──────┴──────┴──────┘
```

- 每个瓦片独立做轮廓提取 → 简化 → 三角化
- 瓦片间在边界处拼接（stitching）
- **优势**：增量更新效率高。运行时 NavMeshObstacle 雕刻只需重新构建受影响的瓦片，而非整个 NavMesh

---

## 六、动态障碍系统 — NavMeshObstacle 详解

来源：`Assets/Prefabs/DynamicBox.prefab`

| 参数 | 值 | 说明 |
|---|---|---|
| `m_Shape` | 1 (Box) | 障碍形状。0 = Capsule, 1 = Box |
| `m_Extents` | (0.5, 0.5, 0.5) | 障碍包围盒的半尺寸（实际大小 = 1×1×1） |
| `m_MoveThreshold` | 0.1 | 位移阈值。位移超过此值时视为"移动中" |
| `m_Carve` | true | **雕刻模式**。开启后在 NavMesh 上"挖洞"，而非仅做避让 |
| `m_CarveOnlyStationary` | true | 仅在静止时雕刻。移动中的物体不雕刻 |
| `m_TimeToStationary` | 0.5 | 静止判定时间（秒）。速度低于阈值后等待 0.5 秒才视为"静止" |
| `m_Center` | (0, 0, 0) | 障碍中心 |

### Carve 工作时序

```
DynamicBox 移动中
    │
    ├─ 位移 > MoveThreshold(0.1) → 视为"移动中"
    ├─ CarveOnlyStationary=true → 不雕刻，NavMesh 保持完整
    ├─ Agent 正常从该区域寻路通过
    │
    ▼
DynamicBox 停止移动
    │
    ├─ 速度低于阈值
    ├─ 等待 TimeToStationary=0.5 秒
    ├─ 判定为"静止" → 触发 Carve
    ├─ NavMesh 在 DynamicBox 区域"挖洞"（标记为不可行走）
    ├─ NavMeshAgent 检测到路径被阻断 → 自动重新寻路
    │
    ▼
DynamicBox 再次移动
    │
    ├─ 位移 > MoveThreshold → 判定为"移动中"
    ├─ 撤销雕刻 → 恢复 NavMesh
    └─ Agent 恢复原始路径
```

### 为什么烘焙时要排除 NavMeshObstacle？

`m_IgnoreNavMeshObstacle = true` → `CollectSources()` 中：
```csharp
if (m_IgnoreNavMeshObstacle)
    sources.RemoveAll(x => x.component != null 
        && x.component.gameObject.GetComponent<NavMeshObstacle>() != null);
```

如果 DynamicBox 被静态烘焙进 NavMesh，它会成为永久障碍——即使 DynamicBox 被移走，该区域仍然不可行走。设置为 true 后，DynamicBox 的阻挡完全由运行时 NavMeshObstacle 动态处理。

---

## 七、运行时寻路

来源：`Assets/Scripts/EnemyMovement.cs`

```csharp
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent navMeshAgent;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player != null)
        {
            navMeshAgent.SetDestination(player.position);
        }
    }
}
```

### 运行时数据流

```
NavMesh-Ground.asset（编辑时烘焙的静态数据）
         │
         ▼
NavMeshSurface.OnEnable() → Register() + AddData()
         │
         ▼
NavMesh.AddNavMeshData() → 注册到全局 NavMesh 系统
         │
         ├─ ← NavMeshObstacle (DynamicBox) 运行时动态雕刻
         │
         ▼
EnemyMovement.Update() → navMeshAgent.SetDestination(player.position)
         │
         ▼
A* 寻路在 NavMesh 多边形图上搜索最优路径
         │
         ├─ 考虑 Area Cost（Walkable=1）
         ├─ 避开 NavMeshObstacle 雕刻的区域
         └─ 输出 waypoint 路径
         │
         ▼
NavMeshAgent 沿路径移动
```