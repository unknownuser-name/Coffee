using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        public readonly AmericanoModel Americano = new AmericanoModel();
        private static readonly Rect WaterButton = new Rect(619, 467, 142, 36);
        private static readonly Rect FinishWaterButton = new Rect(776, 467, 151, 36);
        private CoffeeDrink? finishedDrink;
        private bool latteDrinkSelected;
        private bool AmericanoView => Americano.Active || Americano.Complete;

        private bool TryOpenDrinkReport()
        {
            if (!Brew.Complete || ((AmericanoView || latteActive) && !finishedDrink.HasValue))
                return false;
            if (!professionalReportOpen) OpenShotReport();
            return true;
        }

        private bool TryChooseDrink(CoffeeDrink drink)
        {
            if (paused || !Brew.Complete || finishedDrink.HasValue || AmericanoView || latteActive ||
                EspressoShotServed) return false;

            switch (drink)
            {
                case CoffeeDrink.Espresso:
                    finishedDrink = drink;
                    TryOpenDrinkReport();
                    return true;
                case CoffeeDrink.Americano:
                    if (!Americano.Begin(Brew.InCup)) return false;
                    if (professionalReportOpen) CloseProfessionalOverlay();
                    art.SetAmericanoVisible(true);
                    PositionDrinkCamera();
                    art.UpdateAmericanoArt(Americano);
                    return true;
                case CoffeeDrink.Latte:
                    if (professionalReportOpen) CloseProfessionalOverlay();
                    latteDrinkSelected = true;
                    BeginLatte();
                    return true;
                default:
                    return false;
            }
        }

        private void PositionDrinkCamera()
        {
            art.Camera.transform.position = new Vector3(2.75f, 3.8f, -3.8f);
            art.Camera.transform.LookAt(new Vector3(2.75f, .95f, -1f));
            art.Camera.orthographicSize = .9f;
        }

        private bool TryFinishAmericano()
        {
            if (paused || finishedDrink.HasValue || !Americano.TryFinish()) return false;
            art.UpdateAmericanoArt(Americano);
            finishedDrink = CoffeeDrink.Americano;
            TryOpenDrinkReport();
            return true;
        }

        private bool TryFinishLatteDrink()
        {
            if (!latteDrinkSelected || !latteActive || !Latte.Complete || finishedDrink.HasValue) return false;
            ReleaseLattePointer();
            finishedDrink = CoffeeDrink.Latte;
            TryOpenDrinkReport();
            return true;
        }

        private bool IsDrinkReadyToServe(CoffeeDrink drink) => Brew.Complete && finishedDrink == drink;

        private void ResetDrink()
        {
            if (AmericanoView) art.SetAmericanoVisible(false);
            Americano.Reset();
            finishedDrink = null;
            latteDrinkSelected = false;
        }

        private void UpdateAmericano()
        {
            if (!smoke)
            {
                if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R))
                {
                    ResetStation();
                    return;
                }
                if (Americano.Complete)
                {
                    if (Input.GetKeyDown(KeyCode.Return)) TryOpenDrinkReport();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    TryFinishAmericano();
                    if (Americano.Complete) return;
                }

                bool holdWater = !paused && Application.isFocused && (Input.GetKey(KeyCode.Space) ||
                    (Input.GetMouseButton(0) && WaterButton.Contains(StationGuiPointer())));
                Americano.Step(Time.deltaTime, holdWater);
            }
            art.UpdateAmericanoArt(Americano);
        }

        private void DrawAmericanoGUI()
        {
            GUI.DrawTexture(frame, art.Pixels, ScaleMode.StretchToFill, false);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                Vector3.one * frame.width / 960);
            try
            {
                Color surface = new Color(.07f, .09f, .14f, .95f);
                BeanPanel(new Rect(0, 0, 960, 48), surface);
                BeanPanel(new Rect(0, 452, 960, 88), surface);
                GUI.Label(new Rect(24, 8, 490, 32), "아메리카노 · 온수 붓기", BeanText(21, EspressoInk, true));
                GUI.Label(new Rect(545, 11, 391, 29),
                    $"온수 {Americano.WaterGrams:0.0}g  /  추천 {Americano.TargetWaterGrams:0.0}g",
                    BeanText(18, EspressoAccent, true));
                string hint = Americano.Complete ? "아메리카노 완성 · Enter 리포트 · N 새 커피" :
                    paused ? "잠시 쉬는 중 · Esc로 계속" :
                    $"에스프레소 {Americano.EspressoGrams:0.0}g  ·  총 {Americano.TotalGrams:0.0}g  ·  추천 샷:온수 1:3";
                GUI.Label(new Rect(24, 464, 572, 33), hint, BeanText(16, EspressoInk));
                if (Americano.Complete)
                {
                    if (GUI.Button(FinishWaterButton, "음료 리포트 [Enter]", BeanActionStyle()))
                        TryOpenDrinkReport();
                    return;
                }
                GUI.Label(new Rect(24, 501, 572, 26), "누르는 동안 온수가 나옵니다 · Esc 쉬기 · N 새 커피",
                    BeanText(13, EspressoMuted));

                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && !paused && Americano.TotalGrams < AmericanoModel.CapacityGrams;
                GUI.Button(WaterButton, "온수 붓기 [Space]", BeanActionStyle());
                GUI.enabled = wasEnabled && !paused && Americano.WaterGrams > 0f;
                if (GUI.Button(FinishWaterButton, "완성 [Enter]", BeanActionStyle()))
                    TryFinishAmericano();
                GUI.enabled = wasEnabled;
            }
            finally
            {
                GUI.matrix = previous;
            }
        }

        private void DrawBasicDrinkReport()
        {
            BeanPanel(new Rect(109, 106, 742, 320), new Color(.10f, .12f, .16f));
            string title = finishedDrink.HasValue ? CoffeeDrinkName(finishedDrink.Value) + " 완성" : "음료 선택";
            GUI.Label(new Rect(134, 129, 692, 36), title, BeanText(25, EspressoInk, true));
            GUI.Label(new Rect(134, 180, 692, 27), $"추출한 에스프레소  {Brew.InCup:0.0}g",
                BeanText(17, EspressoMuted));
            DrawDrinkChoiceOrSummary(231);
            DrawDrinkReportNavigation(369);
        }

        private void DrawDrinkChoiceOrSummary(float y)
        {
            if (!finishedDrink.HasValue)
            {
                DrawDrinkChoice(CoffeeDrink.Espresso, "에스프레소 그대로", 134, y);
                DrawDrinkChoice(CoffeeDrink.Americano, "아메리카노 · 온수", 367, y);
                DrawDrinkChoice(CoffeeDrink.Latte, "라떼 · 우유 붓기", 600, y);
                return;
            }

            string summary = finishedDrink.Value switch
            {
                CoffeeDrink.Americano => $"아메리카노 완성 · 샷 {Americano.EspressoGrams:0.0}g + 온수 {Americano.WaterGrams:0.0}g"
                    + $" = {Americano.TotalGrams:0.0}g  ·  샷:온수 1:{Americano.WaterToEspressoRatio:0.00}",
                CoffeeDrink.Latte => $"라떼 완성 · 샷 {Brew.InCup:0.0}g + 우유 {Latte.Cup:0}ml",
                _ => $"에스프레소 완성 · 추출 {Brew.InCup:0.0}g"
            };
            BeanPanel(new Rect(134, y, 692, 36), new Color(.14f, .16f, .20f));
            GUI.Label(new Rect(145, y + 4, 669, 28), summary, BeanText(15, EspressoAccent, true));
        }

        private void DrawDrinkChoice(CoffeeDrink drink, string text, float x, float y)
        {
            if (GUI.Button(new Rect(x, y, 226, 36), text, BeanActionStyle()))
                TryChooseDrink(drink);
        }

        private void DrawDrinkReportNavigation(float y)
        {
            if (GUI.Button(new Rect(134, y, 126, 34), "닫기 [Esc]", BeanActionStyle()))
                CloseProfessionalOverlay();
            if (GUI.Button(new Rect(702, y, 124, 34), "새 샷 [R]", BeanActionStyle()))
                ResetStation();
        }
    }
}
