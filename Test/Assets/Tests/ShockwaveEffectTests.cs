using NUnit.Framework;
using UnityEngine;
using Game.Combat;

namespace Game.Tests
{
    /// <summary>ShockwaveEffect 单元测试</summary>
    public class ShockwaveEffectTests
    {
        private GameObject playerGO;
        private ShockwaveEffect shockwave;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            shockwave = playerGO.AddComponent<ShockwaveEffect>();
            shockwave.damage = 3f;
            shockwave.radius = 6f;
            shockwave.knockbackDistance = 3f;
            shockwave.knockbackDuration = 0.3f;
            shockwave.expandDuration = 1f;
            shockwave.damageUpgradeStep = 2f;
            shockwave.radiusUpgradeStep = 1f;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
        }

        [Test]
        public void Damage_DefaultIsThree()
        {
            Assert.AreEqual(3f, shockwave.damage, "默认冲击波伤害应为 3");
        }

        [Test]
        public void Radius_DefaultIsSix()
        {
            Assert.AreEqual(6f, shockwave.radius, "默认冲击波半径应为 6");
        }

        [Test]
        public void KnockbackDistance_DefaultIsThree()
        {
            Assert.AreEqual(3f, shockwave.knockbackDistance, "默认击退距离应为 3");
        }

        [Test]
        public void ExpandDuration_DefaultIsPointFour()
        {
            Assert.AreEqual(1f, shockwave.expandDuration, "默认扩散持续时间应为 1 秒");
        }

        [Test]
        public void IncreaseDamage_AddsAmount()
        {
            shockwave.IncreaseDamage(2f);
            Assert.AreEqual(5f, shockwave.damage, 0.001f, "3 + 2 应为 5");
        }

        [Test]
        public void IncreaseDamage_MultipleTimes()
        {
            shockwave.IncreaseDamage(2f);
            shockwave.IncreaseDamage(2f);
            shockwave.IncreaseDamage(2f);
            Assert.AreEqual(9f, shockwave.damage, 0.001f, "三次 +2 应为 9");
        }

        [Test]
        public void IncreaseRadius_AddsAmount()
        {
            shockwave.IncreaseRadius(1f);
            Assert.AreEqual(7f, shockwave.radius, 0.001f, "6 + 1 应为 7");
        }

        [Test]
        public void IncreaseRadius_MultipleTimes()
        {
            shockwave.IncreaseRadius(1f);
            shockwave.IncreaseRadius(1f);
            Assert.AreEqual(8f, shockwave.radius, 0.001f, "两次 +1 应为 8");
        }

        [Test]
        public void SetPaused_SetsFlag()
        {
            shockwave.SetPaused(true);
            var field = typeof(ShockwaveEffect).GetField("isPaused",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue((bool)field.GetValue(shockwave), "暂停后 isPaused 应为 true");
        }

        [Test]
        public void SetPaused_UnsetsFlag()
        {
            shockwave.SetPaused(true);
            shockwave.SetPaused(false);
            var field = typeof(ShockwaveEffect).GetField("isPaused",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsFalse((bool)field.GetValue(shockwave), "恢复后 isPaused 应为 false");
        }

        [Test]
        public void DamageUpgradeStep_DefaultIsTwo()
        {
            Assert.AreEqual(2f, shockwave.damageUpgradeStep, "默认伤害升级步长应为 2");
        }

        [Test]
        public void RadiusUpgradeStep_DefaultIsOne()
        {
            Assert.AreEqual(1f, shockwave.radiusUpgradeStep, "默认半径升级步长应为 1");
        }

        [Test]
        public void Trigger_WithNoEnemies_DoesNotThrow()
        {
            // 没有敌人在范围内时 Trigger 不应抛异常
            Assert.DoesNotThrow(() => shockwave.Trigger(), "无敌人时 Trigger 不应抛异常");
        }

        [Test]
        public void Trigger_WithEnemy_DealsDamage()
        {
            // 创建一个带 Enemy 标签的敌人
            var enemyGO = new GameObject("Enemy");
            enemyGO.tag = "Enemy";
            enemyGO.AddComponent<BoxCollider>();
            var enemy = enemyGO.AddComponent<Game.Enemy.EnemyMovement>();
            // 通过反射设置 currentHealth 和 maxHealth（Start 不会在 EditMode 调用）
            var healthField = typeof(Game.Enemy.EnemyMovement).GetField("currentHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxField = typeof(Game.Enemy.EnemyMovement).GetField("maxHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxField.SetValue(enemy, 10f);
            healthField.SetValue(enemy, 10f);

            shockwave.Trigger();

            float healthAfter = (float)healthField.GetValue(enemy);
            Assert.AreEqual(7f, healthAfter, 0.001f, "10 - 3 = 7，敌人应受到冲击波伤害");

            Object.DestroyImmediate(enemyGO);
        }
    }
}
