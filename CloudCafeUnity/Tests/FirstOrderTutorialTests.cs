using System;
using CloudCafe.Crafting;

internal static class FirstOrderTutorialTests
{
    private static int checks;

    private static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition) throw new Exception(reason);
    }

    private static void At(FirstOrderTutorialModel tutorial, FirstOrderTutorialStep expected, int count, string reason)
    {
        Check(tutorial.Step == expected && tutorial.CompletedStepCount == count, reason);
    }

    private static void Main()
    {
        var tutorial = new FirstOrderTutorialModel();
        Check(!tutorial.Active && !tutorial.Completed, "a new tutorial is inactive and unfinished");
        At(tutorial, FirstOrderTutorialStep.AcceptOrder, 0, "a new tutorial has completed no actions");
        Check(FirstOrderTutorialModel.TotalActionSteps == 30, "the first order contains all thirty required actions");
        Check(!tutorial.TryCompleteStep(FirstOrderTutorialStep.AcceptOrder, true),
            "events before the tutorial begins cannot accept an order");
        Check(!tutorial.RestartPreparation(), "an inactive tutorial cannot start preparation");

        tutorial.Begin();
        Check(tutorial.Active && !tutorial.Completed, "beginning activates the tutorial");
        At(tutorial, FirstOrderTutorialStep.AcceptOrder, 0,
            "beginning waits for actual order acceptance instead of counting it as complete");
        Check(!tutorial.RestartPreparation(), "resetting the station before acceptance cannot skip the order");
        At(tutorial, FirstOrderTutorialStep.AcceptOrder, 0, "unaccepted order remains the current action");

        for (int action = 0; action < FirstOrderTutorialModel.TotalActionSteps; action++)
        {
            var step = (FirstOrderTutorialStep)action;
            var next = (FirstOrderTutorialStep)(action + 1);
            Check(!tutorial.TryCompleteStep(step, false), step + " failure does not advance the tutorial");
            Check(!tutorial.TryCompleteStep(next, true), step + " cannot be bypassed by a future success callback");
            Check(!tutorial.TryCompleteStep((FirstOrderTutorialStep)(-1), true),
                step + " rejects an invalid action callback");
            At(tutorial, step, action, step + " stays current until its own operation succeeds");
            Check(tutorial.Active && !tutorial.Completed, step + " remains active while waiting for success");
            Check(tutorial.TryCompleteStep(step, true), step + " advances after its own successful operation");
            At(tutorial, next, action + 1, step + " advances exactly one action");
            Check(!tutorial.TryCompleteStep(step, true), step + " duplicate callback cannot advance another action");
            At(tutorial, next, action + 1, step + " duplicate callback leaves progress intact");
        }

        Check(!tutorial.Active && tutorial.Completed, "the final successful handoff completes and deactivates the tutorial");
        Check(tutorial.CompletedStepCount == FirstOrderTutorialModel.TotalActionSteps,
            "completion records every action exactly once");
        Check(!tutorial.TryCompleteStep(FirstOrderTutorialStep.Complete, true) &&
            !tutorial.TryCompleteStep(FirstOrderTutorialStep.ReleaseServingCup, true),
            "completion and repeated handoff callbacks cannot restart a completed tutorial");
        Check(!tutorial.RestartPreparation(), "retrying preparation cannot reopen a completed tutorial");
        At(tutorial, FirstOrderTutorialStep.Complete, FirstOrderTutorialModel.TotalActionSteps,
            "completion remains stable until explicitly begun or reset");

        tutorial.Begin();
        Check(tutorial.Active && !tutorial.Completed, "an explicit begin can start a fresh tutorial after completion");
        At(tutorial, FirstOrderTutorialStep.AcceptOrder, 0, "beginning again clears all previous action progress");
        Check(tutorial.TryCompleteStep(FirstOrderTutorialStep.AcceptOrder, true), "accept the fresh tutorial's order");
        Check(tutorial.RestartPreparation(), "an accepted order allows a preparation retry");
        At(tutorial, FirstOrderTutorialStep.PickPortafilter, 1, "retry preserves only the completed order acceptance");

        while (tutorial.Step != FirstOrderTutorialStep.PressTamper)
        {
            Check(tutorial.TryCompleteStep(tutorial.Step, true), "advance successful preparation before a retry");
        }
        Check(!tutorial.TryCompleteStep(FirstOrderTutorialStep.PressTamper, false),
            "an unsuccessful tamp remains incomplete before retry");
        Check(tutorial.RestartPreparation(), "a retry can discard an unfinished preparation sequence");
        At(tutorial, FirstOrderTutorialStep.PickPortafilter, 1, "retry returns to pickup with the accepted order preserved");
        Check(tutorial.Active && !tutorial.Completed, "preparation retry remains an active tutorial");
        Check(!tutorial.TryCompleteStep(FirstOrderTutorialStep.PressTamper, true) &&
            !tutorial.TryCompleteStep(FirstOrderTutorialStep.AcceptOrder, true),
            "stale prep and acceptance callbacks after retry cannot skip the new pickup");
        Check(tutorial.TryCompleteStep(FirstOrderTutorialStep.PickPortafilter, true),
            "the first preparation action must succeed again after a retry");
        At(tutorial, FirstOrderTutorialStep.PlaceAtGrinder, 2, "retry resumes the normal ordered sequence");

        tutorial.Reset();
        Check(!tutorial.Active && !tutorial.Completed, "reset clears an in-progress tutorial");
        At(tutorial, FirstOrderTutorialStep.AcceptOrder, 0, "reset clears acceptance and preparation progress");
        Check(!tutorial.TryCompleteStep(FirstOrderTutorialStep.AcceptOrder, true), "reset leaves callbacks inactive");
        tutorial.Begin();
        for (int action = 0; action < FirstOrderTutorialModel.TotalActionSteps; action++)
            Check(tutorial.TryCompleteStep((FirstOrderTutorialStep)action, true), "complete a second explicit tutorial");
        tutorial.Reset();
        Check(!tutorial.Active && !tutorial.Completed, "reset clears completed status as well as active status");
        At(tutorial, FirstOrderTutorialStep.AcceptOrder, 0, "resetting a completed tutorial returns it to the inactive start");
        Console.WriteLine("FIRST_ORDER_TUTORIAL_TESTS_PASSED " + checks);
    }
}
