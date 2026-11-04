using System;
using CloudCafe.Coffee;

namespace CloudCafe.Crafting
{
    public enum EspressoOrderMatch
    {
        None,
        Match,
        WrongBean,
        WrongRoast,
        WrongBeanAndRoast
    }

    public readonly struct EspressoCustomerOrder
    {
        public string BeanId { get; }
        public EspressoRoast Roast { get; }
        public bool HasRequest => !string.IsNullOrEmpty(BeanId);

        public EspressoCustomerOrder(string beanId, EspressoRoast roast)
        {
            BeanId = beanId;
            Roast = roast;
        }

        public EspressoOrderMatch Match(string beanId, EspressoRoast roast)
        {
            if (!HasRequest) return EspressoOrderMatch.None;
            bool beanMatches = string.Equals(BeanId, beanId, StringComparison.Ordinal);
            bool roastMatches = Roast == roast;
            if (beanMatches) return roastMatches ? EspressoOrderMatch.Match : EspressoOrderMatch.WrongRoast;
            return roastMatches ? EspressoOrderMatch.WrongBean : EspressoOrderMatch.WrongBeanAndRoast;
        }
    }
}
