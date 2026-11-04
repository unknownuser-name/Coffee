using System;
using CloudCafe.Crafting;

static class ProfessionalEspressoTests
{
    static int count;

    static void Check(bool condition, string description)
    {
        count++;
        if (!condition)
            throw new Exception(description);
    }

    static float TimeToTarget(ProfessionalEspressoModel model)
    {
        const float step = .02f;
        float beverage = 0f;
        for (int tick = 1; tick * step < ProfessionalEspressoModel.ShotLimitSeconds; tick++)
        {
            float time = tick * step;
            beverage += model.FlowGramsPerSecond(time - step / 2f) * step;
            if (beverage >= ProfessionalEspressoModel.TargetBeverageGrams)
                return time;
        }
        return ProfessionalEspressoModel.ShotLimitSeconds;
    }

    static void Main()
    {
        var nominal = new ProfessionalEspressoModel(18f, 5, 1f);
        Check(nominal.FlowGramsPerSecond(0f) == 0f && nominal.FlowGramsPerSecond(3f) == 0f, "preinfusion produces no cup flow");
        Check(nominal.FlowGramsPerSecond(3.5f) > 0f && nominal.FlowGramsPerSecond(3.5f) < nominal.FlowGramsPerSecond(4f), "flow ramps after preinfusion");
        Check(nominal.FlowGramsPerSecond(60f) == 0f, "shot limit stops flow");

        float time = TimeToTarget(nominal);
        var result = nominal.Evaluate(36f, time);
        Check(time >= 25f && time <= 30f, "18 g nominal shot reaches 36 g at 25–30 s");
        Check(result.BrewRatio == 2f && result.Score == 100 && result.Feedback == ProfessionalEspressoFeedback.OnTarget, "nominal house recipe scores full marks");
        Check(result.ExtractionYieldPercent >= 18f && result.ExtractionYieldPercent <= 22f, "nominal extraction range");
        Check(result.TdsPercent >= 9f && result.TdsPercent <= 11f, "nominal TDS range");
        Check(Math.Abs(result.ExtractionYieldPercent - 36f * result.TdsPercent / 18f) < .0001f, "EY is beverage mass times TDS divided by dose");
        Check(Math.Abs(nominal.DissolvedGrams(36f, time) - 36f * result.TdsPercent / 100f) < .0001f, "reported TDS matches dissolved mass");
        Check(nominal.Evaluate(1f, 4.5f).TdsPercent > result.TdsPercent, "first drips are stronger than the complete shot");

        var finest = new ProfessionalEspressoModel(18f, 1, 1f);
        var coarsest = new ProfessionalEspressoModel(18f, 9, 1f);
        Check(TimeToTarget(coarsest) < time && time < TimeToTarget(finest), "coarser grind reaches the target faster");
        Check(finest.Evaluate(36f, TimeToTarget(finest)).ExtractionYieldPercent < result.ExtractionYieldPercent, "very fine grind loses extraction through uneven flow");
        Check(coarsest.Evaluate(36f, TimeToTarget(coarsest)).ExtractionYieldPercent < result.ExtractionYieldPercent, "very coarse grind loses extraction through rapid flow");
        Check(finest.ChannelingRisk > nominal.ChannelingRisk, "very fine grounds increase modeled channeling risk");
        var uneven = new ProfessionalEspressoModel(18f, 5, .35f);
        Check(uneven.Evaluate(36f, time).Feedback == ProfessionalEspressoFeedback.UnevenPuck, "uneven preparation receives corrective feedback");
        Check(uneven.Evaluate(36f, time).ExtractionYieldPercent < result.ExtractionYieldPercent, "uneven wetting reduces extraction");

        var hotter = new ProfessionalEspressoModel(18f, 5, 1f, 96f);
        var higherPressure = new ProfessionalEspressoModel(18f, 5, 1f, 93f, 9.5f);
        var heavierDose = new ProfessionalEspressoModel(22f, 5, 1f);
        Check(hotter.Evaluate(36f, time).TdsPercent > result.TdsPercent, "hotter water raises modeled extraction at equal mass and time");
        Check(higherPressure.FlowGramsPerSecond(10f) > nominal.FlowGramsPerSecond(10f), "higher pressure increases flow");
        Check(heavierDose.FlowGramsPerSecond(10f) < nominal.FlowGramsPerSecond(10f), "heavier dose slows flow");
        Check(heavierDose.Evaluate(44f, 27f).Feedback == ProfessionalEspressoFeedback.DoseOffTarget, "incorrect dose is visible even at the correct ratio");
        Check(nominal.Evaluate(24f, 26f).Feedback == ProfessionalEspressoFeedback.RatioTooShort, "short beverage ratio feedback");
        Check(nominal.Evaluate(48f, 26f).Feedback == ProfessionalEspressoFeedback.RatioTooLong, "long beverage ratio feedback");
        Check(nominal.Evaluate(36f, 18f).Feedback == ProfessionalEspressoFeedback.FlowTooFast, "fast flow feedback");
        Check(nominal.Evaluate(36f, 40f).Feedback == ProfessionalEspressoFeedback.FlowTooSlow, "slow flow feedback");
        Check(coarsest.Evaluate(36f, 25f).Feedback == ProfessionalEspressoFeedback.UnderExtracted, "under-extraction feedback");
        Check(new ProfessionalEspressoModel(18f, 4, 1f, 96f).Evaluate(36f, 30f).Feedback
            == ProfessionalEspressoFeedback.OverExtracted, "over-extraction feedback");

        var clamped = new ProfessionalEspressoModel(99f, -3, 2f, 88f, 11f);
        Check(clamped.DoseGrams == 22f && clamped.GrindSetting == 1 && clamped.PuckUniformity == 1f, "dose, grind and preparation stay inside game settings");
        Check(clamped.TemperatureCelsius == 90.5f && clamped.PressureBar == 9.5f, "temperature and pressure respect machine limits");
        var empty = nominal.Evaluate(float.NaN, float.PositiveInfinity);
        Check(empty.Feedback == ProfessionalEspressoFeedback.NoBeverage && empty.TdsPercent == 0f && empty.Score == 0, "invalid or empty input yields a safe zero-result shot");

        Console.WriteLine("PROFESSIONAL_ESPRESSO_TESTS_PASSED " + count);
    }
}
