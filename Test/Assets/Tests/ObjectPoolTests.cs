using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Game.Systems;

namespace Game.Tests
{
    /// <summary>ObjectPool 单元测试</summary>
    public class ObjectPoolTests
    {
        private GameObject poolGO;
        private ObjectPool pool;
        private GameObject testPrefab;

        [SetUp]
        public void Setup()
        {
            poolGO = new GameObject("TestPool");
            pool = poolGO.AddComponent<ObjectPool>();
            testPrefab = new GameObject("TestPrefab");
            testPrefab.AddComponent<TestPooledObject>();

            pool.prefab = testPrefab;
            pool.initialSize = 3;
            pool.parent = poolGO.transform;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(poolGO);
            Object.DestroyImmediate(testPrefab);
        }

        [Test]
        public void Spawn_CreatesObject()
        {
            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.IsNotNull(obj, "Spawn 应返回非空对象");
            Assert.IsTrue(obj.activeInHierarchy, "Spawn 的对象应处于激活状态");
        }

        [Test]
        public void Despawn_DeactivatesObject()
        {
            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            pool.Despawn(obj);
            Assert.IsFalse(obj.activeInHierarchy, "Despawn 的对象应处于非激活状态");
        }

        [Test]
        public void Spawn_ReusesDespawnedObject()
        {
            GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
            pool.Despawn(obj1);
            GameObject obj2 = pool.Spawn(Vector3.zero, Quaternion.identity);

            Assert.AreSame(obj1, obj2, "Spawn 应复用已 Despawn 的对象而非创建新对象");
        }

        [Test]
        public void Spawn_SetsPosition()
        {
            Vector3 pos = new Vector3(1, 2, 3);
            GameObject obj = pool.Spawn(pos, Quaternion.identity);
            Assert.AreEqual(pos, obj.transform.position, "Spawn 应设置对象位置");
        }

        [Test]
        public void Spawn_CallsOnSpawnCallback()
        {
            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            var pooled = obj.GetComponent<TestPooledObject>();
            Assert.IsTrue(pooled.onSpawnCalled, "Spawn 应调用 IPooledObject.OnSpawn");
        }

        /// <summary>测试用 IPooledObject 实现</summary>
        public class TestPooledObject : MonoBehaviour, IPooledObject
        {
            public bool onSpawnCalled = false;
            public void OnSpawn() { onSpawnCalled = true; }
        }
    }
}
