using System;

namespace CloudCafe.Crafting
{
    public enum ProfessionalEspressoFeedback
    {
        NoBeverage,
        UnevenPuck,
        DoseOffTarget,
        RatioTooShort,
        RatioTooLong,
        UnderExtracted,
        OverExtracted,
        FlowTooFast,
        FlowTooSlow,
        OnTarget
    }

    public readonly struct ProfessionalEspressoResult
    {
        public float DoseGrams { get; }
        public float BeverageGrams { get; }
        public float ElapsedSeconds { get; }
        public float BrewRatio { get; }
        public float TdsPercent { get; }
        public float ExtractionYieldPercent { get; }
        public float ChannelingRisk { get; }
        public int Score { get; }
        public ProfessionalEspressoFeedback Feedback { get; }

        internal ProfessionalEspressoResult(float doseGrams, float beverageGrams, float elapsedSeconds,
            float tdsPercent, float extractionYieldPercent, float channelingRisk, int score,
            ProfessionalEspressoFeedback feedback)
        {
            DoseGrams = doseGrams;
            BeverageGrams = beverageGrams;
            ElapsedSeconds = elapsedSeconds;
            BrewRatio = beverageGrams / doseGrams;
            TdsPercent = tdsPercent;
            ExtractionYieldPercent = extractionYieldPercent;
            ChannelingRisk = channelingRisk;
            Score = score;
            Feedback = feedback;
        }
    }

    public sealed class ProfessionalEspressoModel
    {
        // House recipe calibrated from the SCA's espresso practice survey.
        public const float TargetDoseGrams = 18f;
        public const float TargetBeverageGrams = 36f;
        public const float TargetMinSeconds = 25f;
        public const float TargetMaxSeconds = 30f;
        public const float TargetTemperatureCelsius = 93f;
        public const float TargetPressureBar = 9f;

        // WBC machine ranges. The in-game machine lets players set its pressure.
        public const float MinTemperatureCelsius = 90.5f;
        public const float MaxTemperatureCelsius = 96f;
        public const float MinPressureBar = 8.5f;
        public const float MaxPressureBar = 9.5f;

        public const float MinDoseGrams = 12f;
        public const float MaxDoseGrams = 22f;
        public const int MinGrindSetting = 1;
        public const int NominalGrindSetting = 5;
        public const int MaxGrindSetting = 9;
        public const float PreinfusionSeconds = 3f;
        public const float ShotLimitSeconds = 60f;

        const float MinTargetRatio = 1.8f;
        const float MaxTargetRatio = 2.2f;
        const float MinTargetExtraction = 18f;
        const float MaxTargetExtraction = 22f;
        const float NominalFlow = 1.54f;

        readonly float coarseness;
        readonly float steadyFlow;

        public float DoseGrams { get; }
        public int GrindSetting { get; }
        public float PuckUniformity { get; }
        public float TemperatureCelsius { get; }
        public float PressureBar { get; }
        public float ChannelingRisk { get; }

        public ProfessionalEspressoModel(float doseGrams, int grindSetting, float puckUniformity,
            float temperatureCelsius = TargetTemperatureCelsius, float pressureBar = TargetPressureBar)
        {
            DoseGrams = Clamp(FiniteOr(doseGrams, TargetDoseGrams), MinDoseGrams, MaxDoseGrams);
            GrindSetting = Math.Max(MinGrindSetting, Math.Min(MaxGrindSetting, grindSetting));
            PuckUniformity = Clamp(FiniteOr(puckUniformity, 0f), 0f, 1f);
            TemperatureCelsius = Clamp(FiniteOr(temperatureCelsius, TargetTemperatureCelsius),
                MinTemperatureCelsius, MaxTemperatureCelsius);
            PressureBar = Clamp(FiniteOr(pressureBar, TargetPressureBar), MinPressureBar, MaxPressureBar);

            coarseness = (GrindSetting - NominalGrindSetting) / (float)(MaxGrindSetting - NominalGrindSetting);
            float unevenPrep = 1f - PuckUniformity;
            float veryFine = Math.Max(0f, (-coarseness - .35f) / .65f);
            float extraPressure = Math.Max(0f, PressureBar - TargetPressureBar);

            // Cameron et al. (2020), doi:10.1016/j.matt.2019.12.019, found that extraction
            // dropped at both grind extremes. These coefficients model that pattern for play.
            ChannelingRisk = Clamp(.78f * unevenPrep + veryFine * veryFine * (.57f + .08f * extraPressure), 0f, .95f);
            float resistanceChange = .85f * coarseness + .055f * (TargetDoseGrams - DoseGrams);
            float pressureAndTemperature = .13f * (PressureBar - TargetPressureBar)
                + .008f * (TemperatureCelsius - TargetTemperatureCelsius);
            steadyFlow = NominalFlow * (float)Math.Exp(resistanceChange + pressureAndTemperature) * (1f + .32f * unevenPrep);
        }

