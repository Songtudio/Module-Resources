using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utility;

namespace Tests.EditMode.UnityResources {
    public class ObjectPoolingTests {
        private static readonly FieldInfo ObjectPoolField = typeof(ObjectPooling).GetField(
            "objectPool",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        private static readonly FieldInfo SingletonInstanceField = typeof(SingletonMonoBehaviour<ObjectPooling>).GetField(
            "instance",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        private readonly List<GameObject> roots = new List<GameObject>();
        private ObjectPooling pooling;
        private ResourceTestAssets assets;

        [OneTimeSetUp]
        public void CreateTestAssets() {
            assets = new ResourceTestAssets();
        }

        [OneTimeTearDown]
        public void DeleteTestAssets() {
            assets?.Dispose();
        }

        [SetUp]
        public void SetUp() {
            ClearStaticState();
            var root = Track(new GameObject("ObjectPoolingTests"));
            pooling = root.AddComponent<ObjectPooling>();
        }

        [TearDown]
        public void TearDown() {
            ClearStaticState();
            foreach (GameObject root in roots) {
                if (root != null) Object.DestroyImmediate(root);
            }
            roots.Clear();
        }

        [Test]
        public void Get_ExistingPrefab_ReturnsActiveObject() {
            GameObject instance = pooling.Get(assets.PrefabPath);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.activeSelf, Is.True);
            Assert.That(instance.transform.parent, Is.EqualTo(pooling.transform));
        }

        [Test]
        public void TryAbandon_ThenGet_ReusesSameObject() {
            GameObject first = pooling.Get(assets.PrefabPath);

            bool abandoned = pooling.TryAbandon(assets.PrefabPath, first);
            GameObject second = pooling.Get(assets.PrefabPath);

            Assert.That(abandoned, Is.True);
            Assert.That(first.activeSelf, Is.True);
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void Get_WithParent_ReparentsObject() {
            var parent = Track(new GameObject("PoolTargetParent"));

            GameObject instance = pooling.Get(assets.PrefabPath, parent.transform);

            Assert.That(instance.transform.parent, Is.EqualTo(parent.transform));
        }

        [Test]
        public void Get_MissingPrefab_ReturnsNull() {
            LogAssert.Expect(LogType.Error, new Regex(
                "ObjectPooling.LoadPrefab::missing-test-prefab"
            ));

            GameObject instance = pooling.Get("missing-test-prefab");

            Assert.That(instance, Is.Null);
        }

        [Test]
        public void Get_MissingComponent_ReturnsNull() {
            Camera component = pooling.Get<Camera>(assets.PrefabPath);

            Assert.That(component, Is.Null);
        }

        [Test]
        public void TryAbandon_NullObject_ReturnsFalse() {
            LogAssert.Expect(LogType.Error,
                $"ObjectPooling.TryAbandon::{assets.PrefabPath} object is null.");

            bool abandoned = pooling.TryAbandon(assets.PrefabPath, null);

            Assert.That(abandoned, Is.False);
        }

        [Test]
        public void TryAbandon_SameObjectTwice_RejectsDuplicateEntry() {
            GameObject first = pooling.Get(assets.PrefabPath);
            Assert.That(pooling.TryAbandon(assets.PrefabPath, first), Is.True);
            LogAssert.Expect(LogType.Error,
                $"ObjectPooling.TryAbandon::{assets.PrefabPath} object is already abandoned.");

            bool secondAbandon = pooling.TryAbandon(assets.PrefabPath, first);
            GameObject reused = pooling.Get(assets.PrefabPath);
            GameObject next = pooling.Get(assets.PrefabPath);

            Assert.That(secondAbandon, Is.False);
            Assert.That(reused, Is.SameAs(first));
            Assert.That(next, Is.Not.SameAs(first));
        }

        [Test]
        public void WarmUp_PositiveAmount_CreatesInactiveObjectsForReuse() {
            pooling.WarmUp(assets.PrefabPath, 3);

            Assert.That(pooling.transform.childCount, Is.EqualTo(3));
            foreach (Transform child in pooling.transform) {
                Assert.That(child.gameObject.activeSelf, Is.False);
            }

            GameObject instance = pooling.Get(assets.PrefabPath);
            Assert.That(instance.activeSelf, Is.True);
            Assert.That(pooling.transform.childCount, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void WarmUp_NonPositiveAmount_DoesNotCreateObjects(int amount) {
            LogAssert.Expect(LogType.Error, new Regex(
                $"ObjectPooling.WarmUp::.*{amount}"
            ));

            pooling.WarmUp(assets.PrefabPath, amount);

            Assert.That(pooling.transform.childCount, Is.Zero);
        }

        private GameObject Track(GameObject gameObject) {
            roots.Add(gameObject);
            return gameObject;
        }

        private static void ClearStaticState() {
            var dictionary = ObjectPoolField?.GetValue(null) as IDictionary;
            dictionary?.Clear();
            SingletonInstanceField?.SetValue(null, null);
        }
    }
}
