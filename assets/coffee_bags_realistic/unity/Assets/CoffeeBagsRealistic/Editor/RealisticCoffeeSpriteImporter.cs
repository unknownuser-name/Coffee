#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoffeeBagsRealistic.Editor
{
    /// <summary>Preserves packaged sprite settings on later texture reimports.</summary>
    public sealed class RealisticCoffeeSpriteImporter : AssetPostprocessor
    {
        private const string SpriteRoot = "Assets/CoffeeBagsRealistic/Sprites/";
        private const string ManifestPath = "Assets/CoffeeBagsRealistic/Data/sprite_manifest.json";

        [Serializable]
        private sealed class SpriteImport
        {
            public string id;
            public float pixels_per_unit;
            public float[] pivot;
        }

        [Serializable]
        private sealed class SpriteManifest
        {
            public SpriteImport[] sprite_imports;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteRoot, StringComparison.OrdinalIgnoreCase) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;

            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            // Use the raw JSON file so import order does not depend on whether
            // Unity has already registered the TextAsset during package import.
            if (File.Exists(ManifestPath))
            {
                SpriteManifest manifest = JsonUtility.FromJson<SpriteManifest>(File.ReadAllText(ManifestPath));
                if (manifest != null && manifest.sprite_imports != null)
                {
                    string id = Path.GetFileNameWithoutExtension(assetPath);
                    foreach (SpriteImport entry in manifest.sprite_imports)
                    {
                        if (entry.id != id || entry.pivot == null || entry.pivot.Length != 2) continue;
                        if (entry.pixels_per_unit > 0f) settings.spritePixelsPerUnit = entry.pixels_per_unit;
                        settings.spriteAlignment = (int)SpriteAlignment.Custom;
                        settings.spritePivot = new Vector2(entry.pivot[0], entry.pivot[1]);
                        break;
                    }
                }
            }
            importer.SetTextureSettings(settings);
        }
    }
}
#endif
