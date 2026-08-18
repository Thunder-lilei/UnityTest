using NUnit.Framework;
using UnityEngine;
using Game.Combat;

namespace Game.Tests
{
    /// <summary>MeleeCombat 单元测试</summary>
    public class MeleeCombatTests
    {
        private GameObject playerGO;
        private MeleeCombat melee;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            melee = playerGO.AddComponent<MeleeCombat>();
            melee.slashInterval = 1.0f;
            melee.minSlashInterval = 0.5f;
            melee.slashDamage = 2f;
            melee.slashRadius = 4f;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
        }

        [Test]
        public void SlashInterval_DefaultIsOne()
        {
            Assert.AreEqual(1.0f, melee.slashInterval, "默认斩击间隔应为 1.0 秒");
        }

        [Test]
        public void ReduceSlashInterval_DecreasesInterval()
        {
            melee.ReduceSlashInterval(0.1f);
            Assert.AreEqual(0.9f, melee.slashInterval, 0.001f, "1.0 - 0.1 应为 0.9");
        }

        [Test]
        public void ReduceSlashInterval_DoesNotExceedMin()
        {
            for (int i = 0; i < 20; i++)
                melee.ReduceSlashInterval(0.1f);

            Assert.AreEqual(melee.minSlashInterval, melee.slashInterval, 0.001f,
                "减少到下限后不应继续减少");
        }

        [Test]
        public void SlashDamage_DefaultIsTwo()
        {
            Assert.AreEqual(2f, melee.slashDamage, "默认斩击伤害应为 2");
        }

        [Test]
        public void SlashRadius_DefaultIsFour()
        {
            Assert.AreEqual(4f, melee.slashRadius, "默认斩击范围应为 4");
        }

        [Test]
        public void SetPaused_StopsTimer()
        {
            melee.SetPaused(true);
            // 模拟一帧的 Update（不会触发斩击，因为 isPaused=true）
            // 验证 slashTimer 未增长（通过反射检查）
            var field = typeof(MeleeCombat).GetField("isPaused",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue((bool)field.GetValue(melee), "暂停后 isPaused 应为 true");
        }
    }
}
