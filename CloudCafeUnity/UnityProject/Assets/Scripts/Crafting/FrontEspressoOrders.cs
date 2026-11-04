using CloudCafe.Coffee;
using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Rect EspressoOrderButton = new Rect(809, 57, 122, 25);
        private static readonly Color OrderSuccessInk = new Color(.70f, .89f, .74f);
        private static readonly Color OrderMismatchInk = new Color(1f, .72f, .66f);
        private static readonly EspressoRoast[] OrderRoasts =
            { EspressoRoast.Light, EspressoRoast.Medium, EspressoRoast.Dark };

        private bool espressoOrderMenuOpen, espressoOrderSubmitted, espressoOrderServed;
        private EspressoCustomerOrder espressoOrder, espressoShotOrder;
        private EspressoOrderMatch espressoOrderMatch;
        private string draftOrderBeanId;
        private EspressoRoast draftOrderRoast;
        private Vector2 orderMenuScroll;
        private int espressoOrderNumber, nextEspressoOrderIndex;
        private bool EspressoShotServed => espressoOrderSubmitted && espressoOrderServed;

        private void EnsureEspressoOrder()
        {
            if (!espressoOrder.HasRequest) TryAdvanceEspressoOrder();
        }

        private bool TrySetEspressoOrder(string beanId, EspressoRoast roast)
        {
            if (!professionalMode || !StationReadyForSettings() || coffeeBeanCatalog?.Find(beanId) == null ||
                (roast != EspressoRoast.Light && roast != EspressoRoast.Medium && roast != EspressoRoast.Dark))
                return false;

            ApplyEspressoOrder(new EspressoCustomerOrder(beanId, roast));
            return true;
        }

        private void ApplyEspressoOrder(EspressoCustomerOrder order)
        {
            espressoOrder = order;
            espressoOrderServed = false;
            espressoOrderNumber++;
            ClearEspressoOrderShot();
            message = "손님 주문을 받았어요. 주문표의 원두와 로스팅을 맞춰 준비하세요.";
        }

        private bool TryAdvanceEspressoOrder()
        {
            CoffeeBeanDefinition[] beans = coffeeBeanCatalog?.Beans;
            if (!professionalMode || beans == null || beans.Length == 0) return false;
            bool afterCompletedShot = Brew.Complete && espressoOrderServed;
            if (!afterCompletedShot && !StationReadyForSettings()) return false;

            int recipeCount = beans.Length * OrderRoasts.Length;
            for (int offset = 0; offset < recipeCount; offset++)
            {
                int index = (nextEspressoOrderIndex + offset) % recipeCount;
                int beanIndex = index % beans.Length;
                CoffeeBeanDefinition bean = beans[beanIndex];
                EspressoRoast roast = OrderRoasts[(index / beans.Length + beanIndex) % OrderRoasts.Length];
                if (bean == null || string.IsNullOrEmpty(bean.Id) ||
                    espressoOrder.Match(bean.Id, roast) == EspressoOrderMatch.Match)
                    continue;

                if (afterCompletedShot) ResetStation();
                nextEspressoOrderIndex = (index + 1) % recipeCount;
                ApplyEspressoOrder(new EspressoCustomerOrder(bean.Id, roast));
                return true;
            }
            return false;
        }

        private void CaptureEspressoOrder(string beanId, EspressoRoast roast)
        {
            // Keep the order as it stood when the pump was set up for this shot.
            espressoShotOrder = espressoOrderServed ? default : espressoOrder;
            espressoOrderMatch = espressoShotOrder.Match(beanId, roast);
            espressoOrderSubmitted = false;
        }

        private void ClearEspressoOrderShot()
        {
            espressoShotOrder = default;
            espressoOrderMatch = EspressoOrderMatch.None;
            espressoOrderSubmitted = false;
            if (espressoOrderMenuOpen) CloseEspressoOrderMenu();
        }

        private bool TryServeEspressoOrder()
        {
            if (!professionalMode || !Brew.Complete || !espressoShotOrder.HasRequest || espressoOrderSubmitted)
                return false;
            espressoOrderSubmitted = true;
            espressoOrderServed = espressoOrderMatch == EspressoOrderMatch.Match;
            message = espressoOrderServed ? "손님이 주문한 원두와 로스팅을 맞췄어요." : "손님이 주문과 다른 항목을 알려 줬어요.";
            return espressoOrderServed;
        }

        private void OpenEspressoOrderMenu()
        {
            if (!professionalMode || espressoOrderMenuOpen || professionalReportOpen || CoffeeBeanSelectionOpen) return;
            if (!StationReadyForSettings())
            {
                message = "주문은 새 샷을 준비할 때 지정할 수 있어요.";
                return;
            }
            if (professionalMenuOpen) CloseProfessionalOverlay();

            draftOrderBeanId = espressoOrder.HasRequest ? espressoOrder.BeanId : selectedCoffeeBean?.Id;
            draftOrderRoast = espressoOrder.HasRequest ? espressoOrder.Roast : EspressoRoast.Light;
            orderMenuScroll = Vector2.zero;
            SaveProfessionalCursor();
            espressoOrderMenuOpen = true;
        }

        private void CloseEspressoOrderMenu()
        {
            if (!espressoOrderMenuOpen) return;
            espressoOrderMenuOpen = false;
            CloseProfessionalOverlay();
        }

        private bool HandleEspressoOrderInput()
        {
            if (espressoOrderMenuOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.O))
                    CloseEspressoOrderMenu();
                return true;
            }
            if (!professionalMode || smoke || latteActive || paused || professionalReleaseGuard ||
                professionalMenuOpen || professionalReportOpen || CoffeeBeanSelectionOpen)
                return false;

            bool buttonPressed = Input.GetMouseButtonDown(0) && EspressoOrderButton.Contains(StationGuiPointer());
            if (!buttonPressed && !Input.GetKeyDown(KeyCode.O)) return false;
            OpenEspressoOrderMenu();
            return true;
        }

        private bool UpdateEspressoOrderCursor()
        {
            if (espressoOrderMenuOpen)
                Cursor.lockState = CursorLockMode.None;
            else if (!professionalMode || latteActive || professionalMenuOpen || professionalReportOpen || CoffeeBeanSelectionOpen ||
                !StationReadyForSettings() || !EspressoOrderButton.Contains(StationGuiPointer()))
                return false;

            Cursor.visible = true;
            handCursor?.Set(HandPose.Open, Vector2.zero, false);
            return true;
        }

        private void DrawEspressoOrderTicket()
        {
            BeanPanel(new Rect(649, 52, 293, 78), new Color(.07f, .09f, .14f, .94f));
            string title = espressoOrderServed ? $"주문 #{espressoOrderNumber} 완료" : $"손님 주문 #{espressoOrderNumber}";
            if (!espressoOrder.HasRequest) title = "손님 주문";
            GUI.Label(new Rect(661, 57, 148, 24), title, BeanText(14, EspressoAccent, true));
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && StationReadyForSettings();
            GUI.Button(EspressoOrderButton, "손님 주문 [O]", BeanActionStyle());
            GUI.enabled = wasEnabled;

            if (!espressoOrder.HasRequest) return;
            GUI.Label(new Rect(661, 82, 267, 22), EspressoOrderBeanName(espressoOrder.BeanId),
                BeanText(14, EspressoInk, true));
            EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(espressoOrder.BeanId, espressoOrder.Roast);
            GUI.Label(new Rect(661, 105, 267, 21),
                $"{EspressoBeanProfiles.RoastNameKo(espressoOrder.Roast)} · 추천 {profile.GrindSetting}/9 · "
                + $"{profile.TemperatureCelsius:0.0}°C · 1:{profile.BrewRatio:0.00}",
                BeanText(12, EspressoMuted));
        }

        private bool DrawEspressoOrderOverlay()
        {
            if (!espressoOrderMenuOpen) return false;
            GUI.DrawTexture(frame, art.Pixels, ScaleMode.StretchToFill, false);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                Vector3.one * frame.width / 960);
            try
            {
                BeanPanel(new Rect(0, 0, 960, 540), new Color(.03f, .04f, .06f, .79f));
                BeanPanel(new Rect(145, 34, 670, 472), new Color(.10f, .12f, .16f));
                DrawEspressoOrderEditor();
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
            return true;
        }

        private void DrawEspressoOrderEditor()
        {
            GUI.Label(new Rect(167, 51, 530, 34), "손님 주문 지정", BeanText(24, EspressoInk, true));
            GUI.Label(new Rect(167, 91, 626, 22), "손님이 요구할 원두와 로스팅을 고르세요.", BeanText(14, EspressoMuted));
            for (int i = 0; i < OrderRoasts.Length; i++)
            {
                EspressoRoast roast = OrderRoasts[i];
                string label = EspressoBeanProfiles.RoastNameKo(roast);
                if (roast == draftOrderRoast) label = "● " + label;
                if (GUI.Button(new Rect(167 + i * 208, 119, 201, 31), label, BeanActionStyle()))
                    draftOrderRoast = roast;
            }

            CoffeeBeanDefinition[] beans = coffeeBeanCatalog?.Beans;
            if (beans != null) DrawEspressoOrderBeanChoices(beans);
            if (!string.IsNullOrEmpty(draftOrderBeanId))
            {
                EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(draftOrderBeanId, draftOrderRoast);
                GUI.Label(new Rect(167, 414, 626, 27),
                    $"요청 · {EspressoOrderBeanName(draftOrderBeanId)} · {EspressoBeanProfiles.RoastNameKo(draftOrderRoast)}"
                    + $"   |   추천 {profile.GrindSetting}/9 · {profile.TemperatureCelsius:0.0}°C · 1:{profile.BrewRatio:0.00}",
                    BeanText(14, EspressoAccent, true));
            }
            if (GUI.Button(new Rect(167, 452, 116, 34), "닫기 [Esc]", BeanActionStyle()))
                CloseEspressoOrderMenu();
            if (GUI.Button(new Rect(518, 452, 129, 34), "다음 손님", BeanActionStyle()) &&
                TryAdvanceEspressoOrder())
                CloseEspressoOrderMenu();
            if (GUI.Button(new Rect(655, 452, 133, 34), "이 주문 받기", BeanActionStyle()) &&
                TrySetEspressoOrder(draftOrderBeanId, draftOrderRoast))
                CloseEspressoOrderMenu();
        }

        private void DrawEspressoOrderBeanChoices(CoffeeBeanDefinition[] beans)
        {
            const int columns = 3;
            const int rowHeight = 40;
            int rows = (beans.Length + columns - 1) / columns;
            orderMenuScroll = GUI.BeginScrollView(new Rect(167, 159, 625, 244), orderMenuScroll,
                new Rect(0, 0, 606, Mathf.Max(240, rows * rowHeight)));
            try
            {
                for (int i = 0; i < beans.Length; i++)
                {
                    CoffeeBeanDefinition bean = beans[i];
                    if (bean == null) continue;
                    string label = EspressoOrderBeanName(bean);
                    if (bean.Id == draftOrderBeanId) label = "● " + label;
                    Rect rect = new Rect(i % columns * 204, i / columns * rowHeight, 195, 34);
                    if (GUI.Button(rect, label, BeanActionStyle())) draftOrderBeanId = bean.Id;
                }
            }
            finally
            {
                GUI.EndScrollView();
            }
        }

        private void DrawEspressoOrderReport(Rect statusRect, Rect actionButtonRect)
        {
            if (!espressoShotOrder.HasRequest)
            {
                if (espressoOrderServed && GUI.Button(actionButtonRect, "다음 주문", BeanActionStyle()))
                    TryAdvanceEspressoOrder();
                return;
            }

            string requested = $"{EspressoOrderBeanName(espressoShotOrder.BeanId)} · {EspressoBeanProfiles.RoastNameKo(espressoShotOrder.Roast)}";
            string response = "손님 요청 · " + requested;
            Color color = EspressoMuted;
            if (espressoOrderSubmitted)
            {
                response += espressoOrderMatch switch
                {
                    EspressoOrderMatch.Match => "  |  손님: 주문대로 나왔어요!",
                    EspressoOrderMatch.WrongBean => "  |  손님: 다른 원두가 나왔어요.",
                    EspressoOrderMatch.WrongRoast => "  |  손님: 요청한 로스팅과 달라요.",
                    _ => "  |  손님: 원두와 로스팅이 둘 다 달라요."
                };
                color = espressoOrderServed ? OrderSuccessInk : OrderMismatchInk;
            }
            GUI.Label(statusRect, response, BeanText(13, color, true, true));
            if (!espressoOrderSubmitted)
            {
                if (GUI.Button(actionButtonRect, "손님에게 내기", BeanActionStyle()))
                    TryServeEspressoOrder();
            }
            else if (espressoOrderServed && GUI.Button(actionButtonRect, "다음 주문", BeanActionStyle()))
            {
                TryAdvanceEspressoOrder();
            }
        }

        private string EspressoOrderBeanName(string beanId)
        {
            CoffeeBeanDefinition bean = coffeeBeanCatalog?.Find(beanId);
            return bean != null ? EspressoOrderBeanName(bean) : beanId;
        }

        private static string EspressoOrderBeanName(CoffeeBeanDefinition bean)
        {
            string variety = string.IsNullOrEmpty(bean.VarietyKo) ? bean.Variety : bean.VarietyKo;
            return $"{OriginName(bean)} {variety}";
        }
    }
}
