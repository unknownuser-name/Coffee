using UnityEngine;

namespace CloudCafe.Coffee
{
    /// <summary>An origin edition and its fixed-view photographic bag sprite.</summary>
    [CreateAssetMenu(menuName = "Cloud Cafe/Coffee Bean", fileName = "CoffeeBean")]
    public sealed class CoffeeBeanDefinition : ScriptableObject
    {
        public string Id;
        public string Country;
        public string CountryKo;
        public string Region;
        public string RegionKo;
        public string Variety;
        public string VarietyKo;
        [TextArea(2, 5)] public string Description;
        public string[] TastingNotes;
        [Range(1, 5)] public int Acidity = 3;
        [Range(1, 5)] public int Sweetness = 3;
        [Range(1, 5)] public int Body = 3;
        public string Process;
        public string Roast;
        public int NetWeightGrams = 250;
        public string SourceUrl;
        public Sprite BagSprite;

        public Texture2D BagTexture { get { return BagSprite == null ? null : BagSprite.texture; } }
    }
}
