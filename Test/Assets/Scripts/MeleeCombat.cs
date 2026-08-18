using UnityEngine;
using Game.Audio;
using Game.Enemy;

namespace Game.Combat
{
    
    /// <summary>近战斩击：自动定时释放半圆形范围攻击，朝玩家面朝方向</summary>
    public class MeleeCombat : MonoBehaviour
    {
        [Header("斩击")]
        public GameObject slashPrefab;            // 刀光特效 Prefab
        public GameObject skill;                  // 技能父物体

        [Header("自动释放")]
        [Tooltip("斩击间隔（秒）")]
        public float slashInterval = 1.0f;
        [Tooltip("斩击伤害")]
        public float slashDamage = 2f;
        [Tooltip("斩击范围半径")]
        public float slashRadius = 4f;
        [Tooltip("最小斩击间隔（升级下限）")]
        public float minSlashInterval = 0.5f;

        private bool isPaused;
        private float slashTimer;

        void Update()
        {
            if (isPaused) return;

            slashTimer += Time.deltaTime;
            if (slashTimer >= slashInterval)
            {
                slashTimer = 0f;
                PerformSlash();
            }
        }

        /// <summary>执行斩击：检测前方半圆范围内敌人并造成伤害，播放刀光特效</summary>
        void PerformSlash()
        {
            Vector3 myPos = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            // 播放刀光特效：旋转 90° X 轴让特效平面平行于地面（环绕主角）
            if (slashPrefab != null)
            {
                Vector3 effectPos = myPos + Vector3.up * 0.1f;
                // LookRotation 朝前方 + 绕 X 轴旋转 90° 让特效水平展开
                Quaternion effectRot = Quaternion.LookRotation(forward) * Quaternion.Euler(90f, 0f, 0f);
                Instantiate(slashPrefab, effectPos, effectRot, skill.transform);
            }

            // 检测范围内所有敌人
            Collider[] hits = Physics.OverlapSphere(myPos, slashRadius);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.transform == transform) continue;

                // 只命中前方半圆内的敌人（dot >= 0 即夹角 ≤ 90°）
                Vector3 toEnemy = hit.transform.position - myPos;
                toEnemy.y = 0;
                if (toEnemy.sqrMagnitude < 0.001f) continue;
                toEnemy.Normalize();

                if (Vector3.Dot(forward, toEnemy) >= 0f)
                {
                    EnemyMovement enemy = hit.GetComponent<EnemyMovement>();
                    if (enemy != null)
                        enemy.TakeDamage(slashDamage);
                }
            }

            AudioManager.Instance?.PlaySlashAttack();
        }

        /// <summary>升级缩短斩击间隔（每次 -0.1s，下限 minSlashInterval）</summary>
        public void ReduceSlashInterval(float amount)
        {
            slashInterval = Mathf.Max(minSlashInterval, slashInterval - amount);
        }

        /// <summary>设置暂停状态（升级选择时调用）</summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
    }
}
