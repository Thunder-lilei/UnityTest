# BiuBiu

> 本子项目位于 `UnityTest` 仓库的 **`BiuBiu/`** 子目录。Unity 项目根即 `BiuBiu/`（不是 `UnityTest/`），用 Unity 打开 `BiuBiu/` 目录即可。

2D 像素风俯视角**幸存者类街机动作游戏**：大地图波次刷怪、弹弓三段蓄力、每轮自动变强。当前为可玩原型，所有美术为**纯色几何灰盒**（无外部素材依赖），后续接入音频。

## 技术栈

- **引擎**：团结引擎 TuanjieEditor 1.9.3（内核 2022.3.62t11，C#/包体系兼容 Unity 生态）
- **渲染**：Built-in 管线，正交相机（Size=9），PPU=32
- **输入**：Unity Input System（支持运行时改键）
- **平台**：Windows PC

## 玩法特性

- **弹弓三段蓄力**：左键蓄力，<0.5s 白色 / ≥0.5s 黄色 / ≥1s 红色，对应不同弹丸伤害与击退
- **敌人三型**：近战（横扫，120° 大扇形，前摇 0.9s / 精英 1.1s）、远程（直线弹）、Boss（5:00 解锁，环形+直线弹）；近战攻击清晰的前摇蓄力窗口供翻滚闪避
- **波次成长**：每轮结束自动微增移速与攻击力
- **操作**：`WASD` 移动 · 左键蓄力射击 · 空格闪避翻滚 · `ESC` 暂停 · `F3` 开发者模式无敌
- **反馈（Game Feel）**：受击闪白、命中微后仰、击碎（同色像素碎片爆发）、镜头震屏/慢动作（hitstop）、尸体与血迹池上限、**玩家受击屏幕红边**、**发射后坐力（三档递进）**、**弹丸拖尾（三档差异，红档粗光带）**、**蓄力音效 + 闪避音 + 三档发射音**

## 目录结构

```
BiuBiu/
├── Assets/
│   ├── Resources/Data/Enemies/   # EnemyData ScriptableObject 实例（5 个敌人配置）
│   ├── Scenes/                   # Boot.unity（引导）+ Main.unity（战斗）
│   ├── Scripts/
│   │   ├── Core/                 # GameBootstrap / GameBalance / MapGenerator2D / RuntimeSceneBuilder / ObjectPool / 等
│   │   ├── Data/                 # EnemyData 等 ScriptableObject 定义
│   │   ├── Enemies/              # EnemyBase2D / EnemyBoss2D / EnemySpawner2D / Projectile2D
│   │   ├── Player/               # PlayerController / PlayerStats
│   │   ├── UI/                   # GameHud / PauseMenu / SettingsPanel / DeathPanel
│   │   ├── Weapons/              # SlingWeapon / PlayerProjectile
│   │   └── Drops/                # DropManager（血迹池）
│   ├── Settings/                 # PlayerControls.inputactions
│   └── Audio/                    # 音频资产（蓄力音 laser_charge.wav + 闪避 dodge.wav + 三档发射 laser_fire_white/yellow/red.wav；其余事件音待补充）
├── Docs/                         # 设计文档.md / 开发文档.md / 数值文档.md（中文决策唯一出处）
```

> 视觉全部运行时生成（灰盒 + 程序化纹理），无美术资产目录。

## 运行方式

1. 用团结引擎 / Unity 2022.3+ 打开本子项目目录 `UnityTest/BiuBiu/`（此为 Unity 项目根）
2. `File → Build Settings` 中场景顺序：`Boot`（索引 0）、`Main`（索引 1）
3. 点击 Play 即从 `Boot` 引导进入 `Main` 开始游戏

## 操作说明

| 按键 | 功能 |
|------|------|
| `WASD` | 移动 |
| 鼠标左键 | 蓄力射击（三段蓄力） |
| `空格` | 闪避翻滚 |
| `ESC` | 暂停菜单（含屏幕震动/慢动作/开发者无敌/改键设置） |
| `F3` | 开发者模式无敌切换 |

## 文档

`Docs/` 下三份中文策划文档是开发决策唯一出处（改设计/数值先改文档）：

- `设计文档.md` — 设计决策
- `开发文档.md` — 环境、目录、规范、里程碑
- `数值文档.md` — 全部游戏数值唯一出处

## 开发状态

- ✅ M0 技术验证（灰盒/池化/闪白/Spine 路线评估）
- ✅ M1/M2 核心循环（波次/敌人/UI/死亡战报/开发者模式）
- ✅ M3 系统扩展（Boss / 精英 / 近战远程差异化灰盒表现；玩家刚体物理碰撞挡墙；近战前摇预警窗口）
- 🚧 M4 元游戏与打磨（音频部分接入：蓄力/闪避/三档发射已接，其余事件音待补充；成就/统计/开始页待做）

## 许可证

未指定，仅供学习/演示用途。
