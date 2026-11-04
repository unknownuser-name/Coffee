using System;
using CloudCafe.Coffee;

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

        const float RatioTolerance = .2f;
        const float ExtractionTolerance = 2f;
        const float TimeTolerance = 2.5f;
        const float NominalFlow = 1.54f;

        readonly float coarseness;
        readonly float steadyFlow;
        readonly float roastExtractionFactor;

        public float DoseGrams { get; }
        public int GrindSetting { get; }
        public float PuckUniformity { get; }
        public float TemperatureCelsius { get; }
        public float PressureBar { get; }
        public float ChannelingRisk { get; }
        public float TargetBrewRatio { get; }
        public float RecommendedTimeMinSeconds { get; }
        public float RecommendedTimeMaxSeconds { get; }
        public float RecommendedExtractionYieldPercent { get; }

        public ProfessionalEspressoModel(float doseGrams, int grindSetting, float puckUniformity,
            float temperatureCelsius = TargetTemperatureCelsius, float pressureBar = TargetPressureBar,
            EspressoBeanProfile? beanProfile = null)
        {
            DoseGrams = Clamp(FiniteOr(doseGrams, TargetDoseGrams), MinDoseGrams, MaxDoseGrams);
            GrindSetting = Math.Max(MinGrindSetting, Math.Min(MaxGrindSetting, grindSetting));
            PuckUniformity = Clamp(FiniteOr(puckUniformity, 0f), 0f, 1f);
            TemperatureCelsius = Clamp(FiniteOr(temperatureCelsius, TargetTemperatureCelsius),
                MinTemperatureCelsius, MaxTemperatureCelsius);
            PressureBar = Clamp(FiniteOr(pressureBar, TargetPressureBar), MinPressureBar, MaxPressureBar);

            coarseness = RelativeCoarseness(GrindSetting);
            float unevenPrep = 1f - PuckUniformity;

            // Cameron et al. (2020), doi:10.1016/j.matt.2019.12.019, found that extraction
            // dropped at both grind extremes. These coefficients model that pattern for play.
            ChannelingRisk = PuckChannelingRisk(coarseness, unevenPrep, PressureBar);
            steadyFlow = SteadyFlow(coarseness, DoseGrams, TemperatureCelsius, PressureBar, unevenPrep);
            EspressoBeanProfile profile = beanProfile.GetValueOrDefault();
            if (profile.BeanId == null)
            {
                TargetBrewRatio = 2f;
                roastExtractionFactor = 1f;
                RecommendedTimeMinSeconds = TargetMinSeconds;
                RecommendedTimeMaxSeconds = TargetMaxSeconds;
                RecommendedExtractionYieldPercent = 20f;
                return;
            }

            TargetBrewRatio = profile.BrewRatio;
            roastExtractionFactor = profile.RoastExtractionFactor;
            float referenceCoarseness = RelativeCoarseness(profile.GrindSetting);
            float referenceMass = TargetDoseGrams * TargetBrewRatio;
            float referenceFlow = SteadyFlow(referenceCoarseness, TargetDoseGrams,
                profile.TemperatureCelsius, TargetPressureBar, 0f);
            // At the recipe's target mass the ramp has finished. Its 1s ramp contributes
            // half a second of steady flow, so only the other half adds to the shot time.
            float referenceTime = PreinfusionSeconds + .5f + referenceMass / referenceFlow;
            RecommendedTimeMinSeconds = referenceTime - TimeTolerance;
            RecommendedTimeMaxSeconds = referenceTime + TimeTolerance;
            RecommendedExtractionYieldPercent = DissolvedMass(referenceMass, referenceTime, TargetDoseGrams,
                referenceCoarseness, profile.TemperatureCelsius,
                PuckChannelingRisk(referenceCoarseness, 0f, TargetPressureBar), roastExtractionFactor)
                / TargetDoseGrams * 100f;
        }

        static float RelativeCoarseness(int grind) =>
            (grind - NominalGrindSetting) / (float)(MaxGrindSetting - NominalGrindSetting);

        static float PuckChannelingRisk(float coarseness, float unevenPrep, float pressureBar)
        {
            float veryFine = Math.Max(0f, (-coarseness - .35f) / .65f);
            float extraPressure = Math.Max(0f, pressureBar - TargetPressureBar);
            return Clamp(.78f * unevenPrep + veryFine * veryFine * (.57f + .08f * extraPressure), 0f, .95f);
        }

        static float SteadyFlow(float coarseness, float dose, float temperature, float pressure, float unevenPrep)
        {
            float resistanceChange = .85f * coarseness + .055f * (TargetDoseGrams - dose);
            float pressureAndTemperature = .13f * (pressure - TargetPressureBar)
                + .008f * (temperature - TargetTemperatureCelsius);
            return NominalFlow * (float)Math.Exp(resistanceChange + pressureAndTemperature) * (1f + .32f * unevenPrep);
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
            return DissolvedMass(beverageGrams, FiniteOr(elapsedSeconds, 0f), DoseGrams,
                coarseness, TemperatureCelsius, ChannelingRisk, roastExtractionFactor);
        }

        static float DissolvedMass(float beverageGrams, float elapsedSeconds, float doseGrams,
            float coarseness, float temperature, float channelingRisk, float roastFactor)
        {
            float wetSeconds = Math.Max(0f, elapsedSeconds - PreinfusionSeconds);
            if (wetSeconds <= 0f)
                return 0f;
            float ratio = beverageGrams / doseGrams;

            // A saturating extraction approximation with less available surface at coarse
            // settings and less evenly wetted coffee when channels form. Preinfusion
            // primes the first drops, and continued flow progressively dilutes the cup.
            float contact = .65f + .35f * Clamp(wetSeconds / (10f + 7f * ratio), 0f, 1.35f);
            float temperatureFactor = 1f + .025f * (temperature - TargetTemperatureCelsius);
            float surfaceFactor = 1f - .32f * coarseness;
            float exposure = .69f * temperatureFactor * surfaceFactor * contact * ratio;
            float availableSolids = .28f * roastFactor * doseGrams * (1f - .68f * channelingRisk);
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
                + 22f * DistanceOutside(ratio, TargetBrewRatio - RatioTolerance, TargetBrewRatio + RatioTolerance)
                + 1.5f * DistanceOutside(elapsedSeconds, RecommendedTimeMinSeconds, RecommendedTimeMaxSeconds)
                + 4f * DistanceOutside(extractionYieldPercent, RecommendedExtractionYieldPercent - ExtractionTolerance,
                    RecommendedExtractionYieldPercent + ExtractionTolerance)
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
            if (ratio < TargetBrewRatio - RatioTolerance)
                return ProfessionalEspressoFeedback.RatioTooShort;
            if (ratio > TargetBrewRatio + RatioTolerance)
                return ProfessionalEspressoFeedback.RatioTooLong;
            if (elapsedSeconds < RecommendedTimeMinSeconds)
                return ProfessionalEspressoFeedback.FlowTooFast;
            if (elapsedSeconds > RecommendedTimeMaxSeconds)
                return ProfessionalEspressoFeedback.FlowTooSlow;
            if (extractionYieldPercent < RecommendedExtractionYieldPercent - ExtractionTolerance)
                return ProfessionalEspressoFeedback.UnderExtracted;
            if (extractionYieldPercent > RecommendedExtractionYieldPercent + ExtractionTolerance)
                return ProfessionalEspressoFeedback.OverExtracted;
            return ProfessionalEspressoFeedback.OnTarget;
        }

        static float DistanceOutside(float value, float min, float max) => Math.Max(0f, Math.Max(min - value, value - max));
        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        static float FiniteOr(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    }
}
