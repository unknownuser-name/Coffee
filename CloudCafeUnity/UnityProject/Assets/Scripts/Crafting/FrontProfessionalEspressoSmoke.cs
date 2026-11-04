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
            EspressoBeanProfile defaultRecipe = EspressoBeanProfiles.ForBean(SelectedCoffeeBean.Id);
            Check(professionalGrind == defaultRecipe.GrindSetting && professionalTemperature == defaultRecipe.TemperatureCelsius,
                "professional starts at the selected bean's settings");

            Check(TrySelectCoffeeBean("mundo_novo"), "select a medium-roast bean");
            EspressoBeanProfile mediumRecipe = EspressoBeanProfiles.ForBean(SelectedCoffeeBean.Id);
            Check(mediumRecipe.BrewRatio < defaultRecipe.BrewRatio &&
                professionalGrind == mediumRecipe.GrindSetting && professionalTemperature == mediumRecipe.TemperatureCelsius &&
                professionalTemperature < defaultRecipe.TemperatureCelsius,
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

            Check(TrySelectCoffeeBean("gesha"), "select Panama Gesha for the shot");
            EspressoBeanProfile shotProfile = EspressoBeanProfiles.ForBean(SelectedCoffeeBean.Id);
            Check(professionalGrind == shotProfile.GrindSetting && professionalTemperature == shotProfile.TemperatureCelsius &&
                shotProfile.BrewRatio != mediumRecipe.BrewRatio && shotProfile.BrewRatio != 2f,
                "next bean also applies its own recipe");

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
            Check(professionalShot.TargetBrewRatio == shotProfile.BrewRatio,
                "pump uses the selected bean recipe as the scoring target");
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
            CloseProfessionalOverlay();
            Check(GrabCup() && latteActive, "professional shot can continue to latte");
            ResetStation();
            Check(professionalMode && Model.ManualDose && !Model.DoseCommitted && !Brew.IsProfessional,
                "reset clears shot while retaining difficulty");
            Check(TryApplyDifficulty(false, 5, 93, 9) && !Model.ManualDose, "return to basic difficulty");
            Debug.Log("PROFESSIONAL_ESPRESSO_SMOKE_PASSED");
            Application.Quit(0);
        }
    }
}
