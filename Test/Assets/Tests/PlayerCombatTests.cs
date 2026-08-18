using NUnit.Framework;
using UnityEngine;
using Game.Player;

namespace Game.Tests
{
    /// <summary>PlayerCombat 单元测试</summary>
    public class PlayerCombatTests
    {
        private GameObject playerGO;
        private PlayerCombat combat;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            combat = playerGO.AddComponent<PlayerCombat>();
            combat.fireInterval = 1.0f;
            combat.minFireInterval = 0.1f;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
        }

        [Test]
        public void ReduceFireInterval_DecreasesInterval()
        {
            combat.ReduceFireInterval(0.1f);
            Assert.AreEqual(0.9f, combat.fireInterval, 0.001f, "1.0 - 0.1 应为 0.9");
        }

        [Test]
        public void ReduceFireInterval_MultipleTimes()
        {
            combat.ReduceFireInterval(0.1f);
            combat.ReduceFireInterval(0.1f);
            combat.ReduceFireInterval(0.1f);
            Assert.AreEqual(0.7f, combat.fireInterval, 0.001f, "三次减少应为 0.7");
        }

        [Test]
        public void ReduceFireInterval_DoesNotExceedMin()
        {
            // 连续减少 15 次，每次 0.1，总计 1.5
            for (int i = 0; i < 15; i++)
                combat.ReduceFireInterval(0.1f);

            Assert.AreEqual(combat.minFireInterval, combat.fireInterval, 0.001f,
                "减少到下限后不应继续减少");
        }

        [Test]
        public void ReduceFireInterval_MinIsPointOne()
        {
            Assert.AreEqual(0.1f, combat.minFireInterval, "最小间隔应为 0.1");
        }

        [Test]
        public void FireInterval_DefaultIsOne()
        {
            Assert.AreEqual(1.0f, combat.fireInterval, "默认发射间隔应为 1.0 秒");
        }
    }
}
