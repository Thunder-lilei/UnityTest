

## Codely Structured Memories

### User
- [2026-08-12 16:18:23] 用户偏好：不动手只教教程时给清晰步骤即可；操作类任务直接执行不需要反复确认。用户是 Unity 学习者，正在通过做一个迷你游戏项目学习 Unity 开发。
### Feedback
- [2026-08-12 16:19:09] FBX 动画导入设置 lastFrame 必须用帧数（秒 × frameRate），不能直接用秒数。之前误将 lastFrame 设为秒值（如 1.667）导致所有动画片段被截断到 ~0.056s。Quaternius 模型 frameRate=30fps。**Why:** lastFrame 在 ModelImporterClipAnimation 中是帧号而非时间。**How to apply:** 通过脚本批量设置 FBX clip loopTime 时，lastFrame = clip.length * clip.frameRate。
- [2026-08-12 17:05:21] Prefab Variant 的 SkinnedMeshRenderer.bones[] 数组在编辑器中显示正确但运行时实例化后全部变为 NULL（序列化缺陷）。**Why:** EnemyFast 是 FBX 的 Prefab Variant，bones[] 引用 FBX 内部 Transform，CopyAsset 或 LoadPrefabContents→SaveAsPrefabAsset 均会破坏引用链，导致运行时 bones 全为 NULL → bounds 为零 → 视锥剔除使怪物不可见（但有碰撞体仍能攻击/被命中）。**How to apply:** 不能通过复制或重建 Variant Prefab 来修复。最终方案：Spawner 的 enemyPrefabs[1] 直接引用原始模型 prefab（与 [0] 相同），运行时按 typeIndex 设置差异属性（scale/speed）。不再使用独立的 EnemyFast.prefab。
- [2026-08-13 14:53:45] Humanoid 动画模型必须通过 PrefabUtility.InstantiatePrefab 加载到场景中，不能用 Object.Instantiate 克隆 FBX。**Why:** Object.Instantiate 会破坏 Avatar 的骨骼映射链（GetBoneTransform 返回 NULL），导致角色运行时显示 T-pose。PrefabUtility.InstantiatePrefab 保留 FBX Prefab 引用链，Avatar 映射正常。**How to apply:** 需要将 FBX 模型放入场景时，始终用 PrefabUtility.InstantiatePrefab；PlayerController 中 Animator 获取改为 GetComponentInChildren<Animator>()（Animator 在模型子对象上）。
- [2026-08-13 15:13:01] 运行时反射修改的 private 字段值（如 HealthBar.maxHealth/currentHealth）会被 Unity 序列化到场景文件中，退出 Play Mode 后仍保留。**Why:** 之前用反射设 maxHealth=99999 测试，结果场景保存了该值，且 currentHealth=0 被保存导致后续进游戏秒死触发 Time.timeScale=0 冻结动画。**How to apply:** 不要在运行时通过反射修改序列化字段做测试；如需提升血量测试，应在 Play Mode 前编辑场景或在代码中临时修改默认值，测试后恢复。

### Project
- [2026-08-12 16:18:23] 项目从 C:\AIProject\ 搬迁到 C:\Learn\，manifest.json 中 cn.tuanjie.ai.generators 的 file 引用路径需同步更新，否则控制台报包依赖错误。已于 2026-08-12 修复。
- [2026-08-13 15:13:03] 项目美术风格已确认并写入设计文档.md 6.3 节：低多边形卡通风格（Low-poly Stylized）。选材原则：优先 Kenney/KayKit/Quaternius 的 CC0 低多边形资产，排除 Polyhaven 写实素材。

- [2026-08-12 21:38:22] 火球特效已从原版 FireBall.vfx 替换为增强版 FireBallEnhanced.vfx（自定义火焰贴图 FireParticle.png，80粒子/秒，5段颜色渐变，尺寸先膨胀后收缩曲线）。FireBall.prefab 的 VisualEffect 引用已更新。旧版 FireBall.vfx 已于 2026-08-12 资产清理时删除。

