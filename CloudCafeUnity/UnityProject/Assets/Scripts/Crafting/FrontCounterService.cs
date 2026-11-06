using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private readonly CounterCustomerModel counterCustomer = new CounterCustomerModel();
        private bool counterView, carryingFinishedDrink, servingCupHeld, counterHandoffInProgress;
        private CoffeeDrink counterOfferedDrink;
        private Vector3 counterCameraReturnPosition, servingCupOffset;
        private Quaternion counterCameraReturnRotation;
        private float counterCameraReturnSize;

        private void EnterCounterView()
        {
            if (!counterView)
            {
                counterCameraReturnPosition = art.Camera.transform.position;
                counterCameraReturnRotation = art.Camera.transform.rotation;
                counterCameraReturnSize = art.Camera.orthographicSize;
            }
            counterView = true;
            if (latteActive) ReleaseLattePointer();
            art.SetCounterVisible(true);
            art.Camera.transform.position = art.CounterCameraPosition;
            art.Camera.transform.LookAt(art.CounterCameraTarget);
            art.Camera.orthographicSize = art.CounterCameraSize;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            handCursor?.Set(HandPose.Open, Vector2.zero, false);
            supportHand?.Set(HandPose.Open, Vector2.zero, false);
        }

        private void LeaveCounterView()
        {
            if (!counterView) return;
            servingCupHeld = false;
            if (carryingFinishedDrink) art.CounterCup.position = art.CounterCupHome;
            counterView = false;
            art.Camera.transform.SetPositionAndRotation(counterCameraReturnPosition, counterCameraReturnRotation);
            art.Camera.orthographicSize = counterCameraReturnSize;
        }

        private void BeginCounterVisit()
        {
            counterCustomer.Reset();
            counterCustomer.Begin(customerVisit.CustomerIndex);
            carryingFinishedDrink = servingCupHeld = false;
            art.SetCounterVisible(true);
            art.ShowCounterDrink(false);
            EnterCounterView();
            RefreshCounterArt();
        }

        private void ResetCounterVisit()
        {
            LeaveCounterView();
            counterCustomer.Reset();
            carryingFinishedDrink = servingCupHeld = false;
            art.SetCounterVisible(false);
        }

        private void ResetCounterShot()
        {
            LeaveCounterView();
            carryingFinishedDrink = servingCupHeld = false;
            art.ShowCounterDrink(false);
        }

        private void UpdateCounterService(float dt)
        {
            if (!professionalMode || paused || (!Application.isFocused && !professionalSmoke)) return;
            CounterCustomerPhase before = counterCustomer.Phase;
            counterCustomer.Step(dt);
            RefreshCounterArt();
            if (before == CounterCustomerPhase.Arriving && counterCustomer.Phase == CounterCustomerPhase.Ordering)
                OpenCustomerDialogue();
        }

        private void RefreshCounterArt()
        {
            if (customerVisit == null) return;
            art.SetCounterCustomerVisible(counterCustomer.Active);
            art.UpdateCounterCustomer(customerVisit.CustomerIndex, counterCustomer.CounterProgress,
                CounterExpression(), counterCustomer.HoldingDrink,
                counterCustomer.HoldingDrink ? counterOfferedDrink : finishedDrink ?? espressoOrder.Drink);
        }

        private int CounterExpression()
        {
            if (customerVisit == null) return 0;
            if (customerVisit.Phase == CustomerVisitPhase.Reaction)
                return customerVisit.Reaction == CustomerVisitReaction.Positive ? 1 :
                    customerVisit.Reaction == CustomerVisitReaction.Negative ? -1 : 0;
            return customerVisit.Phase == CustomerVisitPhase.Preparing && customerVisit.Patience01 < .35f ? -1 : 0;
        }

        private string CounterSpeech()
        {
            if (customerVisit == null) return string.Empty;
            switch (counterCustomer.Phase)
            {
                case CounterCustomerPhase.Arriving:
                    return customerProgress.CustomerVisits[customerVisit.CustomerIndex] > 0 ? "안녕하세요, 또 들렀어요!" : "안녕하세요! 커피 한 잔 주문할게요.";
                case CounterCustomerPhase.Ordering:
                    return SpokenCustomerOrder(customerVisit.Clarified);
                case CounterCustomerPhase.Waiting:
                    if (carryingFinishedDrink) return "제 커피인가요? 이쪽으로 건네 주세요.";
                    return customerVisit.Patience01 < .35f ? "조금 오래 기다렸네요. 제 커피는 언제 나올까요?" :
                        customerVisit.Patience01 < .65f ? "커피가 잘 만들어지고 있나요? 기다리고 있을게요." :
                        "좋은 향이 나네요. 맛있게 부탁해요!";
                case CounterCustomerPhase.Receiving:
                    return CustomerReactionText();
                case CounterCustomerPhase.Leaving:
                    return customerVisit.Reaction == CustomerVisitReaction.Positive ? "잘 마실게요. 다음에 또 올게요!" :
                        customerVisit.OrderMatched ? "커피 잘 받았어요. 안녕히 계세요." :
                        "다음에는 부탁드린 대로 만들어 주세요.";
                default:
                    return "손님이 음료를 받아 나갔어요.";
            }
        }

        private bool BeginCounterHandoff()
        {
            if (!professionalMode || customerVisit?.Phase != CustomerVisitPhase.Preparing ||
                counterCustomer.Phase != CounterCustomerPhase.Waiting || !finishedDrink.HasValue ||
                !IsDrinkReadyToServe(finishedDrink.Value) || espressoOrderSubmitted || paused) return false;
            if (professionalReportOpen || professionalMenuOpen) CloseProfessionalOverlay();
            CloseCustomerDialogue();
            ReleaseLattePointer();
            EnterCounterView();
            carryingFinishedDrink = true;
            servingCupHeld = false;
            counterOfferedDrink = finishedDrink.Value;
            art.SetCounterDrink(finishedDrink.Value);
            art.CounterCup.position = art.CounterCupHome;
            art.ShowCounterDrink(true);
            RefreshCounterArt();
            return true;
        }

        private bool GrabServingCup()
        {
            if (!counterView || paused || servingCupHeld || !carryingFinishedDrink ||
                counterCustomer.Phase != CounterCustomerPhase.Waiting) return false;
            servingCupHeld = true;
            servingCupOffset = art.CounterCup.position - CounterPointerPoint();
            servingCupOffset.y = 0;
            return true;
        }

        private Vector3 CounterPointerPoint()
        {
            float distance;
            var plane = new Plane(Vector3.up, art.CounterReceivePoint + Vector3.up * .12f);
            return plane.Raycast(MouseRay(), out distance) ? MouseRay().GetPoint(distance) : art.CounterCupHome;
        }

        private void MoveServingCup(Vector3 point)
        {
            if (!servingCupHeld || paused) return;
            Vector3 home = art.CounterCupHome;
            art.CounterCup.position = new Vector3(Mathf.Clamp(point.x, home.x - .9f, home.x + .9f),
                art.CounterReceivePoint.y + .12f,
                Mathf.Clamp(point.z, home.z - .35f, art.CounterReceivePoint.z + .25f));
        }

        private bool ServingCupNearCustomer()
        {
            Vector3 cup = art.CounterCup.position, target = art.CounterReceivePoint;
            return new Vector2(cup.x - target.x, cup.z - target.z).sqrMagnitude <= .32f * .32f;
        }

        private bool DropServingCup()
        {
            if (!servingCupHeld || paused) return false;
            servingCupHeld = false;
            if (!ServingCupNearCustomer() || counterCustomer.Phase != CounterCustomerPhase.Waiting)
            {
                art.CounterCup.position = art.CounterCupHome;
                return false;
            }
            counterHandoffInProgress = true;
            try { TryServeEspressoOrder(finishedDrink.Value); }
            finally { counterHandoffInProgress = false; }
            if (!espressoOrderSubmitted || customerVisit.Phase != CustomerVisitPhase.Reaction)
            {
                art.CounterCup.position = art.CounterCupHome;
                return false;
            }
            counterCustomer.TryHandOver();
            carryingFinishedDrink = false;
            art.ShowCounterDrink(false);
            RefreshCounterArt();
            return true;
        }

        private bool HandleCounterShortcut()
        {
            if (!professionalMode || customerVisit == null || smoke || customerDialogueOpen ||
                professionalReportOpen || professionalMenuOpen || CoffeeBeanSelectionOpen || espressoOrderMenuOpen)
                return false;
            if (!Input.GetKeyDown(KeyCode.C)) return false;
            if (counterView && (counterCustomer.Phase == CounterCustomerPhase.Arriving ||
                counterCustomer.Phase == CounterCustomerPhase.Ordering)) return true;
            if (counterView) LeaveCounterView();
            else if (!BeginCounterHandoff()) EnterCounterView();
            return true;
        }

        private void UpdateCounterViewInput()
        {
            if (smoke) return;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (paused) return;
            if (Input.GetMouseButtonDown(0) && StationGuiPointer().y < 452 && !servingCupHeld)
            {
                RaycastHit hit;
                if (Physics.Raycast(MouseRay(), out hit, 20) && hit.transform.IsChildOf(art.CounterCup))
                    GrabServingCup();
            }
            if (servingCupHeld)
            {
                MoveServingCup(CounterPointerPoint() + servingCupOffset);
                if (!Input.GetMouseButton(0)) DropServingCup();
            }
        }

        private void UpdateCounterCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !servingCupHeld;
            Vector3 anchor = art.Camera.WorldToViewportPoint(art.CounterCup.position);
            handCursor?.Set(servingCupHeld ? HandPose.Grip : HandPose.Open,
                new Vector2(anchor.x, anchor.y), servingCupHeld);
            supportHand?.Set(HandPose.Open, Vector2.zero, false);
        }

        private void DrawCounterGUI() => DrawCounterScreen(false);

        private void DrawCounterScreen(bool dialogue)
        {
            GUI.DrawTexture(frame, art.Pixels, ScaleMode.StretchToFill, false);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                Vector3.one * frame.width / 960);
            try
            {
                BeanPanel(new Rect(0, 0, 960, 42), new Color(.07f, .09f, .14f, .96f));
                GUI.Label(new Rect(20, 8, 710, 30),
                    $"카운터 · {CustomerNames[customerVisit.CustomerIndex]} · 영업 {customerProgress.Day}일차",
                    BeanText(20, EspressoInk, true));
                GUI.Label(new Rect(744, 11, 195, 25), $"{customerProgress.Coins}코인", BeanText(15, EspressoAccent, true));
                DrawCounterSpeechBubble();
                if (dialogue) DrawCounterOrderControls();
                else DrawCounterActionControls();
            }
            finally { GUI.matrix = previous; }
        }

        private void DrawCounterSpeechBubble()
        {
            if (!counterCustomer.Active) return;
            Vector3 anchor = art.Camera.WorldToViewportPoint(art.CounterSpeechAnchor.position);
            float height = counterCustomer.Phase == CounterCustomerPhase.Ordering ? 200 : 110;
            float x = Mathf.Clamp(anchor.x * 960 - 465, 20, 540);
            float y = Mathf.Clamp((1 - anchor.y) * 540 - 30, 58, 330 - height);
            Color paper = new Color(.98f, .95f, .88f, .98f);
            BeanPanel(new Rect(x, y, 390, height), paper);
            BeanPanel(new Rect(x + 390, y + height - 32, 16, 14), paper);
            GUI.Label(new Rect(x + 15, y + 11, 360, height - 20), CounterSpeech(),
                BeanText(17, new Color(.19f, .17f, .15f), false, true));
        }

        private void DrawCounterOrderControls()
        {
            BeanPanel(new Rect(0, 367, 960, 173), new Color(.07f, .09f, .14f, .97f));
            string role = CustomerRoles[customerVisit.CustomerIndex];
            if (customerVisit.Hinted)
            {
                var recipe = CloudCafe.Coffee.EspressoBeanProfiles.ForBean(espressoOrder.BeanId, espressoOrder.Roast);
                role += $" · 추천 {recipe.GrindSetting}/9 · {recipe.TemperatureCelsius:0.0}°C · 샷 비율 1:{recipe.BrewRatio:0.00}";
            }
            GUI.Label(new Rect(24, 382, 912, 33), role, BeanText(16, EspressoAccent, true));
            GUI.Label(new Rect(24, 421, 912, 30), "주문을 받은 뒤 제작을 시작합니다. 모호한 주문은 먼저 물어보세요.",
                BeanText(14, EspressoMuted));
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && customerVisit.Phase == CustomerVisitPhase.Talking && !customerVisit.Clarified;
            if (GUI.Button(new Rect(24, 480, 165, 44), "자세히 물어보기", BeanActionStyle())) ClarifyCustomerOrder();
            GUI.enabled = wasEnabled && customerVisit.Phase == CustomerVisitPhase.Talking && !customerVisit.Hinted;
            if (GUI.Button(new Rect(201, 480, 139, 44), "레시피 힌트", BeanActionStyle())) HintCustomerOrder();
            GUI.enabled = wasEnabled;
            if (StationReadyForSettings() &&
                GUI.Button(new Rect(352, 480, 161, 44), "연습 주문 지정", BeanActionStyle()))
            {
                CloseCustomerDialogue();
                OpenEspressoOrderEditor();
            }
            if (GUI.Button(new Rect(709, 480, 227, 44),
                customerVisit.Phase == CustomerVisitPhase.Talking ? "알겠습니다 [Enter]" : "작업대로 돌아가기", BeanActionStyle()))
            {
                if (customerVisit.Phase == CustomerVisitPhase.Talking) AcceptCustomerOrder();
                else { CloseCustomerDialogue(); LeaveCounterView(); }
            }
        }

        private void DrawCounterActionControls()
        {
            BeanPanel(new Rect(0, 452, 960, 88), new Color(.07f, .09f, .14f, .97f));
            string hint = counterCustomer.Phase switch
            {
                CounterCustomerPhase.Arriving => "손님이 카운터로 걸어오고 있어요.",
                CounterCustomerPhase.Waiting => carryingFinishedDrink
                    ? "완성 컵을 잡아 손님 앞의 전달 구역으로 끌어 놓으세요."
                    : $"손님이 기다리고 있어요 · 남은 시간 {CustomerTimeRemaining()}",
                CounterCustomerPhase.Receiving => $"음료를 건넸어요 · 받은 돈 {customerVisit.Payment}코인 · 팁 {customerVisit.Tip}코인",
                CounterCustomerPhase.Leaving => "손님이 음료를 들고 나가고 있어요.",
                _ => "손님이 나갔어요. 다음 손님을 맞이하세요."
            };
            GUI.Label(new Rect(22, 459, 912, 27), paused ? "잠시 쉬는 중 · Esc로 계속" : hint,
                BeanText(16, EspressoInk));
            if (counterCustomer.Phase == CounterCustomerPhase.Waiting &&
                GUI.Button(new Rect(22, 490, 174, 40), "작업대로 [C]", BeanActionStyle()))
                LeaveCounterView();
            if (counterCustomer.Phase == CounterCustomerPhase.Waiting && finishedDrink.HasValue &&
                !carryingFinishedDrink &&
                GUI.Button(new Rect(213, 490, 184, 40), "완성 컵 가져오기", BeanActionStyle()))
                BeginCounterHandoff();
            if (counterCustomer.Phase == CounterCustomerPhase.Gone)
            {
                if (GUI.Button(new Rect(22, 490, 174, 40), "음료 리포트", BeanActionStyle()))
                {
                    LeaveCounterView();
                    TryOpenDrinkReport();
                }
                if (GUI.Button(new Rect(705, 490, 231, 40),
                    customerProgress.DayComplete ? "오늘 영업 정산" : "다음 손님", BeanActionStyle()))
                    AdvanceCustomerVisit();
            }
        }
    }
}
