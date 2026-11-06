using System.Collections;
using CloudCafe.Coffee;
using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private IEnumerator StepGuidanceSmoke()
        {
            yield return null;
            Check(!professionalMode && coffeeBeanCatalog != null, "step guidance starts with the basic station and bean catalog");
            StepSmokeTarget("grinder.pick", art.Tool);
            yield return Shot("step_00-grinder-pick");

            TogglePause();
            StepSmokeHidden("pause suppresses pickup glow");
            TogglePause();
            StepSmokeTarget("grinder.pick", art.Tool);
            OpenDifficultyMenu();
            Check(professionalMenuOpen, "open the real difficulty overlay");
            StepSmokeHidden("difficulty overlay suppresses pickup glow");
            CloseProfessionalOverlay();
            Check(ShowCoffeeBeanSelection(), "open the real bean overlay");
            StepSmokeHidden("bean overlay suppresses pickup glow");
            CloseCoffeeBeanBrowser();

            Check(Grab(), "guidance picks up the actual empty portafilter");
            StepSmokeHidden("tool motion suppresses guidance glow");
            yield return Settle();
            StepSmokePlace("grinder.place", art.Dock);
            Reach(art.Dock);
            yield return Shot("step_01-grinder-place");
            Drop();
            yield return Settle();
            StepSmokeTarget("grinder.start", art.Switch);
            Switch();
            StepSmokeHint("grinder.wait");
            for (int i = 0; i < 120 && Model.Running; i++) Model.Step(.05f);
            Check(!Model.Running && Model.Flying > 0, "basic grinder stops with a final parcel still falling");
            StepSmokeHint("grinder.wait");
            for (int i = 0; i < 8; i++) Model.Step(.05f);
            Check(Model.Ready, "basic dose settles using the actual grinder model");
            StepSmokeTarget("grinder.return.pick", art.Tool);
            Check(Grab(), "guidance lifts the full basket");
            yield return Settle();
            StepSmokePlace("grinder.return.place", art.Mat);
            Reach(art.Mat);
            Drop();
            yield return Settle();
            yield return StepSmokePreparePuck();

            StepSmokeTarget("machine.pick", art.Tool);
            Check(Grab(), "guidance lifts the prepared basket for the machine");
            yield return Settle();
            StepSmokePlace("machine.place", art.GroupSeat);
            Reach(art.GroupSeat);
            yield return Shot("step_05-machine-place");
            Drop();
            yield return Settle();
            StepSmokeTarget("machine.lock", art.Tool);
            SwitchBrew();
            Check(!Brew.Running, "the guidance does not bypass the machine lock");
            TurnBrew(40);
            StepSmokeTarget("cup.pick", art.Cup);
            Check(GrabCup(), "guidance picks up the empty shot cup");
            yield return Settle();
            StepSmokePlace("cup.place", art.CupSeat);
            ReachCup(art.CupSeat);
            DropCup();
            yield return Settle();
            StepSmokeTarget("brew.start", art.BrewButton);
            SwitchBrew();
            StepSmokeHint("brew.wait");
            for (int i = 0; i < 400 && Brew.Running; i++) StepStationBrew(.05f);
            Check(!Brew.Running && Brew.InFlight > 0, "basic brew leaves the last drops in flight");
            StepSmokeHint("brew.wait");
            for (int i = 0; i < 8; i++) StepStationBrew(.05f);
            Check(Brew.Complete && professionalReportOpen, "finished shot opens the real drink choice overlay");
            StepSmokeButton("drink.choose", new Rect(134, 231, 692, 36));
            StepSmokeHidden("drink choice overlay suppresses world glow");
            Check(TryChooseDrink(CoffeeDrink.Espresso), "choose an actual espresso");
            CloseProfessionalOverlay();
            StepSmokeButton("serve.report");
            Check(TryOpenDrinkReport(), "reopen the finished espresso report");
            StepSmokeHidden("finished espresso report suppresses world glow");

            ResetStation();
            SeedDrinkShot();
            Check(TryChooseDrink(CoffeeDrink.Americano), "choose an actual Americano");
            StepSmokeButton("drink.water", WaterButton);
            yield return Shot("step_06-americano-water");
            for (int i = 0; i < 400 && Americano.WaterGrams < Americano.TargetWaterGrams; i++)
                Americano.Step(.05f, true);
            Americano.Step(.05f, false);
            StepSmokeButton("drink.water.finish", FinishWaterButton);
            Check(TryFinishAmericano(), "finish the actual hot water model");
            StepSmokeHidden("Americano completion report suppresses world glow");
            CloseProfessionalOverlay();
            StepSmokeButton("serve.report");

            ResetStation();
            SeedDrinkShot();
            Check(TryChooseDrink(CoffeeDrink.Latte), "choose an actual latte");
            StepSmokeTarget("drink.milk", art.LattePitcher);
            yield return Shot("step_07-latte-milk");
            for (int i = 0; i < 240 && !Latte.Complete; i++) Latte.Step(.05f, true, 0, 0, -1f);
            Check(Latte.Complete && TryFinishLatteDrink(), "finish the actual milk model");
            StepSmokeHidden("latte completion report suppresses world glow");
            CloseProfessionalOverlay();
            StepSmokeButton("serve.report");

            ResetStation();
            Check(TryApplyDifficulty(true, professionalGrind, professionalTemperature, professionalPressure),
                "guidance switches an empty station into professional mode");
            Check(customerVisit != null && counterView && counterCustomer.Phase == CounterCustomerPhase.Arriving,
                "professional guidance starts a real customer visit");
            StepSmokeHint("order.wait");
            UpdateCounterService(2.1f);
            Check(customerDialogueOpen && counterCustomer.Phase == CounterCustomerPhase.Ordering,
                "customer arrival opens the actual order dialogue");
            StepSmokeButton("order.accept", new Rect(709, 480, 227, 44));
            Check(AcceptCustomerOrder(), "accept the generated customer's request");
            StepSmokeTarget("grinder.pick", art.Tool);
            EnterCounterView();
            StepSmokeButton("counter.return", new Rect(22, 490, 174, 40));
            OpenEspressoOrderMenu();
            Check(customerDialogueOpen, "recalling the waiting customer's order opens its real dialogue");
            StepSmokeButton("counter.return", new Rect(709, 480, 227, 44));
            CloseCustomerDialogue();
            LeaveCounterView();

            // An early stop below 12g must lead back to the grinder, never to prep.
            Check(Grab(), "pick up the professional basket");
            yield return Settle();
            Reach(art.Dock);
            Drop();
            yield return Settle();
            Switch();
            for (int i = 0; i < 12; i++) Model.Step(.05f);
            Switch();
            StepSmokeHint("grinder.wait");
            for (int i = 0; i < 8; i++) Model.Step(.05f);
            Check(!Model.Ready && Model.Grounds > 0 && Model.Grounds < ProfessionalEspressoModel.MinDoseGrams,
                "early stopped professional dose is genuinely below the preparation minimum");
            StepSmokeTarget("grinder.start", art.Switch);
            Check(Grab(), "an underweight dose can be removed from the grinder");
            yield return Settle();
            Reach(art.Mat);
            Drop();
            yield return Settle();
            StepSmokeTarget("grinder.pick", art.Tool);
            Check(!PickPrep(1), "guidance cannot skip an underweight dose into leveling");
            Check(Grab(), "return the underweight basket to its grinder");
            yield return Settle();
            StepSmokePlace("grinder.place", art.Dock);
            Reach(art.Dock);
            Drop();
            yield return Settle();
            Switch();
            for (int i = 0; i < 160 && Model.Running && Model.DoseInBasketOrFlight < 18f; i++) Model.Step(.05f);
            StepSmokeTarget("grinder.stop", art.Switch);
            Switch();
            StepSmokeHint("grinder.wait");
            for (int i = 0; i < 8; i++) Model.Step(.05f);
            StepSmokeTarget("grinder.return.pick", art.Tool);
            Check(Grab(), "lift the weighed professional dose");
            yield return Settle();
            StepSmokePlace("grinder.return.place", art.Mat);
            Reach(art.Mat);
            Drop();
            yield return Settle();
            StepSmokeButton("dose.confirm", EspressoActionButton);
            Check(CommitProfessionalDose(), "confirm the actual professional dose");
            StepSmokeTarget("level.pick", art.Distributor);

            // The first shot exercised every physical prep step. Reuse the pump fixture
            // for the customer route, preserving the accepted customer's identity.
            CustomerVisitModel waitingCustomer = customerVisit;
            ResetStation();
            Check(customerVisit == waitingCustomer && customerVisit.Phase == CustomerVisitPhase.Preparing,
                "retry preserves the waiting customer");
            Check(TrySelectCoffeeBean(espressoOrder.BeanId, espressoOrder.Roast),
                "prepare the customer's real bean and roast before the seeded shot");
            Model.Grounds = 18f;
            Check(Brew.Insert(true), "seed the customer's inserted prepared basket");
            Brew.Turn(40);
            Brew.CupDocked = true;
            StepSmokeTarget("brew.start", art.BrewButton);
            SwitchBrew();
            StepSmokeTarget("brew.stop", art.BrewButton);
            for (int i = 0; i < 4; i++) StepStationBrew(.05f);
            SwitchBrew();
            Check(!Brew.ShotFinalized && Brew.ElapsedSeconds == 0,
                "stopping during preinfusion allows the professional brew to restart");
            StepSmokeTarget("brew.start", art.BrewButton);
            SwitchBrew();
            TogglePause();
            float pausedBrewTime = Brew.ElapsedSeconds;
            StepSmokeHidden("pause suppresses the running professional brew glow");
            yield return new WaitForSeconds(.12f);
            Check(Brew.Running && Brew.ElapsedSeconds == pausedBrewTime,
                "pausing leaves the professional shot running without advancing extraction");
            TogglePause();
            StepSmokeTarget("brew.stop", art.BrewButton);
            float targetShotMass = 18f * professionalBeanRecipe.BrewRatio;
            for (int i = 0; i < 1200 && Brew.Running && Brew.InCup < targetShotMass; i++) StepStationBrew(.05f);
            Check(Brew.Running && Brew.InCup >= targetShotMass, "professional shot reaches its bean's manual stop target");
            SwitchBrew();
            StepSmokeHint("brew.wait");
            for (int i = 0; i < 8; i++) StepStationBrew(.05f);
            Check(Brew.Complete && professionalReportOpen, "professional tail settles before drink selection opens");
            float requestedChoiceX = espressoOrder.Drink == CoffeeDrink.Espresso ? 134 :
                espressoOrder.Drink == CoffeeDrink.Americano ? 367 : 600;
            StepSmokeButton("drink.choose", new Rect(requestedChoiceX, 409, 226, 36));
            Check(TryChooseDrink(espressoOrder.Drink), "choose the requested customer drink");
            FinishCustomerDrink(espressoOrder.Drink);
            StepSmokeButton("serve.report", new Rect(273, 475, 254, 40));
            StepSmokeHidden("customer drink report suppresses world glow");
            Check(BeginCounterHandoff(), "carry the finished drink to the counter");
            StepSmokeTarget("serve.pick", art.CounterCup);
            yield return Shot("step_08-serve-pick");
            Check(GrabServingCup(), "pick up the finished serving cup");
            StepSmokePlace("serve.place", art.CounterReceivePoint);
            MoveServingCup(art.CounterCupHome);
            Check(!DropServingCup() && !espressoOrderSubmitted, "a missed handoff remains unserved");
            StepSmokeTarget("serve.pick", art.CounterCup);
            Check(GrabServingCup(), "pick up the cup after a missed handoff");
            MoveServingCup(art.CounterReceivePoint);
            StepSmokePlace("serve.release", art.CounterReceivePoint);
            yield return Shot("step_09-serve-release");
            Check(DropServingCup() && counterCustomer.Phase == CounterCustomerPhase.Receiving && espressoOrderServed,
                "only releasing at the customer's hand completes the matching order");
            StepSmokeHint("serve.wait");
            UpdateStepGuidance();
            Check(stepGlow.HighlightedRendererCount == 0 && !stepGlow.DestinationVisible,
                "received drink no longer highlights the carried cup or handoff destination");
            TogglePause();
            StepSmokeHidden("pause suppresses counter guidance");
            TogglePause();
            ResetStation();
            StepSmokeHint("serve.wait");
            UpdateCounterService(2.1f);
            Check(counterCustomer.Phase == CounterCustomerPhase.Leaving, "customer leaves with the received drink");
            StepSmokeHint("serve.wait");
            UpdateCounterService(2.1f);
            Check(counterCustomer.Phase == CounterCustomerPhase.Gone, "customer finishes leaving before next-order guidance");
            StepSmokeHint("serve.next");
            EnterCounterView();
            StepSmokeButton("serve.next", new Rect(705, 490, 231, 40));
            Check(TryAdvanceEspressoOrder(), "next-order guidance advances the real customer campaign");
            StepSmokeHint("order.wait");
            Debug.Log("STEP_GUIDANCE_SMOKE_PASSED");
            Application.Quit(0);
        }

        private IEnumerator StepSmokePreparePuck()
        {
            StepSmokeTarget("level.pick", art.Distributor);
            Check(PickPrep(1), "guidance picks up the actual distributor");
            yield return Settle();
            StepSmokePlace("level.place", art.Tool.position);
            ReachPrep(art.Mat);
            DropPrep();
            yield return Settle();
            StepSmokeTarget("level.rotate", art.Distributor);
            RotatePrep(135);
            StepSmokeHint("level.rotate");
            RotatePrep(135);
            Check(Prep.LevelProgress == 1 && prepContact, "the seated distributor reaches its rotation goal");
            StepSmokeTarget("level.release", art.Distributor);
            yield return Shot("step_02-level-release");
            EndRotate();
            yield return Settle();
            StepSmokeTarget("tamp.pick", art.Tamper);
            Check(PickPrep(2), "guidance picks up the actual tamper");
            yield return Settle();
            StepSmokePlace("tamp.place", art.Tool.position);
            ReachPrep(art.Mat);
            DropPrep();
            yield return Settle();
            StepSmokeTarget("tamp.press", art.Tamper);
            Check(Prep.BeginTamp(), "actual tamper starts pressing");
            PressPrep(.7f, .05f);
            StepSmokeHint("tamp.press");
            for (int i = 0; i < 6; i++) PressPrep(1, .05f);
            Check(Prep.TampHoldProgress == 1 && !Prep.Complete, "full tamp hold still waits for physical release");
            StepSmokeTarget("tamp.release", art.Tamper);
            yield return Shot("step_03-tamp-release");
            EndPress();
            yield return Settle();
            Check(Prep.Complete && brewView, "releasing the completed tamp enters the brew stage");
        }

        private void StepSmokeHint(string expected)
        {
            var hint = CurrentStepHint();
            Check(hint.Id == expected,
                $"step guidance expected {expected}, got {hint.Id}; slot={Model.Slot}, grounds={Model.Grounds:0.00}, "
                + $"flying={Model.Flying:0.00}, running={Model.Running}, prep={prepKind}/{prepHeld}/{prepContact}, "
                + $"brew={Brew.Inserted}/{Brew.Locked}/{Brew.Running}/{Brew.Complete}, counter={counterCustomer.Phase}");
            Check(hint.Stage >= 0 && hint.Stage <= 8 && !string.IsNullOrEmpty(hint.Instruction),
                expected + " provides a valid stage and instruction");
        }

        private void StepSmokeTarget(string expected, Transform target)
        {
            StepSmokeHint(expected);
            var hint = CurrentStepHint();
            Check(hint.Target == target, expected + " identifies the actual interactable transform");
            UpdateStepGuidance();
            Check(StepGlowAllowed && stepGlow != null && stepGlow.HighlightedRendererCount > 0,
                expected + " highlights the active target's actual meshes");
        }

        private void StepSmokePlace(string expected, Vector3 destination)
        {
            StepSmokeHint(expected);
            var hint = CurrentStepHint();
            Check(hint.Place.HasValue && Vector2.Distance(new Vector2(hint.Place.Value.x, hint.Place.Value.z),
                new Vector2(destination.x, destination.z)) < .001f,
                expected + " marks the real drop destination in world space");
            UpdateStepGuidance();
            Check(StepGlowAllowed && stepGlow != null && stepGlow.DestinationVisible,
                expected + " displays the destination ring and arrow");
        }

        private void StepSmokeButton(string expected, Rect? button = null)
        {
            StepSmokeHint(expected);
            var hint = CurrentStepHint();
            Check(hint.Button.HasValue && (!button.HasValue || hint.Button.Value == button.Value),
                expected + " identifies the visible action button's existing hitbox");
        }

        private void StepSmokeHidden(string reason)
        {
            UpdateStepGuidance();
            Check(!StepGlowAllowed && stepGlow != null && stepGlow.HighlightedRendererCount == 0 &&
                !stepGlow.DestinationVisible, reason);
        }
    }
}
