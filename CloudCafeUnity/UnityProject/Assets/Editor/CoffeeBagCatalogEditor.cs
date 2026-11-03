#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CloudCafe.Coffee;
using UnityEditor;
using UnityEngine;

public static class CoffeeBagCatalogEditor
{
    private const string Root = "Assets/Resources/CoffeeBags";

    [Serializable] private sealed class ProductDocument { public Product[] products; }
    [Serializable] private sealed class Variety { public string name; public string name_ko; }
    [Serializable] private sealed class Taste { public int acidity; public int sweetness; public int body; }
    [Serializable] private sealed class Product
    {
        public string slug;
        public string country;
        public string country_ko;
        public string region;
        public string region_ko;
        public Variety variety;
        public string description_ko;
        public string[] tasting_notes_ko;
        public Taste taste;
        public string process_ko;
        public string roast_ko;
        public int net_weight_g;
        public string source_url;
    }

    [Serializable] private sealed class ValidationItem
    {
        public string id;
        public string definition;
        public string sprite;
        public string sprite_guid;
        public long sprite_file_id;
        public int width;
        public int height;
        public float pixels_per_unit;
    }

    [Serializable] private sealed class ValidationReport
    {
        public bool passed;
        public int count;
        public string unity_version;
        public string resource_path;
        public ValidationItem[] items;
    }

    /// <summary>Batch-mode validation of Unity's imported objects, without rebuilding them.</summary>
    [MenuItem("Cloud Cafe/Validate Registered Coffee Assets")]
    public static void ValidateRegisteredAssets()
    {
        AssetDatabase.Refresh();
        CoffeeBeanCatalog catalog = CoffeeBeanCatalog.Load();
        if (catalog == null || catalog.Beans == null || catalog.Beans.Length != 18)
            throw new InvalidDataException("The Resources coffee catalog must contain exactly 18 definitions.");
        if (AssetDatabase.GetAssetPath(catalog) != Root + "/CoffeeBeanCatalog.asset")
            throw new InvalidDataException("The loaded coffee catalog does not resolve to the registered asset.");
        var identities = new HashSet<string>(StringComparer.Ordinal);
        var spriteGuids = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<ValidationItem>();
        foreach (CoffeeBeanDefinition bean in catalog.Beans)
        {
            if (bean == null || string.IsNullOrEmpty(bean.Id) || !identities.Add(bean.Id))
                throw new InvalidDataException("Coffee definitions must be present and have unique identifiers.");
            if (string.IsNullOrEmpty(bean.Country) || string.IsNullOrEmpty(bean.CountryKo) ||
                string.IsNullOrEmpty(bean.Region) || string.IsNullOrEmpty(bean.RegionKo) ||
                string.IsNullOrEmpty(bean.Variety) || string.IsNullOrEmpty(bean.VarietyKo) ||
                string.IsNullOrEmpty(bean.Description) || bean.TastingNotes == null || bean.TastingNotes.Length < 2)
                throw new InvalidDataException("Coffee origin or tasting metadata is incomplete: " + bean.Id);
            foreach (int rating in new[] { bean.Acidity, bean.Sweetness, bean.Body })
                if (rating < 1 || rating > 5) throw new InvalidDataException("Coffee taste rating is outside 1..5: " + bean.Id);
            string definitionPath = Root + "/Beans/" + bean.Id + ".asset";
            if (AssetDatabase.GetAssetPath(bean) != definitionPath || catalog.Find(bean.Id) != bean)
                throw new InvalidDataException("Coffee definition lookup does not resolve to its asset: " + bean.Id);
            Sprite sprite = bean.BagSprite;
            Texture2D texture = bean.BagTexture;
            string spritePath = Root + "/Sprites/" + bean.Id + ".png";
            if (sprite == null || texture == null || texture.width != 1024 || texture.height != 1536 ||
                sprite.texture != texture || AssetDatabase.GetAssetPath(sprite) != spritePath ||
                AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) != sprite)
                throw new InvalidDataException("Coffee sprite/texture import is invalid: " + bean.Id);
            string spriteGuid;
            long spriteFileId;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out spriteGuid, out spriteFileId) ||
                spriteFileId != 21300000 || !spriteGuids.Add(spriteGuid))
                throw new InvalidDataException("Coffee Sprite GUID/local file ID is invalid: " + bean.Id);
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency ||
                importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                sprite.pixelsPerUnit <= 0)
                throw new InvalidDataException("Coffee image is not configured as an uncompressed transparent Single Sprite: " + bean.Id);
            items.Add(new ValidationItem { id = bean.Id, definition = definitionPath, sprite = spritePath,
                sprite_guid = spriteGuid, sprite_file_id = spriteFileId, width = texture.width,
                height = texture.height, pixels_per_unit = sprite.pixelsPerUnit });
        }
        var report = new ValidationReport { passed = true, count = items.Count,
            unity_version = Application.unityVersion, resource_path = "CoffeeBags/CoffeeBeanCatalog", items = items.ToArray() };
        string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Review/CoffeeAssets/catalog-validation.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        Debug.Log("COFFEE_BAG_CATALOG_VALIDATED " + items.Count);
    }

    [MenuItem("Cloud Cafe/Rebuild Coffee Bean Catalog")]
    public static void Rebuild()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CoffeeBags/Data/products.json");
        if (!File.Exists(path)) throw new FileNotFoundException("Coffee product data was not found.", path);
        ProductDocument document = JsonUtility.FromJson<ProductDocument>(File.ReadAllText(path));
        if (document == null || document.products == null || document.products.Length == 0)
            throw new InvalidDataException("Coffee product data is empty.");

        AssetDatabase.Refresh();
        EnsureFolder(Root + "/Beans");
        var definitions = new List<CoffeeBeanDefinition>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (Product product in document.products)
        {
            if (string.IsNullOrEmpty(product.slug) || !identities.Add(product.slug))
                throw new InvalidDataException("Coffee product identifiers must be nonempty and unique.");
            string imagePath = Root + "/Sprites/" + product.slug + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            if (sprite == null) throw new InvalidDataException("Coffee bag sprite is missing: " + imagePath);
            if (product.variety == null || product.taste == null)
                throw new InvalidDataException("Coffee product has no cultivar or taste profile: " + product.slug);

            string assetPath = Root + "/Beans/" + product.slug + ".asset";
            CoffeeBeanDefinition bean = AssetDatabase.LoadAssetAtPath<CoffeeBeanDefinition>(assetPath);
            if (bean == null)
            {
                bean = ScriptableObject.CreateInstance<CoffeeBeanDefinition>();
                AssetDatabase.CreateAsset(bean, assetPath);
            }
            bean.Id = product.slug;
            bean.Country = product.country;
            bean.CountryKo = product.country_ko;
            bean.Region = product.region;
            bean.RegionKo = product.region_ko;
            bean.Variety = product.variety.name;
            bean.VarietyKo = product.variety.name_ko;
            bean.Description = product.description_ko;
            bean.TastingNotes = product.tasting_notes_ko ?? new string[0];
            bean.Acidity = Mathf.Clamp(product.taste.acidity, 1, 5);
            bean.Sweetness = Mathf.Clamp(product.taste.sweetness, 1, 5);
            bean.Body = Mathf.Clamp(product.taste.body, 1, 5);
            bean.Process = product.process_ko;
            bean.Roast = product.roast_ko;
            bean.NetWeightGrams = product.net_weight_g;
            bean.SourceUrl = product.source_url;
            bean.BagSprite = sprite;
            EditorUtility.SetDirty(bean);
            definitions.Add(bean);
        }

        string catalogPath = Root + "/CoffeeBeanCatalog.asset";
        CoffeeBeanCatalog catalog = AssetDatabase.LoadAssetAtPath<CoffeeBeanCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CoffeeBeanCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }
        catalog.Beans = definitions.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;
        Debug.Log("Coffee bags: registered " + definitions.Count + " origin editions in " + catalogPath + ".");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}

