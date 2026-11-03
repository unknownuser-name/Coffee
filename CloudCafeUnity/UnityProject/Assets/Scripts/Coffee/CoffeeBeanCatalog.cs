using System;
using UnityEngine;

namespace CloudCafe.Coffee
{
    [CreateAssetMenu(menuName = "Cloud Cafe/Coffee Bean Catalog", fileName = "CoffeeBeanCatalog")]
    public sealed class CoffeeBeanCatalog : ScriptableObject
    {
        public CoffeeBeanDefinition[] Beans = new CoffeeBeanDefinition[0];

        public CoffeeBeanDefinition Find(string id)
        {
            if (Beans == null || string.IsNullOrEmpty(id)) return null;
            foreach (CoffeeBeanDefinition bean in Beans)
                if (bean != null && string.Equals(bean.Id, id, StringComparison.Ordinal)) return bean;
            return null;
        }

        public static CoffeeBeanCatalog Load()
        {
            return Resources.Load<CoffeeBeanCatalog>("CoffeeBags/CoffeeBeanCatalog");
        }
    }
}
