

## Codely Structured Memories

### User
- [2026-08-12 16:18:23] 用户偏好：不动手只教教程时给清晰步骤即可；操作类任务直接执行不需要反复确认。用户是 Unity 学习者，正在通过做一个迷你游戏项目学习 Unity 开发。
### Feedback
- [2026-08-12 16:19:09] FBX 动画导入设置 lastFrame 必须用帧数（秒 × frameRate），不能直接用秒数。之前误将 lastFrame 设为秒值（如 1.667）导致所有动画片段被截断到 ~0.056s。Quaternius 模型 frameRate=30fps。**Why:** lastFrame 在 ModelImporterClipAnimation 中是帧号而非时间。**How to apply:** 通过脚本批量设置 FBX clip loopTime 时，lastFrame = clip.length * clip.frameRate。
- [2026-08-12 17:05:21] Prefab Variant 的 SkinnedMeshRenderer.bones[] 数组在编辑器中显示正确但运行时实例化后全部变为 NULL（序列化缺陷）。**Why:** EnemyFast 是 FBX 的 Prefab Variant，bones[] 引用 FBX 内部 Transform，CopyAsset 或 LoadPrefabContents→SaveAsPrefabAsset 均会破坏引用链，导致运行时 bones 全为 NULL → bounds 为零 → 视锥剔除使怪物不可见（但有碰撞体仍能攻击/被命中）。**How to apply:** 不能通过复制或重建 Variant Prefab 来修复。最终方案：Spawner 的 enemyPrefabs[1] 直接引用原始模型 prefab（与 [0] 相同），运行时按 typeIndex 设置差异属性（scale/speed）。不再使用独立的 EnemyFast.prefab。


### Project
- [2026-08-12 16:18:23] 项目从 C:\AIProject\ 搬迁到 C:\Learn\，manifest.json 中 cn.tuanjie.ai.generators 的 file 引用路径需同步更新，否则控制台报包依赖错误。已于 2026-08-12 修复。
- [2026-08-12 16:18:23] 项目美术风格已确认并写入 DESIGN_DOC.md 6.2 节：低多边形卡通风格（Low-poly Stylized）。选材原则：优先 Kenney/KayKit/Quaternius 的 CC0 低多边形资产，排除 Polyhaven 写实素材。
- [2026-08-12 16:18:23] 火球特效已从原版 FireBall.vfx 替换为增强版 FireBallEnhanced.vfx（自定义火焰贴图 FireParticle.png，80粒子/秒，5段颜色渐变，尺寸先膨胀后收缩曲线）。FireBall.prefab 的 VisualEffect 引用已更新。旧版 FireBall.vfx 保留未删。
- [2026-08-12 17:06:49] 场景自然素材已重新布局：138 个实例统一放在 NatureDecoration 父对象下。布局策略：围墙外半径 12-30 密集森林环带（46棵树）+ 35-60 远景稀疏树林（22棵）+ 14-45 散布岩石（18个）+ 13-35 灌木藤蔓（16个）+ 26-55 地形装饰（10个山丘/自然地块）+ 围墙内侧边缘装饰（10个）+ 四面围墙栅栏（16个）。Ground 320×320 大平面，材质为纯色草绿 URP Lit（Assets/Materials/GroundGrass.mat）。


- [2026-08-12 16:19:01] 怪物动画系统已修复：EnemyMovement.cs 在 Update() 中通过 animator.SetFloat("Speed", navMeshAgent.velocity.magnitude) 驱动动画。三个 Animator Controller（basic/chubby/ribcage）均添加了 Speed(float) 参数和 Idle↔Crawl 过渡条件（Speed>0.1 进入 Crawl，<0.1 回到 Idle）。FBX 动画循环已在 .meta clipAnimations 中设置 loopTime=true（Crawl/Idle/Walk/Run）。
- [2026-08-12 16:19:05] EnemyTank 和 EnemyBoss 模型已从旧版 zombie_arm（无 Crawl 动画）替换为 Quaternius Zombie Apocalypse Kit 同系列模型：Tank→Zombie_Chubby，Boss→Zombie_Ribcage。旧 zombie_arm FBX/controller/prefab 已删除。关键：替换模型时必须将子对象直接展开到 Prefab root 下（Instantiate 后 reparent），不能嵌套整个 model prefab，否则动画 clip 骨骼路径不匹配导致动画不播放。
- [2026-08-12 17:05:25] 火球 bug 已修复：FireBall.OnTriggerEnter 原来在 if(Enemy) 块外执行 PlayFireballHit + Despawn，导致火球碰到 MagnetDetector（玩家子物体，半径3m触发器）即被回收。修复：忽略非敌人的触发器（isTrigger && !Enemy）和 Player 层。另外 FireBall.OnSpawn() 增加 vfx.Reinit()+Play() 修复对象池复用时 VFX 不重启问题。所有敌人 SMR 设为 updateWhenOffscreen=True。隐形怪物 bug 根因有二：①场景中残留一个 EnemyFast(Clone) 对象（bones[] 全 NULL），已删除；②EnemyFast.prefab 的 Variant bones[] 运行时丢失，已改为 Spawner 直接引用原始模型 prefab + 运行时设置差异属性。


### Reference

