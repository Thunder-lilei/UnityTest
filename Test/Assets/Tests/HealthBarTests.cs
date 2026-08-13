using NUnit.Framework;
using UnityEngine;
using Game.UI;

namespace Game.Tests
{
    /// <summary>HealthBar 单元测试</summary>
    public class PlayerHealthTests
    {
        private GameObject playerGO;
        private HealthBar healthBar;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            healthBar = playerGO.AddComponent<HealthBar>();
            healthBar.maxHealth = 100f;
            // EditMode 不会调用 Start()，需要手动初始化 currentHealth
            // 通过反射调用 Start 或直接用 TakeDamage/Heal 间接初始化
            // 这里用反射设置 private 字段
            var field = typeof(HealthBar).GetField("currentHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(healthBar, 100f);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            healthBar.TakeDamage(20f);
            // 100 - 20 = 80，不应死亡
            Assert.IsFalse(healthBar.IsDead(), "100HP 受 20 伤害不应死亡");
        }

        [Test]
        public void TakeDamage_ToZero_IsDead()
        {
            healthBar.TakeDamage(100f);
            Assert.IsTrue(healthBar.IsDead(), "100HP 受 100 伤害应死亡");
        }

        [Test]
        public void Heal_RestoresHealth()
        {
            healthBar.TakeDamage(50f);
            healthBar.Heal(30f);
            // 100 - 50 + 30 = 80，不应满血也不应死亡
            Assert.IsFalse(healthBar.IsFull(), "80HP 不应满血");
            Assert.IsFalse(healthBar.IsDead(), "80HP 不应死亡");
        }

        [Test]
        public void Heal_DoesNotExceedMax()
        {
            healthBar.TakeDamage(10f);
            healthBar.Heal(50f);
            Assert.IsTrue(healthBar.IsFull(), "回血超过上限应限制为满血");
        }

        [Test]
        public void IsFull_AtFullHealth()
        {
            Assert.IsTrue(healthBar.IsFull(), "初始状态应满血");
        }

        [Test]
        public void IncreaseMaxHealth_AddsHealth()
        {
            healthBar.IncreaseMaxHealth(20f);
            // maxHealth 100 + 20 = 120，同时回血 20
            // 受 100 伤害不应死亡（120HP）
            healthBar.TakeDamage(100f);
            Assert.IsFalse(healthBar.IsDead(), "120HP 受 100 伤害不应死亡");
        }
    }
}