/// <summary>Preserve each packaged cutout's scale and pivot on reimport.</summary>
public sealed class CoffeeBagSpritePostprocessor : AssetPostprocessor
{
    private const string SpriteRoot = "Assets/Resources/CoffeeBags/Sprites/";
    [Serializable] private sealed class SpriteImport
    {
        public string id;
        public float pixels_per_unit;
        public float[] pivot;
    }
    [Serializable] private sealed class SpriteManifest { public SpriteImport[] sprite_imports; }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(SpriteRoot, StringComparison.Ordinal) ||
            !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
        var importer = assetImporter as TextureImporter;
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
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;

        string dataPath = Path.Combine(Application.dataPath, "Resources/CoffeeBags/Data/sprite_manifest.json");
        if (File.Exists(dataPath))
        {
            SpriteManifest manifest = JsonUtility.FromJson<SpriteManifest>(File.ReadAllText(dataPath));
            if (manifest != null && manifest.sprite_imports != null)
            {
                string id = Path.GetFileNameWithoutExtension(assetPath);
                foreach (SpriteImport entry in manifest.sprite_imports)
                {
                    if (entry.id != id || entry.pivot == null || entry.pivot.Length != 2) continue;
                    if (entry.pixels_per_unit > 0) settings.spritePixelsPerUnit = entry.pixels_per_unit;
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(entry.pivot[0], entry.pivot[1]);
                    break;
                }
            }
        }
        importer.SetTextureSettings(settings);
    }
}
#endif
