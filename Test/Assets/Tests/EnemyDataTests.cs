using NUnit.Framework;
using UnityEngine;
using Game.Enemy;

namespace Game.Tests
{
    /// <summary>EnemyData ScriptableObject 单元测试</summary>
    public class EnemyDataTests
    {
        [Test]
        public void CreateInstance_DefaultsAreCorrect()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            Assert.AreEqual("Enemy", data.enemyName, "默认名称应为 Enemy");
            Assert.IsFalse(data.isBoss, "默认 isBoss 应为 false");
            Assert.AreEqual(2f, data.maxHealth, "默认血量应为 2");
            Assert.AreEqual(2.5f, data.moveSpeed, "默认速度应为 2.5");
            Assert.AreEqual(1f, data.scale, "默认缩放应为 1");
            Assert.AreEqual(0.3f, data.dropChance, "默认掉落概率应为 0.3");
            Assert.AreEqual(1, data.expDrop, "默认经验掉落应为 1");
        }

        [Test]
        public void CreateInstance_BossConfig()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Boss";
            data.isBoss = true;
            data.maxHealth = 20f;
            data.moveSpeed = 1f;
            data.scale = 2.5f;
            data.dropChance = 1f;
            data.expDrop = 3;

            Assert.IsTrue(data.isBoss, "Boss isBoss 应为 true");
            Assert.AreEqual(20f, data.maxHealth, "Boss 血量应为 20");
            Assert.AreEqual(1f, data.moveSpeed, "Boss 速度应为 1");
            Assert.AreEqual(2.5f, data.scale, "Boss 缩放应为 2.5");
            Assert.AreEqual(1f, data.dropChance, "Boss 掉落概率应为 1（必掉）");
            Assert.AreEqual(3, data.expDrop, "Boss 经验掉落应为 3");
        }

        [Test]
        public void CreateInstance_TankConfig()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Tank";
            data.maxHealth = 6f;
            data.moveSpeed = 1.5f;
            data.scale = 1.5f;

            Assert.AreEqual(6f, data.maxHealth, "Tank 血量应为 6");
            Assert.AreEqual(1.5f, data.moveSpeed, "Tank 速度应为 1.5");
            Assert.AreEqual(1.5f, data.scale, "Tank 缩放应为 1.5");
        }

        [Test]
        public void CreateInstance_FastConfig()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Fast";
            data.maxHealth = 1f;
            data.moveSpeed = 5f;
            data.scale = 0.7f;

            Assert.AreEqual(1f, data.maxHealth, "Fast 血量应为 1");
            Assert.AreEqual(5f, data.moveSpeed, "Fast 速度应为 5");
            Assert.AreEqual(0.7f, data.scale, "Fast 缩放应为 0.7");
        }
    }
}
