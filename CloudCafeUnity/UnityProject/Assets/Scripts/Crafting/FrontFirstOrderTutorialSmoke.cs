using System.Collections;
using CloudCafe.Coffee;
using UnityEngine;
using TutorialStep = CloudCafe.Crafting.FirstOrderTutorialStep;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private IEnumerator FirstOrderTutorialSmoke()
        {
            yield return null;
            Check(!professionalMode && coffeeBeanCatalog != null, "tutorial smoke starts at a fresh basic station");
            Check(TryApplyDifficulty(true, professionalGrind, professionalTemperature, professionalPressure),
                "starting professional mode creates the first real customer");
            Check(customerProgress.TotalServed == 0 && !customerProgress.FirstOrderTutorialCompleted &&
                customerVisit != null && counterCustomer.Phase == CounterCustomerPhase.Arriving,
                "a new cafe begins with an uncompleted first-customer tutorial");
            FirstOrderSmokeAt(TutorialStep.AcceptOrder);
            Check(TutorialVisible && espressoOrder.BeanId == "yirgacheffe_kurume" &&
                espressoOrder.Roast == EspressoRoast.Medium && espressoOrder.Drink == CoffeeDrink.Espresso &&
                SelectedCoffeeBean.Id == espressoOrder.BeanId && SelectedCoffeeRoast == espressoOrder.Roast,
                "the first lesson presets a matching medium-roast Yirgacheffe espresso");
            TutorialDid(TutorialStep.AcceptOrder, false);
            TutorialDid(TutorialStep.PickServingCup, true);
            FirstOrderSmokeAt(TutorialStep.AcceptOrder);
            Check(!AcceptCustomerOrder() && !Grab(), "an approaching customer and unaccepted order cannot be skipped");
            UpdateCounterService(2.1f);
            Check(customerDialogueOpen && counterCustomer.Phase == CounterCustomerPhase.Ordering,
                "the first customer reaches the counter and speaks before acceptance");
            Check(AcceptCustomerOrder(), "accept the tutorial customer's actual order");
            FirstOrderSmokeAt(TutorialStep.PickPortafilter);
            CustomerVisitModel firstCustomer = customerVisit;
            float patientAtAcceptance = firstCustomer.PatienceRemainingSeconds;
            UpdateCustomerVisit(60f);
            Check(firstCustomer.PatienceRemainingSeconds == patientAtAcceptance,
                "accepted tutorial customers keep their full patience while the player learns");
            Check(!PickPrep(1) && !PickPrep(2) && !GrabCup() && !TryChooseDrink(CoffeeDrink.Latte),
                "unrelated tools and drinks cannot bypass the current lesson");
            Check(!TrySelectCoffeeBean("gesha", EspressoRoast.Dark) &&
                !TryApplyDifficulty(false, professionalGrind, professionalTemperature, professionalPressure) &&
                !TrySetEspressoOrder("gesha", EspressoRoast.Dark, CoffeeDrink.Americano),
                "the first lesson protects its fixed bean, roast, difficulty, and customer request");
            FirstOrderSmokeAt(TutorialStep.PickPortafilter);

            Check(Grab(), "pick up the tutorial portafilter");
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceAtGrinder);
            ResetStation();
            FirstOrderSmokeAt(TutorialStep.PickPortafilter);
            Check(customerVisit == firstCustomer && firstCustomer.Phase == CustomerVisitPhase.Preparing &&
                counterCustomer.Phase == CounterCustomerPhase.Waiting &&
                firstCustomer.PatienceRemainingSeconds == patientAtAcceptance &&
                !customerProgress.FirstOrderTutorialCompleted && Model.Grounds == 0f,
                "reset retries preparation while preserving the accepted waiting customer and unfinished lesson");

            Check(Grab(), "pick up the portafilter again after a preparation retry");
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceAtGrinder);
            Reach(new Vector3(1.7f, .72f, .2f));
            Drop();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceAtGrinder);
            Check(Model.Slot != CraftSlot.Grinder, "a misplaced basket is not recorded as a grinder placement");
            if (!held)
            {
                Check(Grab(), "the placement lesson permits repicking a misplaced portafilter");
                yield return Settle();
            }
            FirstOrderSmokeAt(TutorialStep.PlaceAtGrinder);
            Reach(art.Dock);
            yield return Shot("tutorial_00-grinder-place");
            Drop();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.StartGrinder);
            Check(!PickPrep(1), "dosing cannot be bypassed with a prep tool");
            Switch();
            Check(Model.Running, "the real grinder begins running");
            FirstOrderSmokeAt(TutorialStep.StopGrinder);
            for (int i = 0; i < 10; i++) Model.Step(.05f);
            Switch();
            Check(!Model.Running && Model.DoseInBasketOrFlight < ProfessionalEspressoModel.MinDoseGrams,
                "an early button press really stops the grinder below its preparation minimum");
            FirstOrderSmokeAt(TutorialStep.StopGrinder);
            for (int i = 0; i < 8; i++) Model.Step(.05f);
            UpdateFirstOrderTutorial();
            FirstOrderSmokeAt(TutorialStep.StopGrinder);
            Check(!Grab() && !CommitProfessionalDose(), "an underweight tutorial dose cannot advance to removal or confirmation");
            Switch();
            Check(Model.Running, "the same tutorial button restarts an underweight dose");
            FirstOrderSmokeAt(TutorialStep.StopGrinder);
            for (int i = 0; i < 160 && Model.Running && Model.DoseInBasketOrFlight < 18f; i++) Model.Step(.05f);
            Check(Model.Running && Model.DoseInBasketOrFlight >= 17.99f, "the lesson reaches the recommended manual dose");
            Switch();
            FirstOrderSmokeAt(TutorialStep.WaitGrounds);
            Check(Model.Flying > 0f && !Grab(), "the lesson waits for actual falling grounds before withdrawal");
            UpdateFirstOrderTutorial();
            FirstOrderSmokeAt(TutorialStep.WaitGrounds);
            for (int i = 0; i < 8; i++) Model.Step(.05f);
            UpdateFirstOrderTutorial();
            FirstOrderSmokeAt(TutorialStep.ReturnPick);
            Check(Grab(), "withdraw the actually settled dose");
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceOnMat);
            Reach(art.Mat);
            Drop();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.CommitDose);
            Check(!PickPrep(1) && !Prep.Leveled, "weighing must be confirmed before leveling");
            Check(CommitProfessionalDose(), "confirm the real weighed dose");
            FirstOrderSmokeAt(TutorialStep.PickLeveler);
            Check(!PickPrep(2), "the tamper cannot replace the required distributor");
            Check(PickPrep(1), "pick up the real distributor");
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceLeveler);
            ReachPrep(new Vector3(-1.5f, .72f, -1f));
            DropPrep();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceLeveler);
            Check(!prepContact && PickPrep(1), "a missed distributor drop can be picked up and retried");
            yield return Settle();
            ReachPrep(art.Mat);
            DropPrep();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.RotateLeveler);
            RotatePrep(135);
            EndRotate();
            FirstOrderSmokeAt(TutorialStep.RotateLeveler);
            Check(!Prep.Leveled, "a partial rotation does not satisfy the leveling goal");
            RotatePrep(135);
            FirstOrderSmokeAt(TutorialStep.ReleaseLeveler);
            Check(prepContact && Prep.Leveled, "rotation progress waits for the player's release");
            EndRotate();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PickTamper);
            Check(PickPrep(2), "pick up the real tamper");
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceTamper);
            ReachPrep(art.Mat);
            DropPrep();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PressTamper);
            Check(Prep.BeginTamp(), "begin the actual tamp input");
            PressPrep(1, .05f);
            EndPress();
            FirstOrderSmokeAt(TutorialStep.PressTamper);
            Check(!Prep.Complete && Prep.TampHoldProgress == 0f, "a short tamp cannot complete its lesson");
            Check(Prep.BeginTamp(), "retry the short tamp at the seated tool");
            for (int i = 0; i < 6; i++) PressPrep(1, .05f);
            FirstOrderSmokeAt(TutorialStep.ReleaseTamper);
            Check(!Prep.Complete, "a full hold still requires a genuine release");
            yield return Shot("tutorial_01-tamp-release");
            PressPrep(.2f, .05f);
            EndPress();
            FirstOrderSmokeAt(TutorialStep.ReleaseTamper);
            Check(!Prep.Complete, "losing tamp depth before release does not record a successful tamp");
            Check(Prep.BeginTamp(), "the release lesson permits rebuilding a lost hold");
            for (int i = 0; i < 6; i++) PressPrep(1, .05f);
            EndPress();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PickPreparedBasket);
            Check(Prep.Complete && Grab(), "lift the genuinely completed puck for brewing");
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.InsertBasket);
            Reach(art.GroupSeat);
            Drop();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.LockBasket);
            TurnBrew(20);
            FirstOrderSmokeAt(TutorialStep.LockBasket);
            SwitchBrew();
            Check(!Brew.Running && !Brew.Locked, "partial handle rotation cannot start extraction");
            TurnBrew(20);
            FirstOrderSmokeAt(TutorialStep.PickCup);
            Check(GrabCup(), "pick up the actual espresso cup");
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceCup);
            ReachCup(art.CupHome);
            DropCup();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.PlaceCup);
            Check(!Brew.CupDocked && GrabCup(), "a misplaced cup must be picked up again");
            yield return Settle();
            ReachCup(art.CupSeat);
            DropCup();
            yield return Settle();
            FirstOrderSmokeAt(TutorialStep.StartExtraction);
            SwitchBrew();
            Check(Brew.Running && Brew.IsProfessional, "start the real professional extraction");
            FirstOrderSmokeAt(TutorialStep.StopExtraction);
            for (int i = 0; i < 4; i++) StepStationBrew(.05f);
            SwitchBrew();
            Check(!Brew.ShotFinalized && Brew.ElapsedSeconds == 0f,
                "a stop during preinfusion does not create a completed tutorial shot");
            FirstOrderSmokeAt(TutorialStep.StopExtraction);
            SwitchBrew();
            float targetMass = Model.Grounds * professionalBeanRecipe.BrewRatio;
            for (int i = 0; i < 1200 && Brew.Running && Brew.InCup < targetMass; i++) StepStationBrew(.05f);
            Check(Brew.Running && Brew.InCup >= targetMass, "the selected recipe reaches its real manual stop target");
            FirstOrderSmokeAt(TutorialStep.StopExtraction);
            Check(!TryChooseDrink(CoffeeDrink.Espresso), "an unfinished flowing shot cannot skip to drink choice");
            SwitchBrew();
            FirstOrderSmokeAt(TutorialStep.WaitExtraction);
            Check(Brew.InFlight > 0f && !Brew.Complete, "the stopped extraction still has coffee in flight");
            UpdateFirstOrderTutorial();
            FirstOrderSmokeAt(TutorialStep.WaitExtraction);
            for (int i = 0; i < 8; i++) StepStationBrew(.05f);
            UpdateFirstOrderTutorial();
            FirstOrderSmokeAt(TutorialStep.ChooseEspresso);
            Check(Brew.Complete && professionalReportOpen, "the settled shot opens the real drink selection");
            Check(!TryChooseDrink(CoffeeDrink.Americano) && !TryChooseDrink(CoffeeDrink.Latte),
                "the espresso lesson blocks other drink routes");
            FirstOrderSmokeAt(TutorialStep.ChooseEspresso);
            Check(TryChooseDrink(CoffeeDrink.Espresso), "choose the customer's actual espresso");
            FirstOrderSmokeAt(TutorialStep.CarryToCounter);
            UpdateCustomerVisit(60f);
            Check(firstCustomer.PatienceRemainingSeconds == patientAtAcceptance &&
                !customerProgress.FirstOrderTutorialCompleted && !espressoOrderSubmitted,
                "finishing the drink neither consumes tutorial patience nor records handoff completion");
            Check(BeginCounterHandoff(), "carry the finished espresso to its customer");
            FirstOrderSmokeAt(TutorialStep.PickServingCup);
            Check(GrabServingCup(), "pick up the finished serving cup");
            FirstOrderSmokeAt(TutorialStep.PlaceServingCup);
            MoveServingCup(art.CounterCupHome);
            Check(!DropServingCup() && !espressoOrderSubmitted && !customerProgress.FirstOrderTutorialCompleted,
                "a release away from the customer cannot complete serving or the tutorial");
            FirstOrderSmokeAt(TutorialStep.PlaceServingCup);
            if (!servingCupHeld) Check(GrabServingCup(), "a missed handoff allows repicking the cup");
            MoveServingCup(art.CounterReceivePoint);
            UpdateFirstOrderTutorial();
            FirstOrderSmokeAt(TutorialStep.ReleaseServingCup);
            Check(!espressoOrderSubmitted && !customerProgress.FirstOrderTutorialCompleted,
                "reaching the customer's hand still waits for actual release");
            yield return Shot("tutorial_02-handoff-release");
            Check(DropServingCup() && espressoOrderServed &&
                counterCustomer.Phase == CounterCustomerPhase.Receiving && counterCustomer.HoldingDrink,
                "the matching customer physically receives the completed espresso");
            FirstOrderSmokeAt(TutorialStep.Complete);
            Check(customerVisit == firstCustomer && firstCustomer.OrderMatched && firstCustomer.Credited &&
                customerProgress.TotalServed == 1 && customerProgress.FirstOrderTutorialCompleted,
                "only the successful real handoff completes and records the first lesson");
            var savedProgress = JsonUtility.FromJson<CustomerCafeProgress>(JsonUtility.ToJson(customerProgress));
            Check(savedProgress.FirstOrderTutorialCompleted, "tutorial completion survives the cafe progress save format");
            ResetStation();
            Check(!TutorialActive && tutorial.Completed && customerProgress.FirstOrderTutorialCompleted,
                "reset after successful handoff cannot restart the completed lesson");
            UpdateCounterService(4.2f);
            Check(counterCustomer.Phase == CounterCustomerPhase.Gone && TryAdvanceEspressoOrder(),
                "the next real persona arrives after the first customer leaves");
            Check(customerVisit != firstCustomer && !TutorialActive && customerProgress.FirstOrderTutorialCompleted &&
                counterCustomer.Phase == CounterCustomerPhase.Arriving,
                "the second customer uses normal service without replaying the first-order tutorial");
            Debug.Log("FIRST_ORDER_TUTORIAL_SMOKE_PASSED");
            Application.Quit(0);
        }

        private void FirstOrderSmokeAt(TutorialStep expected)
        {
            Check(tutorial != null && tutorial.Step == expected &&
                tutorial.CompletedStepCount == (int)expected &&
                TutorialActive == (expected != TutorialStep.Complete) &&
                tutorial.Completed == (expected == TutorialStep.Complete),
                $"first-order tutorial expected {expected}, got {tutorial?.Step}; "
                + $"dose={Model.Grounds:0.00}+{Model.Flying:0.00}, grinder={Model.Running}/{Model.Slot}, "
                + $"prep={prepKind}/{prepHeld}/{prepContact}/{Prep.Complete}, "
                + $"brew={Brew.Running}/{Brew.ShotFinalized}/{Brew.Complete}, counter={counterCustomer.Phase}");
        }
    }
}
