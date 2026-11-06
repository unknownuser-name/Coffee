using System;

namespace CloudCafe.Crafting
{
    public enum CustomerVisitPhase
    {
        Talking,
        Preparing,
        Reaction
    }

    public enum CustomerVisitReaction
    {
        Positive,
        Neutral,
        Negative
    }

    public sealed class CustomerVisitModel
    {
        public const int CustomerCount = 6;
        public const float MinPatienceSeconds = 180f;
        public const float MaxPatienceSeconds = 240f;

        private readonly float patienceBudgetSeconds;

        public int CustomerIndex { get; }
        public int VisitId { get; }
        public CoffeeDrink Drink { get; }
        public CustomerVisitPhase Phase { get; private set; }
        public bool Clarified { get; private set; }
        public bool Hinted { get; private set; }
        public float PatienceRemainingSeconds { get; private set; }
        public float Patience01 => PatienceRemainingSeconds / patienceBudgetSeconds;
        public int QualityScore { get; private set; }
        public int SatisfactionScore { get; private set; }
        public bool OrderMatched { get; private set; }
        public CustomerVisitReaction Reaction { get; private set; }
        public int BasePrice { get; }
        public int Tip { get; private set; }
        public int Payment { get; private set; }
        public bool Credited { get; private set; }

        public CustomerVisitModel(int customerIndex, int visitId, CoffeeDrink drink,
            float patienceBudgetSeconds = 210f)
        {
            if (customerIndex < 0 || customerIndex >= CustomerCount)
                throw new ArgumentOutOfRangeException(nameof(customerIndex));
            if (visitId <= 0) throw new ArgumentOutOfRangeException(nameof(visitId));

            CustomerIndex = customerIndex;
            VisitId = visitId;
            Drink = drink;
            BasePrice = drink switch
            {
                CoffeeDrink.Espresso => 8,
                CoffeeDrink.Americano => 10,
                CoffeeDrink.Latte => 12,
                _ => throw new ArgumentOutOfRangeException(nameof(drink))
            };
            if (float.IsNaN(patienceBudgetSeconds) || float.IsInfinity(patienceBudgetSeconds))
                patienceBudgetSeconds = 210f;
            this.patienceBudgetSeconds = Math.Max(MinPatienceSeconds,
                Math.Min(MaxPatienceSeconds, patienceBudgetSeconds));
            PatienceRemainingSeconds = this.patienceBudgetSeconds;
            Phase = CustomerVisitPhase.Talking;
        }

        public bool Clarify()
        {
            if (Phase != CustomerVisitPhase.Talking || Clarified) return false;
            Clarified = true;
            return true;
        }

        public bool ShowHint()
        {
            if (Phase != CustomerVisitPhase.Talking || Hinted) return false;
            Hinted = true;
            return true;
        }

        public bool Accept()
        {
            if (Phase != CustomerVisitPhase.Talking) return false;
            Phase = CustomerVisitPhase.Preparing;
            return true;
        }

        public void Tick(float seconds)
        {
            if (Phase != CustomerVisitPhase.Preparing || float.IsNaN(seconds) ||
                float.IsInfinity(seconds) || seconds <= 0f) return;
            PatienceRemainingSeconds = Math.Max(0f, PatienceRemainingSeconds - seconds);
        }

        public bool Resolve(bool orderMatched, int qualityScore)
        {
            if (Phase != CustomerVisitPhase.Preparing) return false;
            Phase = CustomerVisitPhase.Reaction;
            OrderMatched = orderMatched;
            QualityScore = Math.Max(0, Math.Min(100, qualityScore));
            if (!orderMatched)
            {
                Reaction = CustomerVisitReaction.Negative;
                return true;
            }

            SatisfactionScore = (int)Math.Round(QualityScore * .65f + Patience01 * 35f);
            Reaction = SatisfactionScore >= 80 ? CustomerVisitReaction.Positive :
                SatisfactionScore >= 50 ? CustomerVisitReaction.Neutral : CustomerVisitReaction.Negative;
            Tip = (int)Math.Floor(BasePrice * .5f * (QualityScore / 100f) * Patience01);
            Payment = BasePrice + Tip;
            return true;
        }

        internal void MarkCredited() => Credited = true;
    }

    [Serializable]
    public sealed class CustomerCafeProgress
    {
        public const int CustomersPerDay = 5;

        public int Coins;
        public int Day = 1;
        public int CustomersToday;
        public int EarnedToday;
        public int CorrectToday;
        public int TotalServed;
        public int[] CustomerVisits = new int[CustomerVisitModel.CustomerCount];

        public bool DayComplete => CustomersToday >= CustomersPerDay;
        public int NextVisitId => TotalServed < int.MaxValue ? TotalServed + 1 : int.MaxValue;

        public void Normalize()
        {
            Coins = Math.Max(0, Coins);
            Day = Math.Max(1, Day);
            CustomersToday = Math.Max(0, Math.Min(CustomersPerDay, CustomersToday));
            EarnedToday = Math.Max(0, EarnedToday);
            CorrectToday = Math.Max(0, Math.Min(CustomersToday, CorrectToday));
            TotalServed = Math.Max(CustomersToday, TotalServed);
            if (CustomerVisits == null || CustomerVisits.Length != CustomerVisitModel.CustomerCount)
            {
                var visits = new int[CustomerVisitModel.CustomerCount];
                if (CustomerVisits != null)
                    Array.Copy(CustomerVisits, visits, Math.Min(CustomerVisits.Length, visits.Length));
                CustomerVisits = visits;
            }
            for (int i = 0; i < CustomerVisits.Length; i++)
                CustomerVisits[i] = Math.Max(0, CustomerVisits[i]);
        }

        public bool TryCredit(CustomerVisitModel visit)
        {
            Normalize();
            if (visit == null || visit.Phase != CustomerVisitPhase.Reaction || visit.Credited || DayComplete)
                return false;

            Coins = AddSafely(Coins, visit.Payment);
            EarnedToday = AddSafely(EarnedToday, visit.Payment);
            CustomersToday++;
            if (visit.OrderMatched) CorrectToday++;
            TotalServed = AddSafely(TotalServed, 1);
            CustomerVisits[visit.CustomerIndex] = AddSafely(CustomerVisits[visit.CustomerIndex], 1);
            visit.MarkCredited();
            return true;
        }

        public bool NextDay()
        {
            Normalize();
            if (!DayComplete) return false;
            Day = AddSafely(Day, 1);
            CustomersToday = EarnedToday = CorrectToday = 0;
            return true;
        }

        private static int AddSafely(int value, int amount) =>
            (int)Math.Min(int.MaxValue, (long)value + amount);
    }
}
