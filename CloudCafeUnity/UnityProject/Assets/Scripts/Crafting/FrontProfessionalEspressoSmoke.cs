using System.Collections;
using CloudCafe.Coffee;
using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private bool professionalSmoke;

        private IEnumerator ProfessionalEspressoSmoke()
        {
            yield return null;
            Check(!professionalMode && !Model.ManualDose, "starts in basic difficulty");
            Check(coffeeBeanCatalog != null && coffeeBeanCatalog.Beans.Length == 18 &&
                SelectedCoffeeBean != null && SelectedCoffeeBean.Id == "yirgacheffe_kurume",
                "professional station loads all beans and the default origin");
            foreach (CoffeeBeanDefinition bean in coffeeBeanCatalog.Beans)
            {
                EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(bean.Id);
                Check(profile.BeanId == bean.Id && profile.RoastKo == bean.Roast,
                    "recommendation matches registered bean and roast: " + bean.Id);
            }

            OpenDifficultyMenu();
            Check(professionalMenuOpen && !draftProfessional, "difficulty menu opens before dosing");
            Check(TryApplyDifficulty(true, draftGrind, draftTemperature, draftPressure),
                "apply professional difficulty with recommended settings");
            CloseProfessionalOverlay();
            Check(professionalMode && Model.ManualDose, "manual grinder selected");
            Check(espressoOrder.HasRequest, "professional station starts with a customer request");
            Check(SelectedCoffeeRoast == EspressoRoast.Medium, "station starts at medium roast");
            EspressoBeanProfile defaultRecipe = EspressoBeanProfiles.ForBean(SelectedCoffeeBean.Id, SelectedCoffeeRoast);
            Check(professionalGrind == defaultRecipe.GrindSetting && professionalTemperature == defaultRecipe.TemperatureCelsius,
                "professional starts at the selected bean's settings");

            Check(TrySelectCoffeeBean("yirgacheffe_kurume", EspressoRoast.Light), "select a lighter roast of the same bean");
            EspressoBeanProfile lightRecipe = EspressoBeanProfiles.ForBean(SelectedCoffeeBean.Id, EspressoRoast.Light);
            Check(SelectedCoffeeRoast == EspressoRoast.Light && professionalBeanRecipe.Roast == EspressoRoast.Light &&
                professionalGrind == lightRecipe.GrindSetting && professionalTemperature == lightRecipe.TemperatureCelsius &&
                professionalBeanRecipe.BrewRatio == lightRecipe.BrewRatio &&
                professionalBeanRecipe.GrindReasonKo != defaultRecipe.GrindReasonKo &&
                professionalBeanRecipe.TemperatureReasonKo != defaultRecipe.TemperatureReasonKo &&
                professionalBeanRecipe.RatioReasonKo != defaultRecipe.RatioReasonKo,
                "switching the same bean's roast applies its own settings and three explanations");

            Check(TrySelectCoffeeBean("mundo_novo", EspressoRoast.Medium), "select a different bean in medium roast");
            EspressoBeanProfile mediumRecipe = EspressoBeanProfiles.ForBean(SelectedCoffeeBean.Id, SelectedCoffeeRoast);
            Check(mediumRecipe.BrewRatio < lightRecipe.BrewRatio &&
                professionalGrind == mediumRecipe.GrindSetting && professionalTemperature == mediumRecipe.TemperatureCelsius &&
                professionalTemperature < lightRecipe.TemperatureCelsius,
                "selecting a medium roast applies its own shorter and cooler recipe");

            int manualGrind = mediumRecipe.GrindSetting - 1;
            float manualTemperature = mediumRecipe.TemperatureCelsius + .5f;
            Check(TryApplyDifficulty(true, manualGrind, manualTemperature, draftPressure),
                "manually dial in the same bean");
            Check(TrySelectCoffeeBean("mundo_novo") && professionalGrind == manualGrind &&
                professionalTemperature == manualTemperature,
                "reselecting a bean preserves its manual settings");
            OpenDifficultyMenu();
            SetDraftBeanRecommendation();
            Check(draftGrind == mediumRecipe.GrindSetting && draftTemperature == mediumRecipe.TemperatureCelsius &&
                professionalGrind == manualGrind && professionalTemperature == manualTemperature,
                "restoring the recommendation edits only the pending menu settings");
            CloseProfessionalOverlay();

            Check(TrySetEspressoOrder("gesha", EspressoRoast.Dark, CoffeeDrink.Espresso) &&
                espressoOrder.BeanId == "gesha" && espressoOrder.Roast == EspressoRoast.Dark &&
                espressoOrder.Drink == CoffeeDrink.Espresso &&
                SelectedCoffeeBean.Id == "mundo_novo" && SelectedCoffeeRoast == EspressoRoast.Medium,
                "customer drink and ingredient requirements are separate from barista preparation");
            Check(TrySelectCoffeeBean("gesha", EspressoRoast.Dark), "select dark roast Panama Gesha for the shot");
            EspressoBeanProfile shotProfile = EspressoBeanProfiles.ForBean(SelectedCoffeeBean.Id, SelectedCoffeeRoast);
            Check(professionalGrind == shotProfile.GrindSetting && professionalTemperature == shotProfile.TemperatureCelsius &&
                professionalBeanRecipe.Roast == EspressoRoast.Dark && professionalBeanRecipe.BrewRatio == shotProfile.BrewRatio,
                "next bean applies its own dark-roast recipe");

            Check(Grab(), "lift empty professional basket");
            yield return Settle();
            Reach(art.Dock);
            Drop();
            yield return Settle();
            Check(!CommitProfessionalDose(), "cannot commit an empty basket");
            Switch();
            float deadline = Time.realtimeSinceStartup + 8f;
            while (Model.DoseInBasketOrFlight < 18 && Time.realtimeSinceStartup < deadline) yield return null;
            Check(Model.Running, "grinder has no automatic 18g cutoff");
            Switch();
            deadline = Time.realtimeSinceStartup + 2f;
            while (!Model.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Check(Model.Ready && Model.Grounds >= 18 && Model.Grounds < 19, "manual dose settles");
            Check(!TryApplyDifficulty(false, 5, 93, 9), "cannot change difficulty mid-shot");
            Check(!TrySelectCoffeeBean("mundo_novo"), "cannot change bean recipe after dosing");
            Check(!TrySelectCoffeeBean("gesha", EspressoRoast.Light) && SelectedCoffeeRoast == EspressoRoast.Dark,
                "cannot change the current bean's roast after dosing");
            Check(!TrySetEspressoOrder("gesha", EspressoRoast.Light, CoffeeDrink.Americano) &&
                espressoOrder.Roast == EspressoRoast.Dark && espressoOrder.Drink == CoffeeDrink.Espresso,
                "cannot change the customer request after dosing");
            Check(Grab(), "lift filled professional basket");
            yield return Settle();
            Reach(art.Mat);
            Drop();
            yield return Settle();
            Check(!PickPrep(1), "leveling requires dose commitment");
            Check(CommitProfessionalDose(), "confirm weighed dose on mat");

            Check(PickPrep(1), "pick professional leveler");
            yield return Settle();
            ReachPrep(art.Mat);
            DropPrep();
            yield return Settle();
            RotatePrep(270);
            EndRotate();
            yield return Settle();
            Check(PickPrep(2), "pick professional tamper");
            yield return Settle();
            ReachPrep(art.Mat);
            DropPrep();
            yield return Settle();
            Check(Prep.BeginTamp(), "tamp prepared dose");
            for (int i = 0; i < 6; i++) PressPrep(1, .05f);
            EndPress();
            yield return Settle();
            Check(Prep.Complete && brewView, "tamp completes before brewing");

            Check(Grab(), "lift tamped professional basket");
            yield return Settle();
            Reach(art.GroupSeat);
            Drop();
            yield return Settle();
            SwitchBrew();
            Check(!Brew.Running, "unlocked pump is blocked");
            TurnBrew(40);
            Check(GrabCup(), "move professional shot cup");
            yield return Settle();
            ReachCup(art.CupSeat);
            DropCup();
            yield return Settle();
            SwitchBrew();
            Check(Brew.Running && Brew.IsProfessional, "start professional pump");
            Check(professionalShot.TargetBrewRatio == shotProfile.BrewRatio &&
                professionalShotBeanRecipe.BeanId == "gesha" && professionalShotBeanRecipe.Roast == EspressoRoast.Dark &&
                professionalShotBeanRecipe.RatioReasonKo == shotProfile.RatioReasonKo,
                "pump captures the selected bean and roast recipe for scoring and the shot report");
            Check(!TryServeEspressoOrder() && !espressoOrderSubmitted && !espressoOrderServed,
                "customer cannot be served an unfinished shot");
            yield return new WaitForSeconds(.2f);
            TogglePause();
            float pausedAt = Brew.ElapsedSeconds;
            yield return new WaitForSeconds(.2f);
            Check(Brew.ElapsedSeconds == pausedAt && Brew.Running, "pause freezes shot without finalizing");
            TogglePause();
            SwitchBrew();
            Check(!Brew.Running && !Brew.ShotFinalized && Brew.ElapsedSeconds == 0, "preinfusion can restart");
            SwitchBrew();

            float targetMass = Model.Grounds * shotProfile.BrewRatio;
            deadline = Time.realtimeSinceStartup + 65f;
            while (Brew.InCup < targetMass && Time.realtimeSinceStartup < deadline) yield return null;
            Check(Brew.Running && Brew.InCup >= targetMass, "pump continues past the selected bean's target mass");
            SwitchBrew();
            deadline = Time.realtimeSinceStartup + 2f;
            while (!professionalReportOpen && Time.realtimeSinceStartup < deadline) yield return null;
            Check(Brew.Complete && professionalReportOpen, "settled manual shot opens report");
            ProfessionalEspressoResult result = professionalShot.Evaluate(Brew.InCup, Brew.ElapsedSeconds);
            Check(result.Score >= 90 && Mathf.Abs(result.BrewRatio - shotProfile.BrewRatio) < .06f &&
                result.ElapsedSeconds >= professionalShot.RecommendedTimeMinSeconds &&
                result.ElapsedSeconds <= professionalShot.RecommendedTimeMaxSeconds,
                "shot report scores against the selected bean's ratio and timing");
            Check(TryChooseDrink(CoffeeDrink.Espresso) && finishedDrink == CoffeeDrink.Espresso,
                "the finished shot can be chosen as espresso");
            Check(espressoOrderMatch == EspressoOrderMatch.Match && TryServeEspressoOrder(CoffeeDrink.Espresso) &&
                espressoOrderSubmitted && espressoOrderServed, "finished shot serves the matching customer request");
            CloseProfessionalOverlay();
            Check(!GrabCup() && !latteActive, "a served espresso cannot also be used for latte");
            ResetStation();
            Check(professionalMode && Model.ManualDose && !Model.DoseCommitted && !Brew.IsProfessional,
                "reset clears shot while retaining difficulty");
            Check(espressoOrder.HasRequest && espressoOrder.BeanId == "gesha" &&
                espressoOrder.Roast == EspressoRoast.Dark && !espressoOrderSubmitted && espressoOrderServed &&
                espressoOrderMatch == EspressoOrderMatch.None,
                "station reset keeps the completed request while clearing per-shot results");
            Check(TryAdvanceEspressoOrder() && espressoOrder.HasRequest &&
                (espressoOrder.BeanId != "gesha" || espressoOrder.Roast != EspressoRoast.Dark) && !espressoOrderServed,
                "the next customer supplies a new bean and roast request");
            CheckOtherDrinkRoutes();
            ResetStation();
            Check(TryApplyDifficulty(false, 5, 93, 9) && !Model.ManualDose, "return to basic difficulty");
            SeedDrinkShot();
            Check(!Brew.IsProfessional && TryChooseDrink(CoffeeDrink.Americano) && Americano.Active &&
                Mathf.Abs(Americano.TargetWaterGrams - 108f) < .001f,
                "basic mode also offers Americano from its 36g shot");
            Americano.Step(.05f, true);
            Check(TryFinishAmericano() && finishedDrink == CoffeeDrink.Americano,
                "basic mode can finish a drink without a customer order");
            Debug.Log("PROFESSIONAL_ESPRESSO_SMOKE_PASSED");
            Application.Quit(0);
        }

        private void CheckOtherDrinkRoutes()
        {
            Check(TrySetEspressoOrder("gesha", EspressoRoast.Dark, CoffeeDrink.Americano),
                "set a custom Americano order");
            SeedDrinkShot();
            Check(!TryServeEspressoOrder(CoffeeDrink.Americano) && !espressoOrderSubmitted,
                "a finished espresso shot alone is not a finished Americano");
            Check(TryChooseDrink(CoffeeDrink.Espresso) && !TryServeEspressoOrder(CoffeeDrink.Espresso) &&
                espressoOrderSubmitted && !espressoOrderServed && espressoOrderMatch == EspressoOrderMatch.WrongDrink,
                "the customer rejects espresso when the requested drink is Americano");

            ResetStation();
            Check(espressoOrder.Drink == CoffeeDrink.Americano && !espressoOrderSubmitted && !espressoOrderServed,
                "retry keeps the unfulfilled Americano order");
            SeedDrinkShot();
            CloseProfessionalOverlay();
            Check(GrabCup() && professionalReportOpen && !latteActive && finishedDrink == null,
                "clicking a finished shot presents drink choices");
            float espressoMass = Brew.InCup;
            Check(TryChooseDrink(CoffeeDrink.Americano) && Americano.Active &&
                Mathf.Abs(Americano.EspressoGrams - espressoMass) < .001f &&
                Mathf.Abs(Americano.TargetWaterGrams - 3f * espressoMass) < .001f,
                "Americano uses the actual shot mass and calculates its water target");
            Check(!TryFinishAmericano() && !TryServeEspressoOrder(CoffeeDrink.Americano) && !espressoOrderSubmitted,
                "Americano requires hot water before finish and service");
            for (int i = 0; i < 400 && Americano.WaterGrams < Americano.TargetWaterGrams; i++)
                Americano.Step(.05f, true);
            float waterMass = Americano.WaterGrams;
            Americano.Step(.05f, false);
            Check(waterMass >= Americano.TargetWaterGrams && Americano.WaterGrams == waterMass &&
                Mathf.Abs(Americano.TotalGrams - espressoMass - waterMass) < .001f,
                "holding adds water, releasing stops it, and the finished mass includes the original shot");
            Check(TryFinishAmericano() && Americano.Complete && finishedDrink == CoffeeDrink.Americano &&
                !IsDrinkReadyToServe(CoffeeDrink.Espresso),
                "finishing an Americano makes only the completed drink ready");
            Check(TryServeEspressoOrder(CoffeeDrink.Americano) && espressoOrderServed &&
                !TryServeEspressoOrder(CoffeeDrink.Americano),
                "the matching Americano can be served once");

            ResetStation();
            Check(!Americano.Active && !Americano.Complete && finishedDrink == null,
                "reset clears the completed drink and hot water");
            Check(TrySetEspressoOrder("gesha", EspressoRoast.Dark, CoffeeDrink.Latte),
                "set a custom latte order");
            SeedDrinkShot();
            Check(TryChooseDrink(CoffeeDrink.Latte) && latteActive && !TryFinishLatteDrink() &&
                !TryServeEspressoOrder(CoffeeDrink.Latte) && !espressoOrderSubmitted,
                "latte still opens milk pouring and cannot be served before completion");
            for (int i = 0; i < 240 && !Latte.Complete; i++) Latte.Step(.05f, true, 0, 0, -1f);
            Check(Latte.Complete && Latte.Marks.Count > 0 && TryFinishLatteDrink() &&
                finishedDrink == CoffeeDrink.Latte && TryServeEspressoOrder(CoffeeDrink.Latte),
                "latte art still works and the completed latte satisfies its own order");
        }

        private void SeedDrinkShot()
        {
            // The first shot exercises the complete workstation. Use the same pump and
            // report callbacks here to reach the other drink choices without replaying prep.
            Model.Grounds = 18f;
            Check(Brew.Insert(true), "seed an inserted basket for another drink");
            Brew.Turn(40);
            Brew.CupDocked = true;
            SwitchBrew();
            for (int i = 0; i < 1200 && Brew.Running && (!Brew.IsProfessional || Brew.InCup < 30f); i++)
                StepStationBrew(.05f);
            if (Brew.IsProfessional)
            {
                Check(Brew.Running && Brew.InCup >= 30f, "seed a stable shot using the selected recipe");
                SwitchBrew();
            }
            for (int i = 0; i < 8; i++) StepStationBrew(.05f);
            Check(Brew.Complete && professionalReportOpen, "another finished shot opens drink choices");
        }
    }
}
