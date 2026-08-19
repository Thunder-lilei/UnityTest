using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Game.Audio;
using Game.Enemy;

namespace Game.Combat
{

    /// <summary>升级冲击波：升级选择确认时触发，对周围敌人造成伤害并击退，伴随扩散视觉特效</summary>
    public class ShockwaveEffect : MonoBehaviour
    {
        [Header("冲击波参数")]
        [Tooltip("冲击波伤害")]
        public float damage = 3f;

        [Tooltip("冲击波影响半径")]
        public float radius = 6f;

        [Tooltip("击退距离")]
        public float knockbackDistance = 3f;

        [Tooltip("击退持续时间（秒）")]
        public float knockbackDuration = 0.3f;

        [Tooltip("冲击波扩散动画持续时间（秒）")]
        public float expandDuration = 1f;

        [Tooltip("升级时增加的伤害值")]
        public float damageUpgradeStep = 2f;

        [Tooltip("升级时增加的半径")]
        public float radiusUpgradeStep = 1f;

        [Header("视觉特效")]
        [Tooltip("冲击波特效 Prefab（可选，无则仅逻辑伤害）")]
        public GameObject shockwavePrefab;

        [Tooltip("特效父物体")]
        public Transform effectParent;

        private bool isPaused;

        /// <summary>触发冲击波：范围内敌人受伤 + 击退 + 播放扩散特效</summary>
        public void Trigger()
        {
            // 播放扩散特效
            if (shockwavePrefab != null)
            {
                GameObject fx = Instantiate(shockwavePrefab, transform.position, Quaternion.identity, effectParent);
                fx.SetActive(true);
                var fxController = fx.GetComponent<ShockwaveVFX>();
                if (fxController != null)
                    fxController.Play(radius, expandDuration);
                else
                    Destroy(fx, expandDuration + 0.1f);
            }

            // 对范围内敌人造成伤害 + 击退
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.transform == transform) continue;

                EnemyMovement enemy = hit.GetComponent<EnemyMovement>();
                if (enemy != null)
                    enemy.TakeDamage(damage);

                // 击退（通过 NavMeshAgent 位移，兼容无 Rigidbody 的敌人）
                NavMeshAgent agent = hit.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    Vector3 knockbackDir = hit.transform.position - transform.position;
                    knockbackDir.y = 0;
                    knockbackDir.Normalize();
                    StartCoroutine(KnockbackAgent(agent, knockbackDir));
                }
            }

            AudioManager.Instance?.PlayUpgradeConfirm();
        }

        /// <summary>通过临时禁用 NavMeshAgent 实现击退位移</summary>
        private IEnumerator KnockbackAgent(NavMeshAgent agent, Vector3 direction)
        {
            Vector3 startPos = agent.transform.position;
            Vector3 targetPos = startPos + direction * knockbackDistance;

            // 确保目标点在 NavMesh 上
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
                targetPos = hit.position;

            agent.enabled = false;
            float elapsed = 0f;
            while (elapsed < knockbackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / knockbackDuration;
                // 缓出曲线
                float eased = 1f - (1f - t) * (1f - t);
                agent.transform.position = Vector3.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            agent.transform.position = targetPos;
            agent.enabled = true;
        }

        /// <summary>升级增加冲击波伤害</summary>
        public void IncreaseDamage(float amount)
        {
            damage += amount;
        }

        /// <summary>升级增加冲击波半径</summary>
        public void IncreaseRadius(float amount)
        {
            radius += amount;
        }

        /// <summary>设置暂停状态（升级选择时调用）</summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
    }
}
