using System.Collections;
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
            OpenDifficultyMenu();
            Check(professionalMenuOpen && !draftProfessional, "difficulty menu opens before dosing");
            Check(TryApplyDifficulty(true, 5, 93, 9), "apply professional difficulty");
            CloseProfessionalOverlay();
            Check(professionalMode && Model.ManualDose, "manual grinder selected");

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
            yield return new WaitForSeconds(.2f);
            TogglePause();
            float pausedAt = Brew.ElapsedSeconds;
            yield return new WaitForSeconds(.2f);
            Check(Brew.ElapsedSeconds == pausedAt && Brew.Running, "pause freezes shot without finalizing");
            TogglePause();
            SwitchBrew();
            Check(!Brew.Running && !Brew.ShotFinalized && Brew.ElapsedSeconds == 0, "preinfusion can restart");
            SwitchBrew();

            deadline = Time.realtimeSinceStartup + 45f;
            while (Brew.InCup < 36 && Time.realtimeSinceStartup < deadline) yield return null;
            Check(Brew.Running && Brew.InCup >= 36, "pump continues past target mass");
            SwitchBrew();
            deadline = Time.realtimeSinceStartup + 2f;
            while (!professionalReportOpen && Time.realtimeSinceStartup < deadline) yield return null;
            Check(Brew.Complete && professionalReportOpen, "settled manual shot opens report");
            ProfessionalEspressoResult result = professionalShot.Evaluate(Brew.InCup, Brew.ElapsedSeconds);
            Check(result.Score >= 90 && result.ElapsedSeconds >= 25 && result.ElapsedSeconds <= 30,
                "nominal shot has expected report and timing");
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
