using System;
using CloudCafe.Crafting;

internal static class AmericanoTests
{
    private static int checks;

    private static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition) throw new Exception(reason);
    }

    private static bool Near(float actual, float expected) => Math.Abs(actual - expected) < .001f;

    private static void Main()
    {
        var drink = new AmericanoModel();
        Check(!drink.Active && !drink.Complete && drink.TotalGrams == 0f, "empty on creation");
        Check(!drink.TryFinish(), "finishing requires an espresso shot");
        Check(!drink.Begin(0f) && !drink.Begin(float.NaN) && !drink.Begin(float.PositiveInfinity) &&
            !drink.Begin(-1f) && !drink.Begin(AmericanoModel.CapacityGrams), "reject impossible shots");
        Check(drink.Begin(36f) && drink.Active && drink.TotalGrams == 36f, "preserve the actual espresso dose");
        Check(drink.TargetWaterGrams == 108f, "36g of espresso recommends 108g of hot water");
        Check(!drink.TryFinish(), "espresso alone is not an Americano");
        Check(!drink.Begin(30f) && drink.EspressoGrams == 36f, "cannot replace a drink in progress");

        drink.Step(.05f, false);
        drink.Step(float.NaN, true);
        drink.Step(float.PositiveInfinity, true);
        drink.Step(-.1f, true);
        Check(!drink.Pouring && drink.WaterGrams == 0f, "idle or invalid input does not dispense water");
        drink.Step(5f, true);
        Check(drink.Pouring && Near(drink.WaterGrams, 2.5f), "a delayed frame is capped");
        for (int i = 0; i < 79; i++) drink.Step(.025f, true);
        Check(Near(drink.WaterGrams, 101.25f), "water follows controlled pour duration");
        drink.Step(.05f, false);
        Check(!drink.Pouring && Near(drink.WaterGrams, 101.25f), "releasing the control stops the pour");
        Check(Near(drink.TotalGrams, 137.25f) && Near(drink.WaterToEspressoRatio, 101.25f / 36f),
            "report uses actual water mass and ratio");
        Check(drink.TryFinish() && drink.Complete && !drink.Active, "finish slightly short of the recommendation");
        drink.Step(.05f, true);
        Check(!drink.TryFinish() && !drink.Pouring && Near(drink.WaterGrams, 101.25f),
            "finished drink cannot be changed");

        Check(drink.Begin(60f) && !drink.Complete && drink.WaterGrams == 0f, "a new shot starts a clean drink");
        Check(drink.TargetWaterGrams == 180f, "maximum normal shot still fits its target");
        for (int i = 0; i < 100; i++) drink.Step(.05f, true);
        Check(!drink.Pouring && Near(drink.TotalGrams, AmericanoModel.CapacityGrams) &&
            Near(drink.WaterGrams, 180f), "water stops at capacity and espresso is retained");
        Check(drink.TryFinish(), "capacity cutoff leaves drink ready to finish");

        drink.Reset();
        Check(!drink.Active && !drink.Complete && !drink.Pouring && drink.TotalGrams == 0f &&
            drink.TargetWaterGrams == 0f && drink.WaterToEspressoRatio == 0f, "reset clears serving state");
        Console.WriteLine("AMERICANO_TESTS_PASSED " + checks);
    }
}
