using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindArt
    {
        private static readonly Vector3 HotWaterOutlet = new Vector3(0, .625f, 0);
        private static readonly Color EspressoColor = new Color32(86, 44, 25, 255);
        private static readonly Color DilutedColor = new Color32(125, 72, 43, 255);

        private Transform americanoStation;
        private Transform americanoSurface;
        private Transform americanoStream;
        private Transform americanoRipple;
        private Material americanoLiquid;
        private bool espressoCupWasVisible;

        public void SetAmericanoVisible(bool visible)
        {
            if (visible)
            {
                EnsureAmericanoStation();
                if (americanoStation.gameObject.activeSelf) return;

                espressoCupWasVisible = Cup.gameObject.activeSelf;
                Cup.gameObject.SetActive(false);
                americanoStation.gameObject.SetActive(true);
            }
            else if (americanoStation != null && americanoStation.gameObject.activeSelf)
            {
                americanoStream.gameObject.SetActive(false);
                americanoRipple.gameObject.SetActive(false);
                americanoStation.gameObject.SetActive(false);
                Cup.gameObject.SetActive(espressoCupWasVisible);
            }
        }

        public void UpdateAmericanoArt(AmericanoModel drink)
        {
            if (americanoStation == null || !americanoStation.gameObject.activeSelf) return;

            float fill = Mathf.Clamp01(drink.TotalGrams / AmericanoModel.CapacityGrams);
            float level = Mathf.Lerp(.043f, .302f, fill);
            float radiusScale = Mathf.Lerp(.189f, .215f, fill) / .189f;
            americanoSurface.localPosition = Vector3.up * level;
            americanoSurface.localScale = new Vector3(radiusScale, 1, radiusScale);
            americanoSurface.gameObject.SetActive(drink.TotalGrams > 0f);
            float dilution = drink.TotalGrams > 0f ? drink.WaterGrams / drink.TotalGrams : 0f;
            americanoLiquid.color = Color.Lerp(EspressoColor, DilutedColor, dilution);

            americanoStream.gameObject.SetActive(drink.Pouring);
            americanoRipple.gameObject.SetActive(drink.Pouring);
            if (!drink.Pouring) return;

            Vector3 impact = Vector3.up * (level + .003f);
            Vector3 stream = HotWaterOutlet - impact;
            americanoStream.localPosition = (HotWaterOutlet + impact) / 2f;
            americanoStream.localScale = new Vector3(1, stream.magnitude, 1);
            americanoRipple.localPosition = impact;
            float rippleSize = 1f + Mathf.Sin(Time.time * 17f) * .22f;
            americanoRipple.localScale = new Vector3(rippleSize, 1, rippleSize);
        }

        private void EnsureAmericanoStation()
        {
            if (americanoStation != null) return;

            americanoStation = new GameObject("Americano hot water station").transform;
            americanoStation.SetParent(root, false);
            americanoStation.localPosition = new Vector3(2.5f, .62f, -1f);
            BuildHotWaterDispenser();
            BuildAmericanoMug();

            americanoStation.gameObject.SetActive(false);
        }

        private void BuildHotWaterDispenser()
        {
            Cube("water station foot", americanoStation, new Vector3(.28f, -.59f, .03f),
                new Vector3(1.30f, .06f, .60f), dark);
            Cube("water station column", americanoStation, new Vector3(.70f, .07f, .16f),
                new Vector3(.34f, 1.28f, .42f), steel);
            Cube("water station control panel", americanoStation, new Vector3(.70f, .34f, -.057f),
                new Vector3(.26f, .30f, .015f), dark);
            Cube("hot water indicator", americanoStation, new Vector3(.70f, .38f, -.070f),
                new Vector3(.14f, .032f, .014f), gold);
            Cube("water station drip tray", americanoStation, new Vector3(.23f, -.045f, 0),
                new Vector3(1.14f, .06f, .57f), dark);
            Cylinder("Americano saucer", americanoStation, new Vector3(0, -.022f, 0), .295f, .041f, light);
            Cube("hot water arm", americanoStation, new Vector3(.33f, .72f, 0),
                new Vector3(.77f, .095f, .13f), steel);
            Cylinder("hot water nozzle", americanoStation, HotWaterOutlet + Vector3.up * .034f, .041f, .068f, light);
            americanoStream = Cylinder("hot water stream", americanoStation, Vector3.zero,
                .010f, 1f, MatColor("water glint", "#91C9D5"));
            americanoStream.gameObject.SetActive(false);
        }

        private void BuildAmericanoMug()
        {
            Material ceramic = MatColor("sea glass ceramic", "#61918F");
            Lathe("Americano mug ceramic shell", americanoStation, Vector3.zero, new[]
            {
                new Vector2(.177f, 0f), new Vector2(.211f, .028f),
                new Vector2(.236f, .21f), new Vector2(.238f, .334f),
                new Vector2(.215f, .334f), new Vector2(.211f, .21f),
                new Vector2(.187f, .034f), new Vector2(0, .034f)
            }, ceramic, 32);
            Ring("Americano mug cream rim", americanoStation, Vector3.up * .332f,
                .240f, .213f, .010f, light);
            Transform handle = Ring("Americano mug handle", americanoStation, new Vector3(-.259f, .172f, 0),
                .115f, .077f, .047f, ceramic);
            handle.localRotation = Quaternion.Euler(90, 0, 0);

            americanoLiquid = MatColor("Americano liquid", "#562C19");
            americanoSurface = Cylinder("Americano liquid surface", americanoStation, Vector3.zero,
                .189f, .006f, americanoLiquid);
            americanoRipple = Ring("hot water impact ripple", americanoStation, Vector3.zero,
                .037f, .029f, .002f, gold);
            americanoRipple.gameObject.SetActive(false);
        }
    }
}
