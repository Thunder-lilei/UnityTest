# 工作记忆

## 音频系统进度（截至 2026-08-25）
项目 BiuBiu 的音频接入工作记录：

### 已接入音频（Assets/Resources/Audio/，共 11 个素材 / 10 调用点）
- 武器：`laser_charge`(蓄力, PlayLoop, 音量2/3, 不循环, 满蓄力停) / `laser_fire_white|yellow|red`(三档发射, SlingWeapon.Fire 按 level 选)
- 玩家：`dodge`(翻滚起手) / `player_hurt`(真实扣血, PlayerController.TakeDamage)
- 敌人：`enemy_hit`(受伤未死) / `enemy_shatter`(红档击碎, 与 enemy_death 互斥) / `enemy_death`(普通/精英/Boss死亡)
- 环境互动：`wall_hit`(弹丸撞墙, 音量0.5) / `stone_break`(可破坏物)

### 音频架构约定
- AudioManager：Play(name, volumeScale=1) 一次性; PlayLoop(name) 持续可中断(StopLoop); 持续音轨独立 _loopSource
- 素材统一放 Assets/Resources/Audio/<name>.wav，用户常给 Assets/Audio/ 需移动到 Resources/Audio
- 蓄力音用 PlayLoop + StopLoop（松手/中断终止）；发射音在 CancelCharge 后接续

### 待补充（P1/P2）
- P1：敌人攻击前摇警示音、精英登场、Boss登场/弹幕/终结、轮次全灭成长 round_clear
- P2：UI点击/悬停/暂停、成就toast、战报弹出、环境氛围+菜单BGM