        public float FlowGramsPerSecond(float elapsedSeconds)
        {
            elapsedSeconds = FiniteOr(elapsedSeconds, 0f);
            if (elapsedSeconds >= ShotLimitSeconds)
                return 0f;

            // The first three seconds wet the puck. Flow then ramps up over one second.
            float ramp = Clamp(elapsedSeconds - PreinfusionSeconds, 0f, 1f);
            return steadyFlow * ramp;
        }

        public float DissolvedGrams(float beverageGrams, float elapsedSeconds)
        {
            beverageGrams = Math.Max(0f, FiniteOr(beverageGrams, 0f));
            float wetSeconds = Math.Max(0f, FiniteOr(elapsedSeconds, 0f) - PreinfusionSeconds);
            if (wetSeconds <= 0f)
                return 0f;
            float ratio = beverageGrams / DoseGrams;

            // A saturating extraction approximation with less available surface at coarse
            // settings and less evenly wetted coffee when channels form. Preinfusion
            // primes the first drops, and continued flow progressively dilutes the cup.
            float contact = .65f + .35f * Clamp(wetSeconds / (10f + 7f * ratio), 0f, 1.35f);
            float temperatureFactor = 1f + .025f * (TemperatureCelsius - TargetTemperatureCelsius);
            float surfaceFactor = 1f - .32f * coarseness;
            float exposure = .69f * temperatureFactor * surfaceFactor * contact * ratio;
            float availableSolids = .28f * DoseGrams * (1f - .68f * ChannelingRisk);
            float dissolved = availableSolids * (1f - (float)Math.Exp(-exposure));
            return Math.Min(beverageGrams, dissolved);
        }

        public ProfessionalEspressoResult Evaluate(float beverageGrams, float elapsedSeconds)
        {
            beverageGrams = Math.Max(0f, FiniteOr(beverageGrams, 0f));
            elapsedSeconds = Math.Max(0f, FiniteOr(elapsedSeconds, 0f));
            if (beverageGrams <= .001f)
                return new ProfessionalEspressoResult(DoseGrams, beverageGrams, elapsedSeconds, 0f, 0f,
                    ChannelingRisk, 0, ProfessionalEspressoFeedback.NoBeverage);

            float tdsPercent = DissolvedGrams(beverageGrams, elapsedSeconds) / beverageGrams * 100f;
            // Moroney et al. (2019), doi:10.1371/journal.pone.0219906, Eq. (1).
            float extractionYieldPercent = beverageGrams * tdsPercent / DoseGrams;
            float ratio = beverageGrams / DoseGrams;
            float penalty = 5f * DistanceOutside(DoseGrams, TargetDoseGrams - 1f, TargetDoseGrams + 1f)
                + 22f * DistanceOutside(ratio, MinTargetRatio, MaxTargetRatio)
                + 1.5f * DistanceOutside(elapsedSeconds, TargetMinSeconds, TargetMaxSeconds)
                + 4f * DistanceOutside(extractionYieldPercent, MinTargetExtraction, MaxTargetExtraction)
                + 48f * ChannelingRisk;
            int score = (int)Math.Round(Clamp(100f - penalty, 0f, 100f));
            ProfessionalEspressoFeedback feedback = Diagnose(ratio, elapsedSeconds, extractionYieldPercent);

            return new ProfessionalEspressoResult(DoseGrams, beverageGrams, elapsedSeconds, tdsPercent,
                extractionYieldPercent, ChannelingRisk, score, feedback);
        }

        ProfessionalEspressoFeedback Diagnose(float ratio, float elapsedSeconds, float extractionYieldPercent)
        {
            if (ChannelingRisk >= .35f)
                return ProfessionalEspressoFeedback.UnevenPuck;
            if (Math.Abs(DoseGrams - TargetDoseGrams) > 1f)
                return ProfessionalEspressoFeedback.DoseOffTarget;
            if (ratio < MinTargetRatio)
                return ProfessionalEspressoFeedback.RatioTooShort;
            if (ratio > MaxTargetRatio)
                return ProfessionalEspressoFeedback.RatioTooLong;
            if (elapsedSeconds < TargetMinSeconds)
                return ProfessionalEspressoFeedback.FlowTooFast;
            if (elapsedSeconds > TargetMaxSeconds)
                return ProfessionalEspressoFeedback.FlowTooSlow;
            if (extractionYieldPercent < MinTargetExtraction)
                return ProfessionalEspressoFeedback.UnderExtracted;
            if (extractionYieldPercent > MaxTargetExtraction)
                return ProfessionalEspressoFeedback.OverExtracted;
            return ProfessionalEspressoFeedback.OnTarget;
        }

        static float DistanceOutside(float value, float min, float max) => Math.Max(0f, Math.Max(min - value, value - max));
        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        static float FiniteOr(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    }
}
