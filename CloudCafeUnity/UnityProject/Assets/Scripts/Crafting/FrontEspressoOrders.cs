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
        private static readonly CoffeeDrink[] OrderDrinks =
            { CoffeeDrink.Espresso, CoffeeDrink.Americano, CoffeeDrink.Latte };

        private bool espressoOrderMenuOpen, espressoOrderSubmitted, espressoOrderServed;
        private EspressoCustomerOrder espressoOrder, espressoShotOrder;
        private EspressoOrderMatch espressoOrderMatch;
        private string draftOrderBeanId;
        private EspressoRoast draftOrderRoast;
        private CoffeeDrink draftOrderDrink;
        private Vector2 orderMenuScroll;
        private int espressoOrderNumber, nextEspressoOrderIndex;
        private bool EspressoShotServed => espressoOrderSubmitted && espressoOrderServed;

        private void EnsureEspressoOrder()
        {
            if (!espressoOrder.HasRequest) TryAdvanceEspressoOrder();
            else if (customerVisit?.Phase == CustomerVisitPhase.Talking)
            {
                EnterCounterView();
                if (counterCustomer.Phase == CounterCustomerPhase.Ordering) OpenCustomerDialogue();
            }
        }

        private bool TrySetEspressoOrder(string beanId, EspressoRoast roast, CoffeeDrink drink = CoffeeDrink.Espresso)
        {
            if (!professionalMode || !StationReadyForSettings() || coffeeBeanCatalog?.Find(beanId) == null ||
                (roast != EspressoRoast.Light && roast != EspressoRoast.Medium && roast != EspressoRoast.Dark) ||
                (drink != CoffeeDrink.Espresso && drink != CoffeeDrink.Americano && drink != CoffeeDrink.Latte))
                return false;

            ApplyEspressoOrder(new EspressoCustomerOrder(beanId, roast, drink));
            return true;
        }

        private void ApplyEspressoOrder(EspressoCustomerOrder order)
        {
            CloseCustomerDialogue();
            ResetCounterVisit();
            customerVisit = null;
            espressoOrder = order;
            espressoOrderServed = false;
            espressoOrderNumber++;
            ClearEspressoOrderShot();
            message = "손님 주문을 받았어요. 주문표에 적힌 음료, 원두, 로스팅을 맞춰 준비하세요.";
        }

        private bool TryAdvanceEspressoOrder() => AdvanceCustomerVisit();

        private void CaptureEspressoOrder(string beanId, EspressoRoast roast)
        {
            // Keep the order as it stood when the pump was set up for this shot.
            espressoShotOrder = espressoOrderServed ? default : espressoOrder;
            espressoOrderMatch = espressoShotOrder.Match(beanId, roast, espressoShotOrder.Drink);
            espressoOrderSubmitted = false;
        }

        private void ClearEspressoOrderShot()
        {
            espressoShotOrder = default;
            espressoOrderMatch = EspressoOrderMatch.None;
            espressoOrderSubmitted = false;
            if (espressoOrderMenuOpen) CloseEspressoOrderMenu();
        }

        private bool TryServeEspressoOrder(CoffeeDrink drink = CoffeeDrink.Espresso)
        {
            if (!professionalMode || !IsDrinkReadyToServe(drink) || !espressoShotOrder.HasRequest || espressoOrderSubmitted ||
                (customerVisit != null && (customerVisit.Phase != CustomerVisitPhase.Preparing ||
                    !counterHandoffInProgress || counterCustomer.Phase != CounterCustomerPhase.Waiting)))
                return false;
            espressoOrderMatch = espressoShotOrder.Match(professionalShotBeanRecipe.BeanId,
                professionalShotBeanRecipe.Roast, drink);
            espressoOrderSubmitted = true;
            espressoOrderServed = espressoOrderMatch == EspressoOrderMatch.Match;
            message = espressoOrderServed ? "손님이 주문한 음료를 완성했어요." : "손님이 주문과 다른 항목을 알려 줬어요.";
            ResolveCustomerVisit(drink);
            return espressoOrderServed;
        }

        private void OpenEspressoOrderMenu()
        {
            if (customerVisit != null)
            {
                if (counterCustomer.Active)
                {
                    EnterCounterView();
                    if (counterCustomer.Phase == CounterCustomerPhase.Ordering ||
                        counterCustomer.Phase == CounterCustomerPhase.Waiting) OpenCustomerDialogue();
                }
                else
                {
                    LeaveCounterView();
                    OpenCustomerDialogue(customerProgress.DayComplete && customerDaySummaryOpen);
                }
                return;
            }
            OpenEspressoOrderEditor();
        }

        private void OpenEspressoOrderEditor()
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
            draftOrderDrink = espressoOrder.HasRequest ? espressoOrder.Drink : CoffeeDrink.Espresso;
            orderMenuScroll = Vector2.zero;
            SaveProfessionalCursor();
            espressoOrderMenuOpen = true;
        }

        private void CloseEspressoOrderMenu()
        {
            if (!espressoOrderMenuOpen) return;
            espressoOrderMenuOpen = false;
            CloseProfessionalOverlay();
            if (customerVisit?.Phase == CustomerVisitPhase.Talking) OpenCustomerDialogue();
        }

        private bool HandleEspressoOrderInput()
        {
            if (professionalMode && customerVisit?.Phase == CustomerVisitPhase.Talking &&
                counterCustomer.Phase == CounterCustomerPhase.Ordering &&
                !espressoOrderMenuOpen && !customerDialogueOpen)
                OpenEspressoOrderMenu();
            if (HandleCustomerDialogueInput()) return true;
            if (espressoOrderMenuOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.O))
                    CloseEspressoOrderMenu();
                return true;
            }
            if (professionalMode && !smoke && !paused && customerVisit != null &&
                !professionalMenuOpen && !professionalReportOpen && !CoffeeBeanSelectionOpen &&
                Input.GetKeyDown(KeyCode.O))
            {
                OpenEspressoOrderMenu();
                return true;
            }
            if (!professionalMode || smoke || latteActive || AmericanoView || paused || professionalReleaseGuard ||
                professionalMenuOpen || professionalReportOpen || CoffeeBeanSelectionOpen)
                return false;

            bool buttonPressed = Input.GetMouseButtonDown(0) && EspressoOrderButton.Contains(StationGuiPointer());
            if (!buttonPressed && !Input.GetKeyDown(KeyCode.O)) return false;
            OpenEspressoOrderMenu();
            return true;
        }

        private bool UpdateEspressoOrderCursor()
        {
            if (UpdateCustomerDialogueCursor()) return true;
            if (espressoOrderMenuOpen)
                Cursor.lockState = CursorLockMode.None;
            else if (!professionalMode || counterView || latteActive || AmericanoView || professionalMenuOpen || professionalReportOpen || CoffeeBeanSelectionOpen ||
                !StationReadyForSettings() || !EspressoOrderButton.Contains(StationGuiPointer()))
                return false;

            Cursor.visible = true;
            handCursor?.Set(HandPose.Open, Vector2.zero, false);
            return true;
        }

        private void DrawEspressoOrderTicket()
        {
            if (customerVisit != null)
            {
                DrawCustomerVisitTicket();
                return;
            }
            BeanPanel(new Rect(649, 52, 293, 99), new Color(.07f, .09f, .14f, .94f));
            string title = espressoOrderServed ? $"주문 #{espressoOrderNumber} 완료" : $"손님 주문 #{espressoOrderNumber}";
            if (!espressoOrder.HasRequest) title = "손님 주문";
            GUI.Label(new Rect(661, 57, 148, 24), title, BeanText(14, EspressoAccent, true));
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && StationReadyForSettings();
            GUI.Button(EspressoOrderButton, "손님 주문 [O]", BeanActionStyle());
            GUI.enabled = wasEnabled;

            if (!espressoOrder.HasRequest) return;
            GUI.Label(new Rect(661, 82, 267, 22), CoffeeDrinkName(espressoOrder.Drink), BeanText(14, EspressoInk, true));
            GUI.Label(new Rect(661, 103, 267, 22),
                $"{EspressoOrderBeanName(espressoOrder.BeanId)} · {EspressoBeanProfiles.RoastNameKo(espressoOrder.Roast)}",
                BeanText(13, EspressoInk));
            EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(espressoOrder.BeanId, espressoOrder.Roast);
            GUI.Label(new Rect(661, 126, 267, 21),
                $"추천 {profile.GrindSetting}/9 · "
                + $"{profile.TemperatureCelsius:0.0}°C · 1:{profile.BrewRatio:0.00}",
                BeanText(12, EspressoMuted));
        }

        private bool DrawEspressoOrderOverlay()
        {
            if (DrawCustomerConversation()) return true;
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
            GUI.Label(new Rect(167, 51, 530, 34), "연습 주문 지정", BeanText(24, EspressoInk, true));
            GUI.Label(new Rect(167, 91, 626, 22), "음료·원두·로스팅을 지정해 제작을 연습하세요.", BeanText(14, EspressoMuted));
            for (int i = 0; i < OrderDrinks.Length; i++)
            {
                CoffeeDrink drink = OrderDrinks[i];
                string label = CoffeeDrinkName(drink);
                if (drink == draftOrderDrink) label = "● " + label;
                if (GUI.Button(new Rect(167 + i * 208, 119, 201, 31), label, BeanActionStyle()))
                    draftOrderDrink = drink;
            }
            for (int i = 0; i < OrderRoasts.Length; i++)
            {
                EspressoRoast roast = OrderRoasts[i];
                string label = EspressoBeanProfiles.RoastNameKo(roast);
                if (roast == draftOrderRoast) label = "● " + label;
                if (GUI.Button(new Rect(167 + i * 208, 158, 201, 31), label, BeanActionStyle()))
                    draftOrderRoast = roast;
            }

            CoffeeBeanDefinition[] beans = coffeeBeanCatalog?.Beans;
            if (beans != null) DrawEspressoOrderBeanChoices(beans);
            if (!string.IsNullOrEmpty(draftOrderBeanId))
            {
                EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(draftOrderBeanId, draftOrderRoast);
                GUI.Label(new Rect(167, 414, 626, 27),
                    $"요청 · {CoffeeDrinkName(draftOrderDrink)} · {EspressoOrderBeanName(draftOrderBeanId)} · {EspressoBeanProfiles.RoastNameKo(draftOrderRoast)}"
                    + $"   |   추천 {profile.GrindSetting}/9 · {profile.TemperatureCelsius:0.0}°C · 1:{profile.BrewRatio:0.00}",
                    BeanText(14, EspressoAccent, true));
            }
            if (GUI.Button(new Rect(167, 452, 116, 34), "닫기 [Esc]", BeanActionStyle()))
                CloseEspressoOrderMenu();
            if (GUI.Button(new Rect(518, 452, 129, 34), "다음 손님", BeanActionStyle()) &&
                TryAdvanceEspressoOrder())
                CloseEspressoOrderMenu();
            if (GUI.Button(new Rect(655, 452, 133, 34), "이 주문 받기", BeanActionStyle()) &&
                TrySetEspressoOrder(draftOrderBeanId, draftOrderRoast, draftOrderDrink))
                CloseEspressoOrderMenu();
        }

        private void DrawEspressoOrderBeanChoices(CoffeeBeanDefinition[] beans)
        {
            const int columns = 3;
            const int rowHeight = 40;
            int rows = (beans.Length + columns - 1) / columns;
            orderMenuScroll = GUI.BeginScrollView(new Rect(167, 198, 625, 205), orderMenuScroll,
                new Rect(0, 0, 606, Mathf.Max(200, rows * rowHeight)));
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

        private void DrawEspressoOrderReport(Rect statusRect, Rect actionButtonRect, CoffeeDrink drink = CoffeeDrink.Espresso)
        {
            if (customerVisit?.Phase == CustomerVisitPhase.Reaction)
            {
                GUI.Label(statusRect, $"{CustomerNames[customerVisit.CustomerIndex]} · 만족도 {customerVisit.SatisfactionScore}점"
                    + $" · 받은 돈 {customerVisit.Payment}코인 (팁 {customerVisit.Tip})", BeanText(13, EspressoAccent, true));
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && counterCustomer.Phase == CounterCustomerPhase.Gone;
                if (GUI.Button(actionButtonRect, customerProgress.DayComplete ? "영업 정산" : "다음 손님", BeanActionStyle()))
                    AdvanceCustomerVisit();
                GUI.enabled = previousEnabled;
                return;
            }
            if (customerVisit?.Phase == CustomerVisitPhase.Preparing)
            {
                GUI.Label(statusRect, "손님 주문 · " + CoffeeDrinkName(espressoShotOrder.Drink),
                    BeanText(13, EspressoMuted, true));
                if (GUI.Button(actionButtonRect, "카운터로 가져가기", BeanActionStyle()))
                    BeginCounterHandoff();
                return;
            }
            if (!espressoShotOrder.HasRequest)
            {
                if (espressoOrderServed && GUI.Button(actionButtonRect, "다음 주문", BeanActionStyle()))
                    TryAdvanceEspressoOrder();
                return;
            }

            string requested = $"{CoffeeDrinkName(espressoShotOrder.Drink)} · {EspressoOrderBeanName(espressoShotOrder.BeanId)} · {EspressoBeanProfiles.RoastNameKo(espressoShotOrder.Roast)}";
            string response = "손님 요청 · " + requested;
            Color color = EspressoMuted;
            if (espressoOrderSubmitted)
            {
                response += espressoOrderServed
                    ? "  |  주문대로 나왔어요!"
                    : "  |  주문과 다른 항목 · " + EspressoOrderMismatches(espressoOrderMatch);
                color = espressoOrderServed ? OrderSuccessInk : OrderMismatchInk;
            }
            GUI.Label(statusRect, response, BeanText(13, color, true, true));
            if (!espressoOrderSubmitted)
            {
                bool wasEnabled = GUI.enabled;
                bool drinkReady = IsDrinkReadyToServe(drink);
                GUI.enabled = wasEnabled && drinkReady;
                if (GUI.Button(actionButtonRect, drinkReady ? "손님에게 내기" : "음료 완성 후 제공", BeanActionStyle()))
                    TryServeEspressoOrder(drink);
                GUI.enabled = wasEnabled;
            }
            else if (espressoOrderServed && GUI.Button(actionButtonRect, "다음 주문", BeanActionStyle()))
            {
                TryAdvanceEspressoOrder();
            }
        }

        private static string CoffeeDrinkName(CoffeeDrink drink)
        {
            return drink switch
            {
                CoffeeDrink.Americano => "아메리카노",
                CoffeeDrink.Latte => "라떼",
                _ => "에스프레소"
            };
        }

        private static string EspressoOrderMismatches(EspressoOrderMatch match)
        {
            string fields = (match & EspressoOrderMatch.WrongDrink) != 0 ? "음료 · " : "";
            if ((match & EspressoOrderMatch.WrongBean) != 0) fields += "원두 · ";
            if ((match & EspressoOrderMatch.WrongRoast) != 0) fields += "로스팅";
            return fields.TrimEnd(' ', '·');
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
