using System;
using CloudCafe.Coffee;
using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private const string CustomerProgressKey = "CloudCafe.CustomerProgress.v1";
        private static readonly string[] CustomerNames = { "윤서", "민호", "소담", "도윤", "하린", "은재" };
        private static readonly string[] CustomerRoles = { "출근길 편집자", "대학원생", "동네 꽃집 주인", "사진가", "소설가", "카페 단골" };
        private CustomerCafeProgress customerProgress;
        private CustomerVisitModel customerVisit;
        private bool customerDialogueOpen, customerDaySummaryOpen;
        private CursorLockMode customerPriorCursorLock;
        private bool customerPriorCursorVisible;

        private void InitializeCustomerProgress()
        {
            if (customerProgress != null) return;
            if (!smoke && PlayerPrefs.HasKey(CustomerProgressKey))
            {
                try { customerProgress = JsonUtility.FromJson<CustomerCafeProgress>(PlayerPrefs.GetString(CustomerProgressKey)); }
                catch (ArgumentException) { customerProgress = null; }
            }
            customerProgress ??= new CustomerCafeProgress();
            customerProgress.Normalize();
            int recipeCount = (coffeeBeanCatalog?.Beans?.Length ?? 0) * OrderRoasts.Length * OrderDrinks.Length;
            if (recipeCount > 0) nextEspressoOrderIndex = customerProgress.TotalServed % recipeCount;
        }

        private void SaveCustomerProgress()
        {
            if (smoke || customerProgress == null) return;
            PlayerPrefs.SetString(CustomerProgressKey, JsonUtility.ToJson(customerProgress));
            PlayerPrefs.Save();
        }

        private void OpenCustomerDialogue(bool daySummary = false)
        {
            if (!daySummary && customerVisit?.Phase == CustomerVisitPhase.Talking &&
                counterCustomer.Phase != CounterCustomerPhase.Ordering) return;
            if (!customerDialogueOpen)
            {
                customerPriorCursorLock = Cursor.lockState;
                customerPriorCursorVisible = Cursor.visible;
            }
            customerDaySummaryOpen = daySummary;
            customerDialogueOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CloseCustomerDialogue()
        {
            if (!customerDialogueOpen) return;
            customerDialogueOpen = customerDaySummaryOpen = false;
            professionalReleaseGuard = true;
            bool restorePointer = Application.isFocused && (!latteActive || lattePointerCaptured);
            Cursor.lockState = restorePointer ? customerPriorCursorLock : CursorLockMode.None;
            Cursor.visible = restorePointer ? customerPriorCursorVisible : true;
        }

        private bool AcceptCustomerOrder()
        {
            if (customerVisit == null || customerVisit.Phase != CustomerVisitPhase.Talking ||
                counterCustomer.Phase != CounterCustomerPhase.Ordering) return false;
            customerVisit.Accept();
            counterCustomer.AcceptOrder();
            CloseCustomerDialogue();
            LeaveCounterView();
            message = CustomerNames[customerVisit.CustomerIndex] + "의 주문을 받았어요. B로 원두와 로스팅을 골라 주세요.";
            return true;
        }

        private bool ClarifyCustomerOrder() => customerVisit != null && customerVisit.Clarify();
        private bool HintCustomerOrder() => customerVisit != null && customerVisit.ShowHint();

        private void UpdateCustomerVisit(float dt)
        {
            if (!professionalMode || paused || (!Application.isFocused && !professionalSmoke)) return;
            customerVisit?.Tick(dt);
        }

        private bool AdvanceCustomerVisit()
        {
            CoffeeBeanDefinition[] beans = coffeeBeanCatalog?.Beans;
            if (!professionalMode || beans == null || beans.Length == 0) return false;
            InitializeCustomerProgress();
            bool resolved = customerVisit?.Phase == CustomerVisitPhase.Reaction;
            if (resolved && counterCustomer.Phase != CounterCustomerPhase.Gone) return false;
            bool completedTrainingShot = customerVisit == null && espressoOrderServed && Brew.Complete;
            if (!StationReadyForSettings() && !resolved && !completedTrainingShot) return false;
            if (customerProgress.DayComplete)
            {
                OpenCustomerDialogue(true);
                return true;
            }

            int recipeCount = beans.Length * OrderRoasts.Length * OrderDrinks.Length;
            for (int offset = 0; offset < recipeCount; offset++)
            {
                int index = (nextEspressoOrderIndex + offset) % recipeCount;
                int beanIndex = index % beans.Length;
                CoffeeBeanDefinition bean = beans[beanIndex];
                if (bean == null || string.IsNullOrEmpty(bean.Id)) continue;
                int recipeIndex = index / beans.Length;
                EspressoRoast roast = OrderRoasts[(recipeIndex + beanIndex) % OrderRoasts.Length];
                CoffeeDrink drink = OrderDrinks[(recipeIndex / OrderRoasts.Length + beanIndex) % OrderDrinks.Length];
                if (espressoOrder.Match(bean.Id, roast, drink) == EspressoOrderMatch.Match) continue;
                CloseCustomerDialogue();
                if (!StationReadyForSettings()) ResetStation();
                nextEspressoOrderIndex = (index + 1) % recipeCount;
                ApplyEspressoOrder(new EspressoCustomerOrder(bean.Id, roast, drink));
                int customerIndex = customerProgress.TotalServed % CustomerNames.Length;
                customerVisit = new CustomerVisitModel(customerIndex, customerProgress.NextVisitId, drink,
                    customerIndex == 0 ? 180f : customerIndex == 4 ? 240f : 210f);
                BeginCounterVisit();
                return true;
            }
            return false;
        }

        private bool StartNextCustomerDay()
        {
            InitializeCustomerProgress();
            if (counterCustomer.Active) return false;
            if (!customerProgress.NextDay()) return false;
            CloseCustomerDialogue();
            ResetStation();
            customerVisit = null;
            SaveCustomerProgress();
            return AdvanceCustomerVisit();
        }

        private void ResolveCustomerVisit(CoffeeDrink drink)
        {
            if (customerVisit == null) return;
            int quality = professionalShot.Evaluate(Brew.InCup, Brew.ElapsedSeconds).Score;
            if (drink == CoffeeDrink.Americano)
            {
                float dilutionError = Mathf.Abs(Americano.WaterToEspressoRatio - AmericanoModel.RecommendedWaterToEspressoRatio);
                quality = Mathf.RoundToInt(quality * Mathf.Clamp01(1f - dilutionError / 6f));
            }
            if (!customerVisit.Resolve(espressoOrderServed, quality)) return;
            customerProgress.TryCredit(customerVisit);
            SaveCustomerProgress();
            if (!counterHandoffInProgress) OpenCustomerDialogue();
        }

        private bool HandleCustomerDialogueInput()
        {
            if (!customerDialogueOpen) return false;
            if (Input.GetKeyDown(KeyCode.Return) && customerVisit?.Phase == CustomerVisitPhase.Talking)
                AcceptCustomerOrder();
            else if (Input.GetKeyDown(KeyCode.Escape) && !customerDaySummaryOpen &&
                customerVisit?.Phase != CustomerVisitPhase.Talking)
                CloseCustomerDialogue();
            return true;
        }

        private bool UpdateCustomerDialogueCursor()
        {
            if (!customerDialogueOpen) return false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            handCursor?.Set(HandPose.Open, Vector2.zero, false);
            supportHand?.Set(HandPose.Open, Vector2.zero, false);
            return true;
        }

        private string SpokenCustomerOrder(bool exact)
        {
            string bean = EspressoOrderBeanName(espressoOrder.BeanId);
            string roast = EspressoBeanProfiles.RoastNameKo(espressoOrder.Roast);
            if (exact) return $"{CoffeeDrinkName(espressoOrder.Drink)} 한 잔이에요. 원두는 {bean}, 로스팅은 {roast}로 부탁해요.";
            string drink = espressoOrder.Drink switch
            {
                CoffeeDrink.Americano => "우유 없이 뜨거운 물을 더해서 길게 마실 한 잔",
                CoffeeDrink.Latte => "따뜻한 우유와 커피를 부드럽게 섞은 한 잔",
                _ => "물이나 우유를 더하지 않은 짧고 진한 한 잔"
            };
            string roastCue = espressoOrder.Roast switch
            {
                EspressoRoast.Light => "가볍게 볶아 산뜻한 쪽",
                EspressoRoast.Dark => "진하게 볶아 고소한 쪽",
                _ => "밝지도 어둡지도 않게 중간으로 볶은 쪽"
            };
            return customerVisit.CustomerIndex switch
            {
                0 => $"곧 출근해야 해서요. {drink} 부탁해요. {bean}를 {roastCue}으로 골라 주세요.",
                1 => $"논문을 좀 읽으려고요. {bean}로 {drink}이면 좋겠어요. {roastCue}이 오늘 기분에 맞겠네요.",
                2 => $"꽃집 문 열기 전에 들렀어요. {bean}, {roastCue}으로요. {drink} 부탁할게요.",
                3 => $"오늘 찍은 사진을 보면서 마시려고요. {drink}, 원두는 {bean}. {roastCue}으로 해 주세요.",
                4 => $"다음 문장이 잘 안 떠올라서요. {bean}를 {roastCue}으로 골라 {drink} 만들어 주실래요?",
                _ => $"오늘도 커피 한 잔 하러 왔어요. {bean}로 {drink}. {roastCue}으로 부탁해요."
            };
        }

        private string CustomerReactionText()
        {
            string[] thanks =
            {
                "딱 원하던 한 잔이에요. 출근길이 한결 좋아졌네요!",
                "향도 좋고 균형도 맞아요. 오늘 공부는 잘되겠어요.",
                "기분 좋은 향이네요. 꽃집 손님들에게도 이야기해야겠어요.",
                "오늘 사진처럼 분위기 좋은 한 잔이에요. 고마워요!",
                "이 맛이면 다음 문장이 떠오를 것 같아요.",
                "역시 여기 오길 잘했네요. 또 들를게요!"
            };
            if (!customerVisit.OrderMatched)
                return "제가 부탁한 것과 다른 부분이 있어요. " + EspressoOrderMismatches(espressoOrderMatch) + "을 확인해 주세요.";
            if (customerVisit.Reaction == CustomerVisitReaction.Positive)
                return thanks[customerVisit.CustomerIndex];
            if (customerVisit.QualityScore < 60)
                return "주문은 맞아요. 다음에는 추출 균형을 조금 더 맞춰 주시면 좋겠어요.";
            if (customerVisit.Patience01 < .35f)
                return "원하던 커피네요. 조금 오래 기다려서 다음에는 더 일찍 받을 수 있으면 좋겠어요.";
            return "주문한 커피 잘 받았어요. 다음 한 잔도 기대할게요.";
        }

        private bool DrawCustomerConversation()
        {
            if (!customerDialogueOpen) return false;
            if (counterView && !customerDaySummaryOpen)
            {
                DrawCounterScreen(true);
                return true;
            }
            GUI.DrawTexture(frame, art.Pixels, ScaleMode.StretchToFill, false);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                Vector3.one * frame.width / 960);
            try
            {
                BeanPanel(new Rect(0, 0, 960, 540), new Color(.025f, .035f, .05f, .82f));
                BeanPanel(new Rect(96, 43, 768, 454), new Color(.10f, .12f, .16f));
                if (customerDaySummaryOpen) DrawCustomerDaySummary();
                else DrawCustomerVisitDialogue();
            }
            finally { GUI.matrix = previous; }
            return true;
        }

        private void DrawCustomerVisitDialogue()
        {
            int index = customerVisit.CustomerIndex;
            bool reaction = customerVisit.Phase == CustomerVisitPhase.Reaction;
            int face = reaction ? (customerVisit.Reaction == CustomerVisitReaction.Positive ? 1 :
                customerVisit.Reaction == CustomerVisitReaction.Negative ? -1 : 0) : 0;
            DrawCafeCustomerPortrait(new Rect(119, 100, 185, 254), index, face);
            GUI.Label(new Rect(120, 63, 700, 32), $"영업 {customerProgress.Day}일차 · {CustomerNames[index]}",
                BeanText(23, EspressoInk, true));
            int previousVisits = customerProgress.CustomerVisits[index] - (customerVisit.Credited ? 1 : 0);
            string introduction = CustomerRoles[index] + (previousVisits > 0 ? $" · {previousVisits + 1}번째 방문" : " · 첫 방문");
            GUI.Label(new Rect(332, 111, 497, 25), introduction, BeanText(14, EspressoAccent, true));
            BeanPanel(new Rect(326, 147, 513, 194), new Color(.18f, .20f, .24f));
            string speech = reaction ? CustomerReactionText() : SpokenCustomerOrder(customerVisit.Clarified);
            GUI.Label(new Rect(342, 160, 482, 167), speech, BeanText(18, EspressoInk, false, true));

            if (reaction)
            {
                GUI.Label(new Rect(327, 352, 512, 30),
                    $"만족도 {customerVisit.SatisfactionScore}점 · 음료값 {customerVisit.BasePrice} · 팁 {customerVisit.Tip} · 받은 돈 {customerVisit.Payment}코인",
                    BeanText(15, EspressoAccent, true));
                GUI.Label(new Rect(327, 385, 512, 24),
                    $"보유 {customerProgress.Coins}코인 · 오늘 응대 {customerProgress.CustomersToday}/5명",
                    BeanText(14, EspressoMuted));
                if (GUI.Button(new Rect(119, 440, 177, 35), "음료 리포트 보기", BeanActionStyle()))
                    CloseCustomerDialogue();
                bool reactionButtonsEnabled = GUI.enabled;
                GUI.enabled = reactionButtonsEnabled && counterCustomer.Phase == CounterCustomerPhase.Gone;
                if (GUI.Button(new Rect(637, 440, 202, 35),
                    customerProgress.DayComplete ? "오늘 영업 정산" : "다음 손님", BeanActionStyle()))
                    AdvanceCustomerVisit();
                GUI.enabled = reactionButtonsEnabled;
                return;
            }

            if (customerVisit.Hinted)
            {
                EspressoBeanProfile recipe = EspressoBeanProfiles.ForBean(espressoOrder.BeanId, espressoOrder.Roast);
                GUI.Label(new Rect(326, 350, 512, 65),
                    $"레시피 힌트 · {CoffeeDrinkName(espressoOrder.Drink)} / {EspressoBeanProfiles.RoastNameKo(espressoOrder.Roast)}\n"
                    + $"분쇄 {recipe.GrindSetting}/9 · {recipe.TemperatureCelsius:0.0}°C · 샷 비율 1:{recipe.BrewRatio:0.00}",
                    BeanText(14, EspressoAccent, true, true));
            }
            else
                GUI.Label(new Rect(326, 351, 512, 52), "주문을 받으면 기다리는 시간이 흐릅니다. 헷갈리는 부분은 물어보세요.",
                    BeanText(14, EspressoMuted, false, true));

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && customerVisit.Phase == CustomerVisitPhase.Talking && !customerVisit.Clarified;
            if (GUI.Button(new Rect(119, 440, 161, 35), "자세히 물어보기", BeanActionStyle()))
                ClarifyCustomerOrder();
            GUI.enabled = previousEnabled && customerVisit.Phase == CustomerVisitPhase.Talking && !customerVisit.Hinted;
            if (GUI.Button(new Rect(290, 440, 132, 35), "레시피 힌트", BeanActionStyle()))
                HintCustomerOrder();
            GUI.enabled = previousEnabled;
            if (StationReadyForSettings() && GUI.Button(new Rect(432, 440, 144, 35), "연습 주문 지정", BeanActionStyle()))
            {
                CloseCustomerDialogue();
                OpenEspressoOrderEditor();
            }
            if (GUI.Button(new Rect(637, 440, 202, 35),
                customerVisit.Phase == CustomerVisitPhase.Talking ? "알겠습니다 [Enter]" : "작업대로 돌아가기",
                BeanActionStyle()))
            {
                if (customerVisit.Phase == CustomerVisitPhase.Talking) AcceptCustomerOrder();
                else CloseCustomerDialogue();
            }
        }

        private void DrawCustomerDaySummary()
        {
            GUI.Label(new Rect(129, 83, 696, 38), $"영업 {customerProgress.Day}일차 정산", BeanText(28, EspressoInk, true));
            GUI.Label(new Rect(130, 154, 690, 45), $"오늘 받은 돈  {customerProgress.EarnedToday}코인", BeanText(25, EspressoAccent, true));
            GUI.Label(new Rect(130, 220, 690, 35),
                $"손님 {customerProgress.CustomersToday}명 · 주문 일치 {customerProgress.CorrectToday}명",
                BeanText(20, EspressoInk));
            GUI.Label(new Rect(130, 269, 690, 35),
                $"보유 {customerProgress.Coins}코인 · 누적 응대 {customerProgress.TotalServed}명", BeanText(20, EspressoInk));
            GUI.Label(new Rect(130, 335, 690, 45), "같은 손님이 다시 방문하면 방문 횟수를 기억합니다.",
                BeanText(15, EspressoMuted, false, true));
            if (GUI.Button(new Rect(601, 431, 224, 43), "다음 영업일 열기", BeanActionStyle()))
                StartNextCustomerDay();
        }

        private void DrawCustomerVisitTicket()
        {
            BeanPanel(new Rect(649, 52, 293, 164), new Color(.07f, .09f, .14f, .95f));
            GUI.Label(new Rect(661, 57, 148, 24), $"영업 {customerProgress.Day}일 · {customerProgress.Coins}코인",
                BeanText(13, EspressoAccent, true));
            GUI.Button(EspressoOrderButton, "손님 대화 [O]", BeanActionStyle());
            int expression = customerVisit.Phase == CustomerVisitPhase.Reaction
                ? (customerVisit.Reaction == CustomerVisitReaction.Positive ? 1 :
                    customerVisit.Reaction == CustomerVisitReaction.Negative ? -1 : 0)
                : customerVisit.Patience01 < .3f ? -1 : 0;
            DrawCafeCustomerPortrait(new Rect(661, 85, 66, 87), customerVisit.CustomerIndex, expression);
            GUI.Label(new Rect(738, 86, 190, 23), CustomerNames[customerVisit.CustomerIndex],
                BeanText(15, EspressoInk, true));
            GUI.Label(new Rect(738, 111, 190, 54),
                customerVisit.Clarified ? $"{CoffeeDrinkName(espressoOrder.Drink)} · {EspressoBeanProfiles.RoastNameKo(espressoOrder.Roast)}\n"
                    + EspressoOrderBeanName(espressoOrder.BeanId) : "대화 속 주문을 확인해 주세요.",
                BeanText(12, EspressoMuted, false, true));
            BeanPanel(new Rect(661, 181, 267, 8), new Color(.23f, .25f, .28f));
            BeanPanel(new Rect(661, 181, 267 * customerVisit.Patience01, 8), new Color(.50f, .75f, .56f));
            GUI.Label(new Rect(661, 194, 267, 20),
                customerVisit.Phase == CustomerVisitPhase.Reaction ? "응대 완료 · O로 손님 반응 보기" :
                    $"기다림 {customerVisit.Patience01:P0} · 오늘 {customerProgress.CustomersToday}/5명",
                BeanText(12, EspressoMuted));
        }
    }
}
