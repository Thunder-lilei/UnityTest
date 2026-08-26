# BiuBiu

> 本子项目位于 `UnityTest` 仓库的 **`BiuBiu/`** 子目录。Unity 项目根即 `BiuBiu/`（不是 `UnityTest/`），用 Unity 打开 `BiuBiu/` 目录即可。

2D 像素风俯视角**幸存者类街机动作游戏**：大地图波次刷怪、弹弓三段蓄力、每轮自动变强。当前为可玩原型，所有美术为**纯色几何灰盒**（无外部素材依赖），音频已由 `AudioManager` 接入（`Resources/Audio/` 下素材驱动：蓄力/三档发射/闪避/受击/击碎/死亡/撞墙/碎砖等事件音）。

## 技术栈

- **引擎**：团结引擎 TuanjieEditor 1.9.3（内核 2022.3.62t11，C#/包体系兼容 Unity 生态）
- **渲染**：Built-in 管线，正交相机（Size=9），PPU=32
- **输入**：Unity Input System（支持运行时改键）
- **平台**：Windows PC

## 玩法特性

- **弹弓三段蓄力**：左键蓄力，<0.5s 白色(速射) / ≥0.5s 黄色(击飞·2 伤) / ≥1s 红色(击碎秒杀普通敌人·对精英Boss 4 伤)，三档伤害与反馈层级分明
- **敌人三型**：近战（横扫，120° 大扇形，前摇 0.9s）、远程（直线弹）、Boss（第 5、10 轮登场，八方向扇形横扫 240°·r4 + 直线冲撞 + **冲撞连击（随机 0~2 追加、重新锁定）** + **二阶段狂暴（血量 <50%：提速/冷却缩短/强制连冲）**，金色，体型 4×近战）；精英（第 3 轮起每轮 +1，紫色，体型 2×近战，**八方向投掷**：朝玩家 1 发 + 米字 7 发，投掷距离 = 普通远程 ×2）。所有攻击均有**红色半透明填充预警**（扇形/冲撞矩形带）供预判闪避
- **波次成长**：每轮结束自动微增移速与攻击力
- **可破坏掩体**：地图散布俄罗斯方块（Tetromino）形状障碍——**满蓄力弹丸命中可击碎**（同色碎片迸发），非满蓄力撞墙迸发火花 + 轻脆音；边界墙不可破坏，仍为满蓄力反弹面
- **操作**：`WASD` 移动 · 左键蓄力射击 · 空格闪避翻滚 · `ESC` 暂停 · `F3` 开发者模式无敌
- **反馈（Game Feel）**：受击闪白、命中微后仰、击碎（同色像素碎片爆发）、镜头震屏（受击/击杀 trauma）、尸体与血迹池上限、**玩家受击屏幕红边**、**发射后坐力（三档递进）**、**弹丸拖尾（三档差异，红档粗光带）**、**蓄力音效 + 闪避音 + 三档发射音**、**撞墙火花/碎墙反馈**、**满蓄力镜头抖动 + 过载（超 2s 强制脱手飞偏）**、**满蓄超 1s 玩家冒泡"要憋不住了！"**、**屏幕外敌人方位指示（边缘三角箭头，颜色随敌、远淡近实）**、**翻滚冷却径向环 HUD（左下角）**、**翻滚残影 + 无敌帧**、**蓄力拉拽位移**、**击杀/受击方向性反馈**

## 目录结构

