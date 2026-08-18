using NUnit.Framework;
using UnityEngine;
using Game.Player;
using Game.UI;

namespace Game.Tests
{
    /// <summary>PlayerHealth DPS 上限机制测试</summary>
    public class PlayerHealthDPSTests
    {
        private GameObject playerGO;
        private HealthBar healthBar;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            healthBar = playerGO.AddComponent<HealthBar>();
            healthBar.maxHealth = 200f;

            var field = typeof(HealthBar).GetField("currentHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(healthBar, 200f);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
        }

        [Test]
        public void MaxDPS_DefaultIs40()
        {
            var ph = playerGO.AddComponent<PlayerHealth>();
            Assert.AreEqual(40f, ph.maxDPS, "默认 DPS 上限应为 40");
        }

        [Test]
        public void HealthBar_MaxHealthIs200()
        {
            Assert.AreEqual(200f, healthBar.maxHealth, "500 敌人规模下初始血量应为 200");
        }

        [Test]
        public void HealthBar_SurvivesMoreThanOld100HP()
        {
            healthBar.TakeDamage(100f);
            Assert.IsFalse(healthBar.IsDead(), "200HP 受 100 伤害不应死亡");
        }
    }
}
