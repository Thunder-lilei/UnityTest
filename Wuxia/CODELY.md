

## Codely Structured Memories

### User
- [2026-08-12 20:17:03] 用户正在通过 Wuxia 项目学习 Unity 开发（C# · Lua · VFX Graph · 动作游戏开发），是 Unity 初学者。希望理解每个决策背后的原因而非仅得到结果，需要技术选型对比和手动操作步骤记录。

### Feedback
- [2026-08-12 19:16:38] 所有技术选型必须记录：具体操作步骤 + 选择原因 + 同类可选项对比及为何不选。适用于整个项目所有阶段。
- [2026-08-12 19:16:38] AI 直接创建/生成的所有内容，执行步骤中必须记录"如果手动操作该怎么做"，确保脱离 AI 也能独立完成。Why: 用户希望通过项目学习 Unity 开发，不能只靠 AI 生成。How to apply: 每次自动创建脚本、配置、资产后，在同一步骤中补充手动操作路径。
- [2026-08-12 19:34:33] 下载外部资产时按需逐个下载，不要一次下载整个大包再筛选。Why: 用户认为下载大包后还需额外花精力筛选，需求缺口不大。How to apply: 搜索外部资产时优先推荐小型/单个资产，按具体需求匹配。
- [2026-08-12 21:17:08] 资产搜索下载后必须整理到设计文档规定的目录结构（Assets/Model/ 放模型素材含FBX和动画、Assets/Prefabs/Environment/ 放环境Prefab、Assets/Prefabs/Player/ 放角色Prefab、Assets/Prefabs/VFX/ 放特效Prefab），不要保留 TJGenerators/DownloadedAssets 等默认下载路径。Why: 用户要求严格遵循预设目录结构，不额外创建新目录。How to apply: 每次批量下载素材后，立即用 AssetDatabase.MoveAsset 整理到正确目录并删除临时目录。

- [2026-08-12 20:59:20] 计划文件不需要「执行记录」部分。Why: Git 已提供准确的变更历史，执行记录是重复 git 的职责，且对学习和复现无价值，增加文档噪音。How to apply: 所有计划文件只保留：目标、任务拆解、涉及脚本/资源、验收标准。
- [2026-08-12 21:09:53] 文档中描述执行步骤或属性数值时，关键/特殊的设置点必须额外解释「为何如此设置」，不能只写操作指令。Why: 项目核心目标是学习理解而非单纯完成，过程比结果重要。How to apply: 每个关键配置值、非默认参数、特殊设计选择，都在旁边补充原因说明。

### Project
- [2026-08-12 19:34:33] 项目美术风格为「国风武侠偏写实」，VFX 特效选型以此为风格基准。
- [2026-08-12 20:20:50] P7 场景素材储备进展（截至 2026-08-12）：已从资产库下载 14 个 Prefab（竹子x2、山石x3、石板x2、建筑x2、木桥x1、松树x1、灯笼x2、石阶x1），另有 3 个待补下载（亭子barracks、城门walltowers、木栅栏woodenfence）。全部为 Low-poly 风格，用户已确认可用。Polyhaven 巨石 Prefab 引用断裂需修复。用户决定先试用免费资产库素材，不使用 AI 生成。
- [2026-08-12 20:54:05] VFX 特效资源存放在 Assets/Prefabs/VFX/（非 Art 目录）。已导入 Kenney CC0 粒子纹理包（80张，含 slash/spark/smoke/dirt/trace 等）和 4 个示例 Prefab（Electricity、Fire、Smoke、Sparks）。Hearts 和 Magic 已按用户要求删除（风格不匹配）。
- [2026-08-12 20:59:20] 游戏视角为俯视角（top-down 3D action game），非第三人称。影响角色控制器选型（Rigidbody 而非 CharacterController）、摄像机设计（固定角度跟随位置不旋转）、Animator 方案（1D Blend Tree 足够，不需要方向混合）等决策。

### Reference
- [2026-08-12 20:59:20] 免费 AI 3D 模型生成工具备选（P7 场景素材备选方案）：TRELLIS 2（完全免费开源，需自有GPU）、Hunyuan3D 2.0（腾讯开源，中文友好）、Tripo AI、Luma AI Genie、CSM AI、Sudo AI（每月40 Credits）。用户倾向先试免费外部工具，Codely 内置 generate_3d_model 作为备选。注意：Meshy AI 已于 2026-08-12 确认下架，不可用。

