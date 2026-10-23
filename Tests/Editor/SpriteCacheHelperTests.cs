using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utility;

namespace Tests.EditMode.UnityResources {
    public class SpriteCacheHelperTests {
        private ResourceTestAssets assets;

        [OneTimeSetUp]
        public void CreateTestAssets() {
            assets = new ResourceTestAssets();
        }

        [OneTimeTearDown]
        public void DeleteTestAssets() {
            SpriteCacheHelper.Clear();
            assets?.Dispose();
        }

        [SetUp]
        public void SetUp() {
            SpriteCacheHelper.Clear();
        }

        [TearDown]
        public void TearDown() {
            SpriteCacheHelper.Clear();
        }

        [Test]
        public void Get_ExistingSprite_ReturnsCachedInstance() {
            Sprite first = SpriteCacheHelper.Get(assets.SpritePath);
            Sprite second = SpriteCacheHelper.Get(assets.SpritePath);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void Get_MissingSprite_ReturnsNullAndLogsError() {
            LogAssert.Expect(LogType.Error, new Regex("SpriteCacheHelper.Get::missing-test-sprite"));

            Sprite sprite = SpriteCacheHelper.Get("missing-test-sprite");

            Assert.That(sprite, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Get_InvalidName_ReturnsNullAndLogsError(string name) {
            LogAssert.Expect(LogType.Error, "SpriteCacheHelper.Get::invalid sprite name.");

            Sprite sprite = SpriteCacheHelper.Get(name);

            Assert.That(sprite, Is.Null);
        }

        [Test]
        public void Clear_AllowsSpriteToBeLoadedAgain() {
            Assert.That(SpriteCacheHelper.Get(assets.SpritePath), Is.Not.Null);

            SpriteCacheHelper.Clear();

            Assert.That(SpriteCacheHelper.Get(assets.SpritePath), Is.Not.Null);
        }
    }
}