```
BiuBiu/
├── Assets/
│   ├── Resources/Data/Enemies/   # EnemyData ScriptableObject 实例（4 个敌人配置：远程/近战横扫/精英/Boss）
│   ├── Scenes/                   # Boot.unity（引导）+ Main.unity（战斗）
│   ├── Scripts/
│   │   ├── Core/                 # GameBootstrap / GameBalance / GameState / MapGenerator2D / RuntimeSceneBuilder / ObjectPool / AudioManager / CameraFollow+Trauma / GreyBoxFactory / HitFlash / DeveloperMode / SaveSystem / RunStats / IDamageable+IKnockbackable
│   │   ├── Data/                 # EnemyData 等 ScriptableObject 定义
│   │   ├── Enemies/              # EnemyBase2D / EnemyBoss2D / EnemySpawner2D / Projectile2D
│   │   ├── Player/               # PlayerController / PlayerStats
│   │   ├── UI/                   # GameHud / PauseMenu / SettingsPanel / TitleCard（开场卡）/ TitleScreen（死亡结尾卡）/ SpeechBubbleManager（气泡）/ HurtVignette（受击红边）
│   │   ├── Weapons/              # SlingWeapon / PlayerProjectile
│   │   ├── Effects/              # BreakBurstManager / BreakShard（击碎同色碎片特效，Sprite 手搓伪粒子）
│   │   └── Drops/                # DropManager（血迹池）
│   ├── Settings/                 # PlayerControls.inputactions
│   └── Resources/Audio/          # 音频资产（AudioManager 运行时加载）：laser_charge/laser_fire_white·yellow·red/dodge/player_hurt/enemy_hit/enemy_shatter/enemy_death/stone_break/wall_hit（wall_hit 素材待补）
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
| `ESC` | 暂停菜单（含屏幕震动/慢动作演出/开发者无敌设置及统计记录、退出游戏） |
| `F3` | 开发者模式无敌切换 |
| 任意键 / 鼠标 | 首次启动的电影风格开场卡（「怎么才算好玩？」——李雷）确认开始；死亡后显示结尾标题卡（「在哪跌倒就在哪躺会儿 ——李雷」）按任意键/点击重开 |

## 文档

`Docs/` 下四份中文文档是开发决策唯一出处（改设计/数值先改文档）：

- `设计文档.md` — 设计决策
- `开发文档.md` — 环境、目录、规范、里程碑
- `数值文档.md` — 全部游戏数值唯一出处
- `技术文档.md` — 核心系统技术实现详解

## 开发状态

- ✅ M0 技术验证（灰盒/池化/闪白/Spine 路线评估）
- ✅ M1/M2 核心循环（波次/敌人/UI/死亡战报/开发者模式）
- ✅ M3 系统扩展（Boss / 精英 / 近战远程差异化灰盒表现；玩家刚体物理碰撞挡墙；近战前摇预警窗口）
- ✅ M3.x 战斗节奏重构（精英/Boss 改按轮次登场：第 3 轮起每轮 +1 精英、第 5/10 轮各 1 Boss；第 8 轮起数量封顶转血量/精英占比；精英八方向投掷、Boss 八方向扇形横扫+冲撞；HUD 右上角剩余敌人计数）
- 🚧 M4 元游戏与打磨
  - ✅ 战斗反馈打磨冲刺（本轮）：满蓄力镜头抖动 + 过载脱手飞偏；满蓄超 1s 玩家冒泡"要憋不住了！"；屏幕外敌人方位指示（边缘三角·颜色随敌·远淡近实）；翻滚冷却 2.0s + 左下角径向冷却环 HUD；三色伤害拉开区分（白1/黄2击飞/红秒杀·对精英Boss4）；玩家移速 4.0→3.4；蓄力拉拽位移与 velocity 统一写入（根治蓄力相关漂移）；普通敌人配色绿→浅蓝（解决远处绿箭头与绿地板混淆）；修复敌人蓄力被击退扇形框残留 bug
  - ✅ 暂停菜单 UI 修复：统计/设置子面板模态层级（由 PauseMenu 统一调度绘制、层级在主面板之上）、ESC 退出时子面板一并关闭、事件穿透（点「返回」误触主菜单）修复；设置面板移除改键入口；屏幕震动关闭即时停震 + 慢动作演出接线到死亡慢动作、满蓄力抖动受震动开关控制
  - ✅ 电影风格开场卡（开始页骨架）：首次启动播纯黑底 + 居中两行台词「怎么才算好玩？」/「——李雷」，按任意键开始；5s 未操作右下角呼吸提醒「按任意键开始」；仅首次启动播，回标题重进不重播（IMGUI，无 Canvas/后处理）
  - ✅ Boss 压迫感增强（v3.9）：冲撞连击（随机追加 0~2 发、重新锁定玩家）+ 二阶段狂暴（血量 <50%：移速×1.5/冲撞冷却×0.6/强制连冲）；技能间隔缩短（横扫后慢走 3→1.5s、冲撞冷却 6→2.5s、前摇 0.6→0.45s）；攻击预警统一**红色半透明填充+红边框**（八扇形填充 + 冲撞矩形带 18×4）
  - ✅ 死亡流程改版（v3.9）：玩家死亡尸体倒地保留（侧躺 −90°+变灰半透明）；死亡演出结束直接播结尾标题卡「在哪跌倒就在哪躺会儿 ——李雷」，按任意键/点击重开——战报面板退役（数据仍照常结算存档，ESC 统计记录可查）；开场卡提醒延迟 5s→3s
  - ✅ 跨局 bug 根治（v3.9）：受击闪白方形修复（SpriteFlash shader 预乘 alpha，圆形 sprite 不再显方）；重开后气泡底色透明修复（运行时纹理/材质统一 `HideAndDontSave`）；**重开后上一局弹丸残留根治**（内容级模板与对象池根去除 DDOL 留活动场景，随 `LoadScene` 自动清理）；**重开无法移动根治**（`SlingWeapon` OnDisable 复位 static 蓄力状态、`StartRun` 复位 GameState/timeScale）
  - ✅ Build 闪白失效修复（v0.29）：SpriteFlash shader 仅被 `Shader.Find()` 字符串引用，Build 时被裁剪——加入 `Graphics Settings → Always Included Shaders` 修复
  - ✅ 暂停菜单新增「退出游戏」按钮（编辑器退出 Playmode，打包退出进程）
  - 🔲 成就系统等元游戏待做（开始页已落地为电影开场卡）

## 许可证

未指定，仅供学习/演示用途。
