using NUnit.Framework;
using UnityEngine;
using Game.Player;
using System.Reflection;

namespace Game.Tests
{
    /// <summary>PlayerMovement 闪避与动画参数测试</summary>
    public class PlayerMovementTests
    {
        private GameObject playerGO;
        private PlayerMovement movement;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            playerGO.AddComponent<Rigidbody>();
            movement = playerGO.AddComponent<PlayerMovement>();

            // Start() 不会在 EditMode 自动调用，手动初始化 rb 和 animator
            var rbField = typeof(PlayerMovement)
                .GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance);
            rbField?.SetValue(movement, playerGO.GetComponent<Rigidbody>());
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
        }

        [Test]
        public void DashSpeed_DefaultIs30()
        {
            Assert.AreEqual(30f, movement.dashSpeed, "默认闪避速度应为 30");
        }

        [Test]
        public void DashDuration_DefaultIsPoint2()
        {
            Assert.AreEqual(0.2f, movement.dashDuration, "默认闪避持续时间应为 0.2s");
        }

        [Test]
        public void DashCooldown_DefaultIs2()
        {
            Assert.AreEqual(2f, movement.dashCooldown, "默认闪避冷却应为 2s");
        }

        [Test]
        public void SetPaused_True_PreventsMovement()
        {
            movement.SetPaused(true);
            // isPaused 是 private，通过反射验证
            var isPaused = typeof(PlayerMovement)
                .GetField("isPaused", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(movement);
            Assert.IsTrue((bool)isPaused, "SetPaused(true) 后 isPaused 应为 true");
        }

        [Test]
        public void SetPaused_False_ResumesMovement()
        {
            movement.SetPaused(true);
            movement.SetPaused(false);
            var isPaused = typeof(PlayerMovement)
                .GetField("isPaused", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(movement);
            Assert.IsFalse((bool)isPaused, "SetPaused(false) 后 isPaused 应为 false");
        }

        [Test]
        public void Dash_FiresOnDashStateChangedEvent()
        {
            bool eventFired = false;
            bool eventValue = false;
            movement.OnDashStateChanged += (dashing) =>
            {
                eventFired = true;
                eventValue = dashing;
            };

            // Dash() 是 private，通过反射调用
            var dashMethod = typeof(PlayerMovement)
                .GetMethod("Dash", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(dashMethod, "Dash 方法应存在");
            dashMethod.Invoke(movement, null);

            Assert.IsTrue(eventFired, "Dash() 应触发 OnDashStateChanged 事件");
            Assert.IsTrue(eventValue, "事件参数应为 true（开始闪避）");
        }

        [Test]
        public void Dash_SetsIsDashingTrue()
        {
            var dashMethod = typeof(PlayerMovement)
                .GetMethod("Dash", BindingFlags.NonPublic | BindingFlags.Instance);
            dashMethod.Invoke(movement, null);

            var isDashing = typeof(PlayerMovement)
                .GetField("isDashing", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(movement);
            Assert.IsTrue((bool)isDashing, "Dash() 后 isDashing 应为 true");
        }

        [Test]
        public void Dash_SetsDashTimerToDuration()
        {
            var dashMethod = typeof(PlayerMovement)
                .GetMethod("Dash", BindingFlags.NonPublic | BindingFlags.Instance);
            dashMethod.Invoke(movement, null);

            var dashTimer = typeof(PlayerMovement)
                .GetField("dashTimer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(movement);
            Assert.AreEqual(movement.dashDuration, (float)dashTimer, 0.001f,
                "Dash() 后 dashTimer 应等于 dashDuration");
        }
    }
}
