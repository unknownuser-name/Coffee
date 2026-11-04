using System;

namespace CloudCafe.Crafting
{
    public sealed class AmericanoModel
    {
        public const float CapacityGrams = 240f;
        public const float RecommendedWaterToEspressoRatio = 3f;
        public const float PourRateGramsPerSecond = 50f;
        private const float MaxFrameSeconds = .05f;
        private const float MinEspressoGrams = .001f;

        public float EspressoGrams { get; private set; }
        public float WaterGrams { get; private set; }
        public float TotalGrams => EspressoGrams + WaterGrams;
        public float TargetWaterGrams => Math.Min(CapacityGrams - EspressoGrams,
            EspressoGrams * RecommendedWaterToEspressoRatio);
        public float WaterToEspressoRatio => EspressoGrams > 0f ? WaterGrams / EspressoGrams : 0f;
        public bool Active => EspressoGrams > 0f && !Complete;
        public bool Complete { get; private set; }
        public bool Pouring { get; private set; }

        public bool Begin(float espressoGrams)
        {
            if (Active || float.IsNaN(espressoGrams) || espressoGrams < MinEspressoGrams ||
                espressoGrams >= CapacityGrams)
                return false;

            Reset();
            EspressoGrams = espressoGrams;
            return true;
        }

        public void Step(float dt, bool pouring)
        {
            Pouring = false;
            if (!Active || !pouring || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f)
                return;

            float amount = PourRateGramsPerSecond * Math.Min(dt, MaxFrameSeconds);
            float water = Math.Min(CapacityGrams - EspressoGrams, WaterGrams + amount);
            Pouring = water > WaterGrams;
            WaterGrams = water;
        }

        public bool TryFinish()
        {
            if (!Active || WaterGrams <= 0f) return false;
            Complete = true;
            Pouring = false;
            return true;
        }

        public void Reset()
        {
            EspressoGrams = WaterGrams = 0f;
            Complete = Pouring = false;
        }
    }
}
