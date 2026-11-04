using System;
using System.Collections.Generic;
using CloudCafe.Coffee;
using CloudCafe.Crafting;

static class ProfessionalEspressoTests
{
    static int count;

    static readonly string[] CatalogBeanIds =
    {
        "batian", "bourbon", "catuai", "caturra", "gesha", "kilimanjaro_bourbon",
        "maragogipe", "marsellesa", "mundo_novo", "pacamara", "pache", "parainema",
        "ruiru_11", "sl28", "sl34", "typica", "villa_sarchi", "yirgacheffe_kurume"
    };

    static void Check(bool condition, string description)
    {
        count++;
        if (!condition)
            throw new Exception(description);
    }

    static float TimeToTarget(ProfessionalEspressoModel model,
        float beverageTarget = ProfessionalEspressoModel.TargetBeverageGrams)
    {
        const float step = .02f;
        float beverage = 0f;
        for (int tick = 1; tick * step < ProfessionalEspressoModel.ShotLimitSeconds; tick++)
        {
            float time = tick * step;
            beverage += model.FlowGramsPerSecond(time - step / 2f) * step;
            if (beverage >= beverageTarget)
                return time;
        }
        return ProfessionalEspressoModel.ShotLimitSeconds;
    }

    static void CheckBeanProfiles()
    {
        var recommendations = new HashSet<(int, float, float)>();
        foreach (string id in CatalogBeanIds)
        {
            EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(id);
            Check(profile.BeanId == id, id + " has a dedicated recommendation");
            Check(recommendations.Add((profile.GrindSetting, profile.TemperatureCelsius, profile.BrewRatio)),
                id + " has a distinct combination of grind, temperature and ratio");
            Check(profile.GrindSetting >= ProfessionalEspressoModel.MinGrindSetting &&
                profile.GrindSetting <= ProfessionalEspressoModel.MaxGrindSetting &&
                profile.TemperatureCelsius >= ProfessionalEspressoModel.MinTemperatureCelsius &&
                profile.TemperatureCelsius <= ProfessionalEspressoModel.MaxTemperatureCelsius &&
                profile.BrewRatio > 0f && profile.BrewRatio * ProfessionalEspressoModel.MaxDoseGrams < 60f,
                id + " recommendation fits the machine and cup");
            Check(!string.IsNullOrWhiteSpace(profile.GrindReasonKo) &&
                !string.IsNullOrWhiteSpace(profile.TemperatureReasonKo) &&
                !string.IsNullOrWhiteSpace(profile.RatioReasonKo),
                id + " explains all three recommendations");

            var model = RecommendedModel(profile);
            float targetMass = ProfessionalEspressoModel.TargetDoseGrams * profile.BrewRatio;
            float time = TimeToTarget(model, targetMass);
            ProfessionalEspressoResult shot = model.Evaluate(targetMass, time);
            Check(model.TargetBrewRatio == profile.BrewRatio &&
                time >= model.RecommendedTimeMinSeconds && time <= model.RecommendedTimeMaxSeconds,
                id + " recommended grind reaches its target ratio within the modeled time window");
            Check(shot.Score >= 98 && shot.Feedback == ProfessionalEspressoFeedback.OnTarget,
                id + " receives full or near-full credit for its own calibrated recipe");
        }

        EspressoBeanProfile light = EspressoBeanProfiles.ForBean("yirgacheffe_kurume");
        EspressoBeanProfile medium = EspressoBeanProfiles.ForBean("mundo_novo");
        Check(light.RoastKo != medium.RoastKo && light.RoastExtractionFactor < medium.RoastExtractionFactor,
            "roast levels differ in modeled extractability");
        Check(light.TemperatureCelsius > medium.TemperatureCelsius && light.BrewRatio > medium.BrewRatio,
            "the lighter floral profile starts hotter and longer than the medium roast");
        var lightRecipe = RecommendedModel(light);
        var mediumRecipe = RecommendedModel(medium);
        float longerMass = ProfessionalEspressoModel.TargetDoseGrams * light.BrewRatio;
        float longerTime = TimeToTarget(lightRecipe, longerMass);
        Check(lightRecipe.Evaluate(longerMass, longerTime).Feedback == ProfessionalEspressoFeedback.OnTarget &&
            mediumRecipe.Evaluate(longerMass, longerTime).Feedback == ProfessionalEspressoFeedback.RatioTooLong,
            "the same long output is on target for the floral profile and too long for the medium roast");

        var cooler = new ProfessionalEspressoModel(18f, light.GrindSetting, 1f, light.TemperatureCelsius - 1f,
            beanProfile: light);
        Check(cooler.TemperatureCelsius < lightRecipe.TemperatureCelsius &&
            cooler.Evaluate(longerMass, longerTime).TdsPercent < lightRecipe.Evaluate(longerMass, longerTime).TdsPercent,
            "a manual temperature override affects extraction despite the selected recommendation");

        EspressoBeanProfile fallback = EspressoBeanProfiles.ForBean("unknown_test_bean");
        Check(fallback.BeanId == null && fallback.GrindSetting == ProfessionalEspressoModel.NominalGrindSetting &&
            fallback.TemperatureCelsius == ProfessionalEspressoModel.TargetTemperatureCelsius && fallback.BrewRatio == 2f,
            "missing IDs preserve the neutral reference recipe");
    }

