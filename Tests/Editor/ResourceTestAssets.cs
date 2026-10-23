using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode.UnityResources {
    // 각 테스트 클래스가 소유한 에셋만 만들고 정리해 소비 프로젝트의 리소스와 충돌하지 않아요.
    internal sealed class ResourceTestAssets : IDisposable {
        private readonly string assetRoot;

        public string PrefabPath { get; }
        public string SpritePath { get; }

        public ResourceTestAssets() {
            string id = "UnityResourcesTests_" + Guid.NewGuid().ToString("N");
            assetRoot = "Assets/" + id;
            string resourcesRoot = assetRoot + "/Resources";
            string resourceDirectory = resourcesRoot + "/" + id;

            AssetDatabase.CreateFolder("Assets", id);
            AssetDatabase.CreateFolder(assetRoot, "Resources");
            AssetDatabase.CreateFolder(resourcesRoot, id);

            PrefabPath = id + "/TestPrefab";
            SpritePath = id + "/TestSprite";

            try {
                CreatePrefab(resourceDirectory + "/TestPrefab.prefab");
                CreateSprite(resourceDirectory + "/TestSprite.png");
            } catch {
                Dispose();
                throw;
            }
        }

        public void Dispose() {
            if (AssetDatabase.IsValidFolder(assetRoot)) {
                AssetDatabase.DeleteAsset(assetRoot);
            }
        }

        private static void CreatePrefab(string path) {
            var root = new GameObject("TestPrefab");
            try {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            } finally {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateSprite(string path) {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try {
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
            } finally {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
