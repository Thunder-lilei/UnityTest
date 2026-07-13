# UnityTest

基于 [Roll-a-Ball](https://learn.unity.com/project/roll-a-ball) 教程的 Unity 学习项目，在官方教程基础上扩展了 AI 生成角色、敌人追逐、脚印系统和游戏结束功能。

## 环境要求

- **引擎**：团结引擎 2022.3.62t11（兼容 Unity 2022.3 LTS）
- **渲染管线**：Built-in Render Pipeline
- **输入系统**：经典 Input Manager

## 项目结构

```
Test/
├── Assets/
│   ├── Characters/             # 角色 Prefab（AI 生成）
│   ├── Materials/              # 材质（Player/Enemy/PickUp/Wall/Background/Footprint）
│   ├── Prefabs/                # 预制体（PickUp/DynamicBox/Quad）
│   ├── Scenes/
│   │   └── 迷你游戏.scene       # 主关卡（含 NavMesh 烘焙数据）
│   ├── Scripts/                # C# 脚本
│   │   ├── PlayerController.cs # 玩家控制、动画驱动、脚印、游戏结束
│   │   ├── CameraController.cs # 摄像机跟随
│   │   ├── EnemyMovement.cs    # 敌人 NavMesh 追逐
│   │   ├── Footprint.cs        # 脚印渐隐消失
│   │   └── Rotator.cs          # 收集物旋转动画
│   └── TJGenerators/           # AI 生成资源缓存
├── Packages/
└── ProjectSettings/
```

## 游戏玩法

- **WASD / 方向键**：控制角色移动（恒定速度，非物理力驱动）
- 角色自动朝向移动方向，行走时留下渐隐脚印
- 收集场景中的 4 个旋转方块即可获胜
- 躲避敌人追逐，被碰到则失败
- 胜利或失败后弹出面板，可选择**重新开始**或**退出游戏**

## 扩展功能（相对原版教程）

| 功能 | 说明 |
|------|------|
| AI 生成角色 | 通过 Meshy AI + TJGenerators 插件生成 Humanoid 角色（含 Idle/Walk/Run 动画） |
| Animator 状态机 | Speed 参数驱动 Idle ↔ Walk ↔ Run 过渡，Action 触发特殊动作 |
| 恒定速度移动 | 使用 rb.velocity 替代 AddForce，避免加速感 |
| 朝向移动方向 | Quaternion.Slerp 平滑转向 |
| 脚印系统 | 移动时左右交替生成脚印，3 秒渐隐消失 |
| 敌人追逐 | 使用 NavMesh 实现敌人自动寻路追踪玩家 |
| 游戏结束面板 | 胜利/失败时弹出 UI 面板，暂停游戏（Time.timeScale = 0） |
| 重新开始 | 通过 SceneManager 重新加载当前场景 |
| 退出游戏 | Application.Quit() |
| 中文支持 | TextMeshPro 使用微软雅黑字体资产 |

## 版本差异说明

本项目基于团结引擎 2022.3 开发，与教程使用的 Unity 6.3 存在以下差异：

| 差异项 | 教程（Unity 6.3） | 本项目（团结 2022.3） |
|--------|-------------------|----------------------|
| 输入系统 | 新版 Input System（OnMove 回调） | 经典 Input Manager（Input.GetAxis） |
| 材质属性 | Base Map | Albedo |
| NavMeshSurface | 预装 AI Navigation 包 | 需手动安装 |
| TMP 中文 | - | 需生成中文字体资产 |

## 如何运行

1. 用团结 Hub（或 Unity 2022.3 LTS）打开 `Test/` 目录
2. 打开 `Assets/Scenes/迷你游戏.scene`
3. 点击 Play 运行

## 许可证

本项目仅供学习用途。