    static ProfessionalEspressoModel RecommendedModel(EspressoBeanProfile profile)
    {
        return new ProfessionalEspressoModel(ProfessionalEspressoModel.TargetDoseGrams, profile.GrindSetting, 1f,
            profile.TemperatureCelsius, beanProfile: profile);
    }

    static void CheckRoastProfiles()
    {
        EspressoRoast[] roasts = { EspressoRoast.Light, EspressoRoast.Medium, EspressoRoast.Dark };
        var recommendationsByRoast = new HashSet<(EspressoRoast, int, float, float)>();
        foreach (string id in CatalogBeanIds)
        {
            var grindSettings = new HashSet<int>();
            var temperatures = new HashSet<float>();
            var ratios = new HashSet<float>();
            var grindReasons = new HashSet<string>();
            var temperatureReasons = new HashSet<string>();
            var ratioReasons = new HashSet<string>();
            float previousExtractionFactor = 0f;
            foreach (EspressoRoast roast in roasts)
            {
                EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(id, roast);
                string description = id + " " + roast;
                Check(profile.BeanId == id && profile.Roast == roast &&
                    profile.RoastKo == EspressoBeanProfiles.RoastNameKo(roast),
                    description + " retains the requested bean and roast");
                Check(grindSettings.Add(profile.GrindSetting) && temperatures.Add(profile.TemperatureCelsius) &&
                    ratios.Add(profile.BrewRatio), description + " changes all three settings for this bean");
                Check(recommendationsByRoast.Add((roast, profile.GrindSetting, profile.TemperatureCelsius, profile.BrewRatio)),
                    description + " remains distinct from other beans at the same roast");
                Check(!string.IsNullOrWhiteSpace(profile.GrindReasonKo) && grindReasons.Add(profile.GrindReasonKo) &&
                    !string.IsNullOrWhiteSpace(profile.TemperatureReasonKo) && temperatureReasons.Add(profile.TemperatureReasonKo) &&
                    !string.IsNullOrWhiteSpace(profile.RatioReasonKo) && ratioReasons.Add(profile.RatioReasonKo),
                    description + " has different reasons for all three recommendations");
                Check(profile.RoastExtractionFactor > previousExtractionFactor,
                    description + " applies the roast's modeled extractability");
                previousExtractionFactor = profile.RoastExtractionFactor;
                Check(profile.GrindSetting >= ProfessionalEspressoModel.MinGrindSetting &&
                    profile.GrindSetting <= ProfessionalEspressoModel.MaxGrindSetting &&
                    profile.TemperatureCelsius >= ProfessionalEspressoModel.MinTemperatureCelsius &&
                    profile.TemperatureCelsius <= ProfessionalEspressoModel.MaxTemperatureCelsius &&
                    profile.BrewRatio > 0f && profile.BrewRatio * ProfessionalEspressoModel.MaxDoseGrams < 60f,
                    description + " recommendation fits the machine and cup");

                ProfessionalEspressoModel model = RecommendedModel(profile);
                float targetMass = ProfessionalEspressoModel.TargetDoseGrams * profile.BrewRatio;
                float time = TimeToTarget(model, targetMass);
                Check(time < ProfessionalEspressoModel.ShotLimitSeconds && model.TargetBrewRatio == profile.BrewRatio &&
                    time >= model.RecommendedTimeMinSeconds && time <= model.RecommendedTimeMaxSeconds,
                    description + " can reach its own ratio and time targets");
                ProfessionalEspressoResult shot = model.Evaluate(targetMass, time);
                Check(shot.Score >= 98 && shot.Feedback == ProfessionalEspressoFeedback.OnTarget,
                    description + " earns credit for its own calibration");
            }

            var lightShot = new ProfessionalEspressoModel(18f, 5, 1f,
                beanProfile: EspressoBeanProfiles.ForBean(id, EspressoRoast.Light));
            var darkShot = new ProfessionalEspressoModel(18f, 5, 1f,
                beanProfile: EspressoBeanProfiles.ForBean(id, EspressoRoast.Dark));
            Check(darkShot.Evaluate(36f, 28f).TdsPercent > lightShot.Evaluate(36f, 28f).TdsPercent,
                id + " roast changes simulated extraction at the same machine settings");
        }
    }

    static void Main()
    {
        CheckBeanProfiles();
        CheckRoastProfiles();
        CheckCustomerOrder();
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

    static void CheckCustomerOrder()
    {
        var order = new EspressoCustomerOrder("gesha", EspressoRoast.Dark);
        Check(order.HasRequest && order.BeanId == "gesha" && order.Roast == EspressoRoast.Dark,
            "order retains both selected requirements");
        Check(order.Match("gesha", EspressoRoast.Dark) == EspressoOrderMatch.Match,
            "matching bean and roast fulfills the order");
        Check(order.Match("mundo_novo", EspressoRoast.Dark) == EspressoOrderMatch.WrongBean,
            "matching roast alone does not fulfill the order");
        Check(order.Match("gesha", EspressoRoast.Light) == EspressoOrderMatch.WrongRoast,
            "matching bean alone does not fulfill the order");
        Check(order.Match("mundo_novo", EspressoRoast.Light) == EspressoOrderMatch.WrongBeanAndRoast,
            "both wrong requirements are reported together");
        Check(default(EspressoCustomerOrder).Match("gesha", EspressoRoast.Dark) == EspressoOrderMatch.None,
            "an empty order is not treated as served");
    }
}
