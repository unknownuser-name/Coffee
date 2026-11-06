using System;
using CloudCafe.Crafting;

internal static class CustomerVisitTests
{
    private static int checks;

    private static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition) throw new Exception(reason);
    }

    private static CustomerVisitModel Resolved(int customer, int id, bool matched, int quality = 100)
    {
        var visit = new CustomerVisitModel(customer, id, CoffeeDrink.Espresso);
        visit.Accept();
        visit.Resolve(matched, quality);
        return visit;
    }

    private static void Main()
    {
        var visit = new CustomerVisitModel(0, 1, CoffeeDrink.Latte);
        Check(visit.Phase == CustomerVisitPhase.Talking && visit.Patience01 == 1f,
            "a new customer starts in conversation with full patience");
        visit.Tick(50f);
        Check(visit.PatienceRemainingSeconds == 420f, "conversation time does not spend preparation patience");
        Check(!visit.Resolve(true, 100), "cannot serve before accepting an order");
        Check(visit.Clarify() && !visit.Clarify() && visit.Clarified, "clarification is shown once");
        Check(visit.ShowHint() && !visit.ShowHint() && visit.Hinted, "recipe hint is shown once");
        Check(visit.Accept() && !visit.Accept(), "accepting starts preparation once");
        Check(!visit.Clarify() && !visit.ShowHint(), "preparation preserves the accepted conversation");
        visit.Tick(float.NaN);
        visit.Tick(float.PositiveInfinity);
        visit.Tick(-2f);
        Check(visit.Patience01 == 1f, "invalid time does not change patience");
        visit.Tick(30f);
        Check(visit.PatienceRemainingSeconds == 390f, "patience counts the full elapsed preparation time");
        Check(visit.Resolve(true, 100) && visit.Reaction == CustomerVisitReaction.Positive &&
            visit.Payment == visit.BasePrice + visit.Tip && visit.Tip > 0,
            "a timely accurate drink receives its price and a tip");
        int payment = visit.Payment;
        visit.Tick(600f);
        Check(!visit.Resolve(false, 0) && visit.Payment == payment && visit.PatienceRemainingSeconds == 390f,
            "a resolved visit is immutable");

        var progress = new CustomerCafeProgress();
        Check(!progress.TryCredit(null) && !progress.NextDay(), "empty cafe cannot settle or skip the day");
        Check(progress.TryCredit(visit) && !progress.TryCredit(visit) && visit.Credited &&
            progress.Coins == payment && progress.CustomersToday == 1 && progress.CorrectToday == 1 &&
            progress.CustomerVisits[0] == 1, "payment and returning-customer count are credited once");
        var wrong = Resolved(1, progress.NextVisitId, false);
        Check(wrong.Reaction == CustomerVisitReaction.Negative && wrong.Payment == 0 && wrong.Tip == 0,
            "an incorrect order is resolved with a negative response and no payment");
        Check(progress.TryCredit(wrong) && progress.Coins == payment && progress.CustomersToday == 2 &&
            progress.CorrectToday == 1 && progress.TotalServed == 2,
            "an incorrect order still counts as a completed customer visit");

        var slow = new CustomerVisitModel(2, progress.NextVisitId, CoffeeDrink.Americano, 360f);
        slow.Accept();
        slow.Tick(1000f);
        Check(slow.Patience01 == 0 && slow.Resolve(true, 100) && slow.Tip == 0 &&
            slow.Payment == slow.BasePrice && slow.Reaction == CustomerVisitReaction.Neutral,
            "running out of patience lowers the response and removes the tip");
        Check(progress.TryCredit(slow), "a slow correct visit can be settled");
        Check(progress.TryCredit(Resolved(3, progress.NextVisitId, true)) &&
            progress.TryCredit(Resolved(4, progress.NextVisitId, true)) && progress.DayComplete &&
            progress.CustomersToday == 5 && progress.TotalServed == 5,
            "five resolved customers complete the day");
        var sixth = Resolved(5, progress.NextVisitId, true);
        Check(!progress.TryCredit(sixth) && !sixth.Credited, "the next customer waits for the next day");
        int coins = progress.Coins;
        Check(progress.NextDay() && progress.Day == 2 && !progress.DayComplete && progress.Coins == coins &&
            progress.EarnedToday == 0 && progress.CorrectToday == 0 && progress.CustomersToday == 0 &&
            progress.CustomerVisits[0] == 1, "new day resets daily results and retains wallet and regular visits");
        Check(progress.TryCredit(sixth) && progress.TotalServed == 6 && progress.CustomerVisits[5] == 1,
            "the pending visit can be settled on the next day");

        var legacy = new CustomerCafeProgress
        {
            Coins = -100, Day = 0, CustomersToday = -1, CorrectToday = 20, EarnedToday = -30,
            TotalServed = -2, CustomerVisits = new[] { -2, 3 }
        };
        legacy.Normalize();
        Check(legacy.Coins == 0 && legacy.Day == 1 && legacy.CustomersToday == 0 && legacy.CorrectToday == 0 &&
            legacy.EarnedToday == 0 && legacy.TotalServed == 0 && legacy.CustomerVisits.Length == 6 &&
            legacy.CustomerVisits[0] == 0 && legacy.CustomerVisits[1] == 3,
            "missing and invalid save fields are normalized while valid roster history survives");
        legacy.CustomerVisits = null;
        legacy.Normalize();
        Check(legacy.CustomerVisits.Length == 6, "older saves can omit roster history");
        legacy.Coins = int.MaxValue;
        Check(legacy.TryCredit(Resolved(0, 1, true)) && legacy.Coins == int.MaxValue,
            "large saved balances do not wrap negative on payment");
        var bounded = new CustomerVisitModel(0, 1, CoffeeDrink.Espresso, float.NaN);
        Check(bounded.PatienceRemainingSeconds == 420f, "invalid patience budgets use a stable default");
        var shortBudget = new CustomerVisitModel(0, 1, CoffeeDrink.Espresso, 0f);
        var longBudget = new CustomerVisitModel(0, 1, CoffeeDrink.Espresso, 900f);
        Check(shortBudget.PatienceRemainingSeconds == 360f && longBudget.PatienceRemainingSeconds == 480f,
            "customer patience stays within the intended real-time range");
        var poor = Resolved(0, 1, true, -10);
        Check(poor.QualityScore == 0 && poor.Reaction == CustomerVisitReaction.Negative && poor.Tip == 0,
            "poor extraction affects the customer response even when identity matches");
        Console.WriteLine("CUSTOMER_VISIT_TESTS_PASSED " + checks);
    }
}
