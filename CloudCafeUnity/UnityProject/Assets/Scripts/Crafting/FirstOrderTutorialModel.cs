namespace CloudCafe.Crafting
{
    public enum FirstOrderTutorialStep
    {
        AcceptOrder,
        PickPortafilter,
        PlaceAtGrinder,
        StartGrinder,
        StopGrinder,
        WaitGrounds,
        ReturnPick,
        PlaceOnMat,
        CommitDose,
        PickLeveler,
        PlaceLeveler,
        RotateLeveler,
        ReleaseLeveler,
        PickTamper,
        PlaceTamper,
        PressTamper,
        ReleaseTamper,
        PickPreparedBasket,
        InsertBasket,
        LockBasket,
        PickCup,
        PlaceCup,
        StartExtraction,
        StopExtraction,
        WaitExtraction,
        ChooseEspresso,
        CarryToCounter,
        PickServingCup,
        PlaceServingCup,
        ReleaseServingCup,
        Complete
    }

    public sealed class FirstOrderTutorialModel
    {
        public const int TotalActionSteps = (int)FirstOrderTutorialStep.Complete;

        public FirstOrderTutorialStep Step { get; private set; } = FirstOrderTutorialStep.AcceptOrder;
        public bool Active { get; private set; }
        public bool Completed => Step == FirstOrderTutorialStep.Complete;
        public int CompletedStepCount => (int)Step;

        public void Begin()
        {
            Step = FirstOrderTutorialStep.AcceptOrder;
            Active = true;
        }

        public bool TryCompleteStep(FirstOrderTutorialStep expectedStep, bool succeeded)
        {
            if (!Active || !succeeded || expectedStep != Step) return false;

            Step = (FirstOrderTutorialStep)((int)Step + 1);
            Active = Step != FirstOrderTutorialStep.Complete;
            return true;
        }

        public bool RestartPreparation()
        {
            // Retrying a drink preserves an accepted order, but cannot accept it.
            if (!Active || Step == FirstOrderTutorialStep.AcceptOrder) return false;
            Step = FirstOrderTutorialStep.PickPortafilter;
            return true;
        }

        public void Reset()
        {
            Step = FirstOrderTutorialStep.AcceptOrder;
            Active = false;
        }
    }
}
