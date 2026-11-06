using System;
using CloudCafe.Crafting;

internal static class CounterCustomerTests
{
    private static int checks;

    private static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition) throw new Exception(reason);
    }

    private static void Near(float actual, float expected, string reason)
    {
        Check(Math.Abs(actual - expected) < .0001f, reason);
    }

    private static CounterCustomerModel Waiting()
    {
        var customer = new CounterCustomerModel();
        customer.Begin(2);
        customer.Step(CounterCustomerModel.ArrivalSeconds);
        customer.AcceptOrder();
        return customer;
    }

    private static void Main()
    {
        var customer = new CounterCustomerModel();
        Check(!customer.Active && customer.Phase == CounterCustomerPhase.Gone &&
            customer.CustomerIndex == -1 && !customer.HoldingDrink, "station starts without a visitor");
        Check(!customer.Begin(-1) && !customer.Begin(CustomerVisitModel.CustomerCount),
            "invalid identities cannot enter");
        Check(!customer.AcceptOrder() && !customer.TryHandOver(), "absent customer cannot order or receive a drink");
        Check(customer.Begin(4) && customer.Active && customer.Phase == CounterCustomerPhase.Arriving &&
            customer.CustomerIndex == 4, "a customer begins approaching the counter");
        Check(!customer.Begin(0), "a second arrival cannot replace the current visitor");
        Check(!customer.AcceptOrder() && !customer.TryHandOver(), "approaching customer cannot skip to handover");
        Near(customer.CounterProgress, 0f, "arrival begins away from the counter");
        customer.Step(.5f);
        Near(customer.PhaseProgress, .25f, "arrival uses elapsed seconds");
        Near(customer.CounterProgress, .25f, "counter approach moves toward the seat");
        customer.Step(float.NaN);
        customer.Step(float.PositiveInfinity);
        customer.Step(-1f);
        Near(customer.PhaseProgress, .25f, "invalid time does not move the customer");
        customer.Step(100f);
        Check(customer.Phase == CounterCustomerPhase.Ordering && !customer.HoldingDrink,
            "arrival stops to order even if the frame contains extra time");
        Near(customer.CounterProgress, 1f, "ordering takes place at the counter");
        Check(!customer.TryHandOver(), "an order must be accepted before handover");
        Check(customer.AcceptOrder() && !customer.AcceptOrder() && customer.Phase == CounterCustomerPhase.Waiting,
            "an accepted order makes the customer wait once");
        customer.Step(100f);
        Check(customer.Phase == CounterCustomerPhase.Waiting, "waiting does not automatically serve a drink");
        Check(customer.TryHandOver() && !customer.TryHandOver() &&
            customer.Phase == CounterCustomerPhase.Receiving && customer.HoldingDrink,
            "handover gives the visitor a drink exactly once");
        customer.Step(CounterCustomerModel.ReceivingSeconds * .4f);
        Near(customer.CounterProgress, 1f, "receiving customer stays at the counter");
        Near(customer.PhaseProgress, .4f, "receiving animation advances with elapsed time");
        customer.Step(CounterCustomerModel.ReceivingSeconds * .6f + .25f);
        Check(customer.Phase == CounterCustomerPhase.Leaving && customer.HoldingDrink,
            "receiving transitions to leaving with the drink");
        Near(customer.PhaseProgress, .125f, "extra frame time carries into leaving");
        Near(customer.CounterProgress, .875f, "departure moves away from the counter");
        customer.Step(10f);
        Check(customer.Phase == CounterCustomerPhase.Gone && !customer.Active && customer.HoldingDrink,
            "the customer leaves the scene with the received drink");
        Near(customer.CounterProgress, 0f, "the departed customer is away from the counter");
        Check(customer.Begin(1) && !customer.HoldingDrink, "another customer arrives without the previous drink");
        customer.Reset();
        Check(customer.Phase == CounterCustomerPhase.Gone && customer.CustomerIndex == -1,
            "reset clears the active arrival");

        var aggregate = Waiting();
        var divided = Waiting();
        aggregate.TryHandOver();
        divided.TryHandOver();
        aggregate.Step(3f);
        for (int i = 0; i < 60; i++) divided.Step(.05f);
        Check(aggregate.Phase == divided.Phase, "splitting elapsed time preserves the animation phase");
        Near(aggregate.CounterProgress, divided.CounterProgress, "splitting elapsed time preserves movement");
        var receiving = Waiting();
        receiving.TryHandOver();
        receiving.Reset();
        receiving.Step(10f);
        Check(receiving.Phase == CounterCustomerPhase.Gone && !receiving.HoldingDrink,
            "reset cancels an in-progress handover");

        Console.WriteLine("COUNTER_CUSTOMER_TESTS_PASSED " + checks);
    }
}
