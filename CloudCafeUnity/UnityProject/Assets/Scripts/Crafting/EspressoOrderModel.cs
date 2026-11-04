using System;
using CloudCafe.Coffee;

namespace CloudCafe.Crafting
{
    public enum CoffeeDrink
    {
        Espresso,
        Americano,
        Latte
    }

    [Flags]
    public enum EspressoOrderMatch
    {
        None = 0,
        Match = 1,
        WrongBean = 2,
        WrongRoast = 4,
        WrongBeanAndRoast = WrongBean | WrongRoast,
        WrongDrink = 8
    }

    public readonly struct EspressoCustomerOrder
    {
        public string BeanId { get; }
        public EspressoRoast Roast { get; }
        public CoffeeDrink Drink { get; }
        public bool HasRequest => !string.IsNullOrEmpty(BeanId);

        public EspressoCustomerOrder(string beanId, EspressoRoast roast, CoffeeDrink drink = CoffeeDrink.Espresso)
        {
            BeanId = beanId;
            Roast = roast;
            Drink = drink;
        }

        public EspressoOrderMatch Match(string beanId, EspressoRoast roast, CoffeeDrink drink = CoffeeDrink.Espresso)
        {
            if (!HasRequest) return EspressoOrderMatch.None;
            var mismatches = EspressoOrderMatch.None;
            if (!string.Equals(BeanId, beanId, StringComparison.Ordinal)) mismatches |= EspressoOrderMatch.WrongBean;
            if (Roast != roast) mismatches |= EspressoOrderMatch.WrongRoast;
            if (Drink != drink) mismatches |= EspressoOrderMatch.WrongDrink;
            return mismatches == EspressoOrderMatch.None ? EspressoOrderMatch.Match : mismatches;
        }
    }
}
