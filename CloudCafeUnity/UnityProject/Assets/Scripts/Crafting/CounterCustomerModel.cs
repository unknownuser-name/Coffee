using System;

namespace CloudCafe.Crafting
{
    public enum CounterCustomerPhase
    {
        Gone,
        Arriving,
        Ordering,
        Waiting,
        Receiving,
        Leaving
    }

    public sealed class CounterCustomerModel
    {
        public const float ArrivalSeconds = 2f;
        public const float ReceivingSeconds = 2f;
        public const float LeavingSeconds = 2f;

        private float phaseSeconds;
        private bool drinkReceived;

        public int CustomerIndex { get; private set; } = -1;
        public CounterCustomerPhase Phase { get; private set; } = CounterCustomerPhase.Gone;
        public bool Active => Phase != CounterCustomerPhase.Gone;
        public bool HoldingDrink => drinkReceived;

        public float PhaseProgress
        {
            get
            {
                float duration = PhaseDuration;
                return duration > 0f ? Math.Min(1f, phaseSeconds / duration) : 0f;
            }
        }

        public float CounterProgress => Phase switch
        {
            CounterCustomerPhase.Arriving => PhaseProgress,
            CounterCustomerPhase.Leaving => 1f - PhaseProgress,
            CounterCustomerPhase.Gone => 0f,
            _ => 1f
        };

        private float PhaseDuration => Phase switch
        {
            CounterCustomerPhase.Arriving => ArrivalSeconds,
            CounterCustomerPhase.Receiving => ReceivingSeconds,
            CounterCustomerPhase.Leaving => LeavingSeconds,
            _ => 0f
        };

        public bool Begin(int customerIndex)
        {
            if (Active || customerIndex < 0 || customerIndex >= CustomerVisitModel.CustomerCount) return false;
            CustomerIndex = customerIndex;
            phaseSeconds = 0f;
            drinkReceived = false;
            Phase = CounterCustomerPhase.Arriving;
            return true;
        }

        public bool AcceptOrder()
        {
            if (Phase != CounterCustomerPhase.Ordering) return false;
            Phase = CounterCustomerPhase.Waiting;
            phaseSeconds = 0f;
            return true;
        }

        public bool TryHandOver()
        {
            if (Phase != CounterCustomerPhase.Waiting) return false;
            Phase = CounterCustomerPhase.Receiving;
            phaseSeconds = 0f;
            drinkReceived = true;
            return true;
        }

        public void Step(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) return;
            while (dt > 0f)
            {
                float duration = PhaseDuration;
                if (duration <= 0f) return;
                float remaining = duration - phaseSeconds;
                if (dt < remaining)
                {
                    phaseSeconds += dt;
                    return;
                }

                dt -= remaining;
                phaseSeconds = 0f;
                Phase = Phase switch
                {
                    CounterCustomerPhase.Arriving => CounterCustomerPhase.Ordering,
                    CounterCustomerPhase.Receiving => CounterCustomerPhase.Leaving,
                    _ => CounterCustomerPhase.Gone
                };
            }
        }

        public void Reset()
        {
            CustomerIndex = -1;
            phaseSeconds = 0f;
            drinkReceived = false;
            Phase = CounterCustomerPhase.Gone;
        }
    }
}
