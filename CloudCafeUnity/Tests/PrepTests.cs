using System;
using CloudCafe.Crafting;

internal static class PrepTests
{
    private static int checks;

    private static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition) throw new Exception(reason);
    }

    private static void Near(float actual, float expected, string reason) =>
        Check(Math.Abs(actual - expected) < .0001f, reason);

    private static void Main()
    {
        var prep = new PuckPrepModel();
        Near(prep.LevelProgress, 0f, "a fresh basket has no leveling progress");
        Near(prep.TampHoldProgress, 0f, "a fresh basket has no tamp hold progress");
        prep.Rotate(720f);
        Check(!prep.Leveled, "rotation away from basket does nothing");
        Check(!prep.BeginTamp(), "tamping requires leveling");

        prep.BeginLevel();
        prep.Rotate(130f);
        Near(prep.LevelProgress, 130f / PuckPrepModel.RequiredRotationDegrees,
            "leveling progress follows the accumulated rotation");
        prep.Rotate(-140f);
        Check(prep.Leveled, "leveling accepts either direction and accumulates arc");
        Near(prep.LevelProgress, 1f, "a leveled basket shows full progress");
        prep.Rotate(720f);
        Near(prep.LevelProgress, 1f, "extra rotation cannot overfill the progress indicator");
        prep.EndLevel();

        Check(prep.BeginTamp(), "tamping starts after leveling");
        prep.Press(1f, .1f);
        Near(prep.FirmTime, .05f, "a large frame contributes only the supported hold interval");
        Near(prep.TampHoldProgress, .2f, "partial firm hold has partial UI progress");
        prep.Release();
        Check(!prep.Complete, "a short press is insufficient");
        Near(prep.FirmTime, 0f, "releasing a short press clears the hold time");
        Near(prep.TampHoldProgress, 0f, "releasing a short press clears the UI progress");

        prep.BeginTamp();
        for (int i = 0; i < 10; i++) prep.Press(.4f, .05f);
        Near(prep.TampHoldProgress, 0f, "light pressure does not fill the hold indicator");
        prep.Release();
        Check(!prep.Complete, "light pressure is insufficient");

        prep.BeginTamp();
        prep.Press(PuckPrepModel.RequiredTampDepth, .05f);
        Near(prep.TampHoldProgress, .2f, "the required depth starts firm hold progress");
        prep.Press(PuckPrepModel.RequiredTampDepth - .01f, .05f);
        Near(prep.TampHoldProgress, 0f, "falling below required depth restarts the hold");
        for (int i = 0; i < 5; i++) prep.Press(PuckPrepModel.RequiredTampDepth, .05f);
        Near(prep.FirmTime, PuckPrepModel.RequiredFirmSeconds, "firm hold reaches the release threshold");
        Near(prep.TampHoldProgress, 1f, "a sufficient hold prompts the player to release");
        Check(!prep.Complete, "full hold progress alone does not complete tamping");
        prep.Press(1f, .05f);
        Near(prep.TampHoldProgress, 1f, "an extra hold cannot exceed full UI progress");
        prep.Release();
        Check(prep.Complete, "firm press followed by release completes tamping");
        Near(prep.FirmTime, 0f, "completed release clears the live hold timer");
        Near(prep.TampHoldProgress, 1f, "completed tamping stays at full UI progress");

        prep.Reset();
        Check(!prep.Complete && !prep.Leveled && prep.Rotation == 0f, "reset clears the workflow");
        Near(prep.LevelProgress, 0f, "reset clears leveling progress");
        Near(prep.TampHoldProgress, 0f, "reset clears completed tamp progress");
        Near(prep.FirmTime, 0f, "reset clears the firm hold timer");
        Console.WriteLine("PUCK_PREP_TESTS_PASSED " + checks);
    }
}
