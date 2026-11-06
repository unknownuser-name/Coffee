using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindArt
    {
        public Vector3 CounterCameraPosition => new Vector3(8.3f, 2f, -4.2f);
        public Vector3 CounterCameraTarget => new Vector3(8.3f, .55f, 0);
        public float CounterCameraSize => 1.55f;
        public Vector3 CounterCupHome => new Vector3(8.3f, .24f, -.62f);
        public Vector3 CounterReceivePoint => new Vector3(8.3f, .24f, .18f);
        public Transform CounterCup { get; private set; }
        public Transform CounterSpeechAnchor { get; private set; }

        private Transform counterStation, counterCustomer, counterHeldCup, counterHead;
        private Transform counterLeftBrow, counterRightBrow, counterMouthLeft, counterMouthRight;
        private Transform counterLeftEye, counterRightEye;
        private Transform counterCupArt, counterHeldArt;
        private readonly Transform[] counterArms = new Transform[4];
        private readonly Transform[] counterHands = new Transform[2];
        private readonly Transform[] counterLegs = new Transform[2];
        private readonly Transform[] counterShoes = new Transform[2];
        private readonly GameObject[] counterAccessories = new GameObject[6];
        private Material counterSkin, counterHair, counterClothes, counterLiquid, counterPaper;
        private float counterPriorProgress;
        private int counterPriorIndex = -1;

        private static readonly Color[] CounterSkinPalette =
        {
            new Color32(194, 133, 95, 255), new Color32(240, 190, 161, 255),
            new Color32(120, 77, 58, 255), new Color32(209, 151, 110, 255),
            new Color32(163, 103, 77, 255), new Color32(216, 178, 139, 255)
        };
        private static readonly Color[] CounterHairPalette =
        {
            new Color32(56, 44, 42, 255), new Color32(110, 57, 43, 255),
            new Color32(215, 215, 198, 255), new Color32(48, 43, 42, 255),
            new Color32(64, 44, 38, 255), new Color32(223, 223, 212, 255)
        };
        private static readonly Color[] CounterClothesPalette =
        {
            new Color32(187, 129, 60, 255), new Color32(65, 120, 104, 255),
            new Color32(157, 76, 78, 255), new Color32(94, 148, 143, 255),
            new Color32(138, 120, 166, 255), new Color32(70, 101, 128, 255)
        };

        public void SetCounterVisible(bool visible)
        {
            if (visible) EnsureCounterStation();
            if (counterStation != null) counterStation.gameObject.SetActive(visible);
        }

        public void SetCounterCustomerVisible(bool visible)
        {
            if (visible) EnsureCounterStation();
            if (counterCustomer != null) counterCustomer.gameObject.SetActive(visible);
        }

        public void ShowCounterDrink(bool visible)
        {
            if (visible) EnsureCounterStation();
            if (CounterCup != null) CounterCup.gameObject.SetActive(visible);
        }

        public void SetCounterDrink(CoffeeDrink drink)
        {
            EnsureCounterStation();
            bool espresso = drink == CoffeeDrink.Espresso;
            bool latte = drink == CoffeeDrink.Latte;
            Vector3 scale = espresso ? new Vector3(.72f, .68f, .72f) : Vector3.one;
            CounterCup.localScale = scale;
            counterHeldCup.localScale = scale;
            counterLiquid.color = latte ? new Color32(183, 127, 77, 255) :
                espresso ? new Color32(183, 125, 66, 255) : new Color32(77, 42, 27, 255);
            counterCupArt.gameObject.SetActive(latte);
            counterHeldArt.gameObject.SetActive(latte);
        }

        public void UpdateCounterCustomer(int customerIndex, float atCounter, int expression,
            bool holdingDrink, CoffeeDrink drink)
        {
            EnsureCounterStation();
            int index = ((customerIndex % CounterSkinPalette.Length) + CounterSkinPalette.Length) % CounterSkinPalette.Length;
            float progress = Mathf.Clamp01(atCounter);
            bool walking = Mathf.Abs(progress - counterPriorProgress) > .0001f;
            if (index != counterPriorIndex)
            {
                counterSkin.color = CounterSkinPalette[index];
                counterHair.color = CounterHairPalette[index];
                counterClothes.color = CounterClothesPalette[index];
                for (int i = 0; i < counterAccessories.Length; i++)
                    counterAccessories[i].SetActive(i == index);
                counterPriorIndex = index;
            }

            float stride = walking ? Mathf.Sin(Time.time * 11f) : 0;
            counterCustomer.localPosition = new Vector3(Mathf.Lerp(11.45f, 8.3f, progress),
                walking ? Mathf.Abs(stride) * .026f : 0, .76f);
            counterHead.localRotation = Quaternion.Euler(0, walking ? 8f * stride : 0, 0);
            for (int side = 0; side < 2; side++)
            {
                float direction = side == 0 ? -1f : 1f;
                float legStride = stride * direction;
                counterLegs[side].localRotation = Quaternion.Euler(legStride * 18f, 0, 0);
                counterShoes[side].localPosition = new Vector3(direction * .13f, -1.11f,
                    -.06f + legStride * .09f);
                Vector3 shoulder = new Vector3(direction * .33f, .67f, 0);
                Vector3 hand = holdingDrink && side == 0 ? new Vector3(-.30f, .34f, -.42f) :
                    new Vector3(direction * .43f, .03f, stride * direction * .08f);
                Vector3 elbow = Vector3.Lerp(shoulder, hand, .55f) + Vector3.back * .045f;
                SetCounterLimb(counterArms[side * 2], shoulder, elbow, .14f);
                SetCounterLimb(counterArms[side * 2 + 1], elbow, hand, .11f);
                counterHands[side].localPosition = hand;
            }

            float mood = Mathf.Clamp(expression, -1, 1);
            counterLeftBrow.localEulerAngles = new Vector3(0, 0, mood < 0 ? -17f : 0f);
            counterRightBrow.localEulerAngles = new Vector3(0, 0, mood < 0 ? 17f : 0f);
            counterLeftEye.localScale = new Vector3(.047f, mood > 0 ? .025f : .052f, .025f);
            counterRightEye.localScale = counterLeftEye.localScale;
            counterMouthLeft.localEulerAngles = new Vector3(0, 0, mood * -22f);
            counterMouthRight.localEulerAngles = new Vector3(0, 0, mood * 22f);
            counterMouthLeft.localPosition = new Vector3(-.028f, -.102f, -.223f);
            counterMouthRight.localPosition = new Vector3(.028f, -.102f, -.223f);
            counterHeldCup.gameObject.SetActive(holdingDrink);
            SetCounterDrink(drink);
            counterPriorProgress = progress;
        }

        private static void SetCounterLimb(Transform limb, Vector3 from, Vector3 to, float thickness)
        {
            limb.localPosition = (from + to) * .5f;
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
            limb.localScale = new Vector3(thickness, (to - from).magnitude, thickness);
        }

        private void EnsureCounterStation()
        {
            if (counterStation != null) return;
            counterStation = new GameObject("Customer service counter").transform;
            counterStation.SetParent(root, false);
            counterPaper = MatColor("counter paper cream", "#F4E8D1");
            counterSkin = MatColor("counter customer skin", "#C2855F");
            counterHair = MatColor("counter customer hair", "#382C2A");
            counterClothes = MatColor("counter customer clothing", "#BB813C");
            counterLiquid = MatColor("counter finished drink", "#B77D42");
            Material counterWood = MatColor("front counter walnut", "#825D49");
            Material floor = MatColor("customer area floor", "#C3AD8C");
            Material backdrop = MatColor("customer wall sage", "#728B83");
            Cube("customer floor", counterStation, new Vector3(9.15f, -1.24f, .45f), new Vector3(6.3f, .12f, 3.4f), floor);
            Cube("customer back wall", counterStation, new Vector3(9.15f, .20f, 2.10f), new Vector3(6.3f, 3f, .12f), backdrop);
            Cube("entry door", counterStation, new Vector3(11.1f, -.1f, 2.02f), new Vector3(.86f, 2.2f, .04f), dark);
            Cube("entry door glass", counterStation, new Vector3(11.1f, .18f, 1.98f), new Vector3(.65f, 1.36f, .018f), steel);
            Cube("counter wooden front", counterStation, new Vector3(8.3f, -.49f, -.15f), new Vector3(3.3f, 1.34f, .92f), counterWood);
            Cube("counter cream face", counterStation, new Vector3(8.3f, -.49f, -.62f), new Vector3(3.12f, 1.14f, .032f), counterPaper);
            Cube("counter walnut top", counterStation, new Vector3(8.3f, .20f, -.16f), new Vector3(3.55f, .08f, 1.28f), counterWood);
            Cube("counter brass trim", counterStation, new Vector3(8.3f, .16f, -.81f), new Vector3(3.55f, .035f, .025f), gold);
            Ring("handoff marker", counterStation, CounterReceivePoint + new Vector3(0, .007f, 0), .30f, .265f, .008f, gold);
            Cylinder("finished drink saucer", counterStation, CounterCupHome - new Vector3(0, .006f, 0), .30f, .016f, counterPaper);
            CounterCup = new GameObject("finished drink to hand over").transform;
            CounterCup.SetParent(counterStation, false);
            CounterCup.localPosition = CounterCupHome;
            BuildCounterCup(CounterCup, out counterCupArt, true);
            CounterCup.gameObject.SetActive(false);
            BuildCounterCustomer();
        }

        private void BuildCounterCustomer()
        {
            counterCustomer = new GameObject("customer at service counter").transform;
            counterCustomer.SetParent(counterStation, false);
            CounterPrimitive("customer torso", counterCustomer, new Vector3(0, .32f, 0),
                new Vector3(.59f, .88f, .34f), PrimitiveType.Capsule, counterClothes);
            CounterPrimitive("customer neck", counterCustomer, new Vector3(0, .83f, 0),
                new Vector3(.17f, .24f, .17f), PrimitiveType.Capsule, counterSkin);
            counterHead = new GameObject("customer expressive head").transform;
            counterHead.SetParent(counterCustomer, false);
            counterHead.localPosition = new Vector3(0, 1.12f, 0);
            CounterPrimitive("customer face", counterHead, Vector3.zero, new Vector3(.46f, .59f, .44f), PrimitiveType.Sphere, counterSkin);
            CounterPrimitive("customer hair cap", counterHead, new Vector3(0, .14f, .025f),
                new Vector3(.49f, .35f, .44f), PrimitiveType.Sphere, counterHair);
            CounterPrimitive("customer nose", counterHead, new Vector3(0, -.035f, -.223f),
                new Vector3(.065f, .08f, .075f), PrimitiveType.Sphere, counterSkin);
            foreach (float side in new[] { -.235f, .235f })
                CounterPrimitive("customer ear", counterHead, new Vector3(side, -.015f, 0),
                    new Vector3(.08f, .13f, .08f), PrimitiveType.Sphere, counterSkin);
            counterLeftEye = CounterPrimitive("left eye", counterHead, new Vector3(-.09f, .035f, -.204f), new Vector3(.047f, .052f, .025f), PrimitiveType.Sphere, dark);
            counterRightEye = CounterPrimitive("right eye", counterHead, new Vector3(.09f, .035f, -.204f), new Vector3(.047f, .052f, .025f), PrimitiveType.Sphere, dark);
            counterLeftBrow = Cube("left brow", counterHead, new Vector3(-.09f, .09f, -.205f), new Vector3(.075f, .017f, .025f), counterHair);
            counterRightBrow = Cube("right brow", counterHead, new Vector3(.09f, .09f, -.205f), new Vector3(.075f, .017f, .025f), counterHair);
            counterMouthLeft = Cube("left smile", counterHead, new Vector3(-.028f, -.102f, -.223f), new Vector3(.067f, .017f, .018f), dark);
            counterMouthRight = Cube("right smile", counterHead, new Vector3(.028f, -.102f, -.223f), new Vector3(.067f, .017f, .018f), dark);
            for (int side = 0; side < 2; side++)
            {
                float direction = side == 0 ? -1f : 1f;
                counterLegs[side] = CounterPrimitive("customer trouser leg", counterCustomer,
                    new Vector3(direction * .13f, -.62f, 0), new Vector3(.19f, .96f, .20f), PrimitiveType.Capsule, dark);
                counterShoes[side] = CounterPrimitive("customer shoe", counterCustomer,
                    new Vector3(direction * .13f, -1.11f, -.06f), new Vector3(.23f, .12f, .35f), PrimitiveType.Cube, counterHair);
                counterArms[side * 2] = Cylinder("customer upper arm", counterCustomer, Vector3.zero, .5f, 1f, counterClothes);
                counterArms[side * 2 + 1] = Cylinder("customer forearm", counterCustomer, Vector3.zero, .5f, 1f, counterSkin);
                counterHands[side] = CounterPrimitive("customer hand", counterCustomer, Vector3.zero,
                    new Vector3(.14f, .15f, .12f), PrimitiveType.Sphere, counterSkin);
            }
            BuildCounterAccessories();
            counterHeldCup = new GameObject("drink received by customer").transform;
            counterHeldCup.SetParent(counterCustomer, false);
            counterHeldCup.localPosition = new Vector3(-.30f, .32f, -.44f);
            BuildCounterCup(counterHeldCup, out counterHeldArt, false);
            counterHeldCup.gameObject.SetActive(false);
            CounterSpeechAnchor = new GameObject("customer speech anchor").transform;
            CounterSpeechAnchor.SetParent(counterCustomer, false);
            CounterSpeechAnchor.localPosition = new Vector3(0, 1.75f, 0);
        }

        private void BuildCounterAccessories()
        {
            for (int i = 0; i < counterAccessories.Length; i++)
            {
                counterAccessories[i] = new GameObject("customer style " + i);
                counterAccessories[i].transform.SetParent(counterHead, false);
                counterAccessories[i].SetActive(false);
            }
            Cube("editor cap brim", counterAccessories[0].transform, new Vector3(0, .17f, -.19f), new Vector3(.55f, .055f, .35f), counterClothes);
            CounterPrimitive("editor cap crown", counterAccessories[0].transform, new Vector3(0, .24f, .015f), new Vector3(.47f, .22f, .42f), PrimitiveType.Sphere, counterClothes);
            Cube("student bob left", counterAccessories[1].transform, new Vector3(-.215f, -.01f, .025f), new Vector3(.10f, .40f, .34f), counterHair);
            Cube("student bob right", counterAccessories[1].transform, new Vector3(.215f, -.01f, .025f), new Vector3(.10f, .40f, .34f), counterHair);
            foreach (float x in new[] { -.10f, .10f })
            {
                Transform frame = Ring("flower shop glasses", counterAccessories[2].transform,
                    new Vector3(x, .035f, -.23f), .078f, .060f, .018f, dark);
                frame.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Cube("glasses bridge", counterAccessories[2].transform, new Vector3(0, .04f, -.235f), new Vector3(.06f, .014f, .02f), dark);
            CounterPrimitive("photographer hair bun", counterAccessories[3].transform, new Vector3(.06f, .31f, .10f), new Vector3(.22f, .22f, .22f), PrimitiveType.Sphere, counterHair);
            foreach (float x in new[] { -.26f, .26f })
                CounterPrimitive("writer headphone", counterAccessories[4].transform, new Vector3(x, .03f, .02f), new Vector3(.11f, .21f, .16f), PrimitiveType.Capsule, dark);
            Cube("writer headphone band", counterAccessories[4].transform, new Vector3(0, .31f, .025f), new Vector3(.47f, .045f, .06f), dark);
            foreach (float x in new[] { -.04f, .04f })
                CounterPrimitive("regular moustache", counterAccessories[5].transform, new Vector3(x, -.08f, -.234f), new Vector3(.10f, .045f, .025f), PrimitiveType.Sphere, counterHair);
        }

        private void BuildCounterCup(Transform parent, out Transform latteArt, bool interactive)
        {
            Lathe("finished ceramic mug", parent, Vector3.zero, new[]
            {
                new Vector2(.13f, 0), new Vector2(.19f, .025f), new Vector2(.205f, .28f),
                new Vector2(.18f, .28f), new Vector2(.16f, .035f), new Vector2(0, .035f)
            }, counterPaper, 32);
            Ring("finished mug rim", parent, new Vector3(0, .278f, 0), .207f, .179f, .009f, light);
            Transform handle = Ring("finished mug handle", parent, new Vector3(.24f, .15f, 0), .105f, .068f, .037f, counterPaper);
            handle.localRotation = Quaternion.Euler(90, 0, 0);
            Cylinder("finished drink surface", parent, new Vector3(0, .246f, 0), .178f, .007f, counterLiquid);
            latteArt = new GameObject("latte white leaf").transform;
            latteArt.SetParent(parent, false);
            for (int i = 0; i < 5; i++)
            {
                float z = -.08f + i * .035f;
                float radius = .022f + (4 - i) * .008f;
                CounterPrimitive("white latte leaf", latteArt, new Vector3(0, .253f, z), new Vector3(radius * 2f, .004f, .038f), PrimitiveType.Sphere, light);
            }
            if (!interactive) return;
            BoxCollider body = parent.gameObject.AddComponent<BoxCollider>();
            body.center = new Vector3(0, .145f, 0);
            body.size = new Vector3(.43f, .30f, .43f);
            handle.gameObject.AddComponent<BoxCollider>().size = new Vector3(.25f, .08f, .25f);
        }

        private static Transform CounterPrimitive(string name, Transform parent, Vector3 position,
            Vector3 scale, PrimitiveType primitive, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(primitive);
            item.name = name;
            Object.Destroy(item.GetComponent<Collider>());
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            if (primitive == PrimitiveType.Capsule) scale.y *= .5f;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item.transform;
        }
    }
}