- [2026-08-12 17:06:49] 场景自然素材已重新布局：138 个实例统一放在 NatureDecoration 父对象下。布局策略：围墙外半径 12-30 密集森林环带（46棵树）+ 35-60 远景稀疏树林（22棵）+ 14-45 散布岩石（18个）+ 13-35 灌木藤蔓（16个）+ 26-55 地形装饰（10个山丘/自然地块）+ 围墙内侧边缘装饰（10个）+ 四面围墙栅栏（16个）。Ground 320×320 大平面，材质为纯色草绿 URP Lit（Assets/Materials/GroundGrass.mat）。


- [2026-08-12 16:19:01] 怪物动画系统已修复：EnemyMovement.cs 在 Update() 中通过 animator.SetFloat("Speed", navMeshAgent.velocity.magnitude) 驱动动画。三个 Animator Controller（basic/chubby/ribcage）均添加了 Speed(float) 参数和 Idle↔Crawl 过渡条件（Speed>0.1 进入 Crawl，<0.1 回到 Idle）。FBX 动画循环已在 .meta clipAnimations 中设置 loopTime=true（Crawl/Idle/Walk/Run）。
- [2026-08-12 16:19:05] EnemyTank 和 EnemyBoss 模型已从旧版 zombie_arm（无 Crawl 动画）替换为 Quaternius Zombie Apocalypse Kit 同系列模型：Tank→Zombie_Chubby，Boss→Zombie_Ribcage。旧 zombie_arm FBX/controller/prefab 已删除。关键：替换模型时必须将子对象直接展开到 Prefab root 下（Instantiate 后 reparent），不能嵌套整个 model prefab，否则动画 clip 骨骼路径不匹配导致动画不播放。
- [2026-08-12 17:05:25] 火球 bug 已修复：FireBall.OnTriggerEnter 原来在 if(Enemy) 块外执行 PlayFireballHit + Despawn，导致火球碰到 MagnetDetector（玩家子物体，半径3m触发器）即被回收。修复：忽略非敌人的触发器（isTrigger && !Enemy）和 Player 层。另外 FireBall.OnSpawn() 增加 vfx.Reinit()+Play() 修复对象池复用时 VFX 不重启问题。所有敌人 SMR 设为 updateWhenOffscreen=True。隐形怪物 bug 根因有二：①场景中残留一个 EnemyFast(Clone) 对象（bones[] 全 NULL），已删除；②EnemyFast.prefab 的 Variant bones[] 运行时丢失，已改为 Spawner 直接引用原始模型 prefab + 运行时设置差异属性。
- [2026-08-13 14:54:05] 场景中曾存在两个同名 Player 根对象（一个有 PlayerController 无模型，一个有 Y Bot 模型无 PlayerController），导致：①火球双发（两个 PlayerController 同时响应鼠标点击）；②角色环绕初始位置运动（PlayerController 操作空对象 Rigidbody，模型不受控）。已合并为单个 Player 对象，YBot 作为子对象（Animator 在 YBot 上）。另外 GameOverPanel 曾残留在激活状态 + currentHealth=0 导致进游戏秒死，已修复。
- [2026-08-13 15:12:56] 主角模型已替换为标准 Mixamo Y Bot（灰色人偶）。资源从 C:\Learn\UnityTest\Wuxia\Assets\Model\Characters 复制到 Assets/Models/YBot/（Y Bot.fbx + Idle/Walking/Running/Jumping.fbx）。Animator Controller 为新建的 Assets/Animations/PlayerController.controller（Idle↔Walk↔Run，Speed 参数驱动）。PlayerController.cs 的 Start() 中 Animator 获取改为 GetComponentInChildren<Animator>()。YBot 通过 PrefabUtility.InstantiatePrefab 加载为 Player 子对象，Animator 在 YBot 上。
- [2026-08-13 16:04:56] 敌人死亡溶解特效已完成：手写 URP HLSL Shader（Dissolve.shader，ShaderLab+HLSL，AlphaToMask+噪声+边缘发光）+ DissolveMat.mat + DissolveEffect.cs 脚本（运行时替换材质+动画DissolveAmount 0→1）。三个敌人 Prefab（Zombie_Basic/EnemyTank/EnemyBoss）已添加 DissolveEffect 组件并关联材质。EnemyMovement.cs Die() 方法已集成溶解逻辑。教程文档：溶解特效制作教程.md。


### Reference

