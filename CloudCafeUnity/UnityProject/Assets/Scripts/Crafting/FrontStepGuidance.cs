using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Color GuideGold = new Color(1f, .79f, .32f);
        private static readonly Color GuideCyan = new Color(.31f, 1f, .89f);
        private static readonly string[] GuideStages =
            { "주문", "분쇄", "표면 정리", "탬핑", "머신 장착", "컵 배치", "추출", "음료 완성", "음료 전달" };
        private StepTargetGlow stepGlow;
        private bool stepGuidanceSmoke;

        private readonly struct StepHint
        {
            public readonly string Id, Instruction, TargetLabel, PlaceLabel;
            public readonly int Stage;
            public readonly Transform Target;
            public readonly Vector3? Place;
            public readonly Rect? Button;
            public readonly float Radius;

            public StepHint(string id, int stage, string instruction, Transform target = null,
                Vector3? place = null, string targetLabel = null, string placeLabel = null,
                Rect? button = null, float radius = .29f)
            {
                Id = id; Stage = stage; Instruction = instruction; Target = target;
                Place = place; TargetLabel = targetLabel; PlaceLabel = placeLabel;
                Button = button; Radius = radius;
            }
        }

        private bool StepGlowAllowed => !paused && !Moving && !customerDialogueOpen && !professionalMenuOpen &&
            !professionalReportOpen && !espressoOrderMenuOpen && !CoffeeBeanSelectionOpen;

        private StepHint CurrentStepHint()
        {
            return TutorialActive ? TutorialStepHint() : DefaultStepHint();
        }

        private StepHint DefaultStepHint()
        {
            if (customerDaySummaryOpen)
                return new StepHint("serve.next", 8, "오늘 영업을 마쳤어요. 다음 영업일을 여세요.",
                    button: new Rect(601, 431, 224, 43));
            if (customerVisit?.Phase == CustomerVisitPhase.Reaction)
            {
                if (counterCustomer.Phase != CounterCustomerPhase.Gone)
                    return new StepHint("serve.wait", 8, "손님이 음료를 받고 나가는 중이에요.");
                return new StepHint("serve.next", 8, counterView || professionalReportOpen
                        ? "전달 완료! 다음 손님 버튼을 누르세요." : "C로 카운터에 가서 다음 손님을 맞이하세요.",
                    button: counterView ? new Rect(705, 490, 231, 40) :
                        professionalReportOpen ? new Rect(273, 475, 254, 40) :
                            Brew.Complete ? EspressoActionButton : (Rect?)null);
            }
            if (counterView)
            {
                if (counterCustomer.Phase == CounterCustomerPhase.Arriving)
                    return new StepHint("order.wait", 0, "손님이 카운터에 도착할 때까지 기다려요.");
                if (counterCustomer.Phase == CounterCustomerPhase.Ordering &&
                    customerVisit?.Phase == CustomerVisitPhase.Talking)
                    return new StepHint("order.accept", 0, "주문을 읽고 알겠습니다 버튼을 누르세요.",
                        button: new Rect(709, 480, 227, 44));
                if (customerDialogueOpen && counterCustomer.Phase == CounterCustomerPhase.Waiting)
                    return new StepHint("counter.return", 1, "주문을 확인했으면 작업대로 돌아가세요.",
                        button: new Rect(709, 480, 227, 44));
                if (carryingFinishedDrink)
                {
                    if (servingCupHeld)
                        return new StepHint(ServingCupNearCustomer() ? "serve.release" : "serve.place", 8,
                            ServingCupNearCustomer() ? "위치가 맞았어요. 손을 놓아 건네세요." :
                                "컵을 청록색 원 안으로 옮겨 놓으세요.",
                            place: art.CounterReceivePoint + Vector3.up * .015f, placeLabel: "놓기 · 손님 앞",
                            radius: .32f);
                    return new StepHint("serve.pick", 8, "금색으로 빛나는 완성 컵을 잡으세요.",
                        art.CounterCup, art.CounterReceivePoint + Vector3.up * .015f,
                        "잡기 · 완성 컵", "놓기 · 손님 앞", radius: .32f);
                }
                if (finishedDrink.HasValue)
                    return new StepHint("serve.report", 8, "완성 컵 가져오기 버튼을 누르세요.",
                        button: new Rect(213, 490, 184, 40));
                return new StepHint("counter.return", 1, "작업대로 돌아가 커피를 만드세요.",
                    button: new Rect(22, 490, 174, 40));
            }
            if (finishedDrink.HasValue)
                return new StepHint("serve.report", 8,
                    professionalMode && customerVisit?.Phase == CustomerVisitPhase.Preparing
                        ? professionalReportOpen ? "카운터로 가져가기 버튼을 누르세요." : "리포트를 열어 카운터로 가져가세요."
                        : "음료 완성! 리포트에서 결과를 확인하세요.",
                    professionalReportOpen || latteActive || AmericanoView ? null : art.Cup,
                    targetLabel: "누르기 · 완성 음료",
                    button: professionalReportOpen
                        ? professionalMode && customerVisit?.Phase == CustomerVisitPhase.Preparing
                            ? new Rect(273, 475, 254, 40) : (Rect?)null
                        : latteActive ? new Rect(758, 485, 180, 34) : AmericanoView ? FinishWaterButton : EspressoActionButton);

            if (AmericanoView)
            {
                bool enough = Americano.WaterGrams >= Americano.TargetWaterGrams;
                return new StepHint(enough ? "drink.water.finish" : "drink.water", 7,
                    enough ? "추천 온수량이에요. 붓기를 놓고 완성 버튼을 누르세요." :
                        "온수 붓기 버튼 또는 Space를 누르고 있으세요.",
                    place: art.AmericanoGuidePoint, placeLabel: "컵 · 온수 붓기",
                    button: enough ? FinishWaterButton : WaterButton, radius: .25f);
            }
            if (latteActive)
                return new StepHint("drink.milk", 7,
                    !smoke && !lattePointerCaptured ? "화면을 클릭한 뒤 Space를 누르면 우유를 부어요." :
                        "마우스로 붓는 곳을 맞추고 Space로 우유를 부어요.",
                    art.LattePitcher, art.Cup.position + Vector3.up * .24f,
                    "조작 · 우유 피처", "컵 · 우유 붓기", radius: .19f);

            if (Brew.Complete)
            {
                float y = professionalMode ? 409 : 231;
                Rect choices = new Rect(134, y, 692, 36);
                string instruction = "음료 선택을 열어 완성할 음료를 고르세요.";
                if (professionalReportOpen)
                {
                    instruction = "완성할 음료 버튼을 선택하세요.";
                    if (espressoShotOrder.HasRequest)
                    {
                        CoffeeDrink requested = espressoShotOrder.Drink;
                        choices = new Rect(requested == CoffeeDrink.Espresso ? 134 :
                            requested == CoffeeDrink.Americano ? 367 : 600, y, 226, 36);
                        instruction = CoffeeDrinkName(requested) + " 버튼을 선택하세요.";
                    }
                }
                return new StepHint("drink.choose", 7, instruction,
                    professionalReportOpen ? null : art.Cup, targetLabel: "누르기 · 음료 선택",
                    button: professionalReportOpen ? choices : EspressoActionButton);
            }
            if (Brew.Inserted)
            {
                if (!Brew.Locked)
                    return new StepHint("machine.lock", 4, "빛나는 손잡이를 누른 채 오른쪽으로 돌리세요.",
                        art.Tool, targetLabel: "돌리기 · 손잡이");
                if (!Brew.CupDocked)
                    return new StepHint(cupHeld ? "cup.place" : "cup.pick", 5,
                        cupHeld ? "컵을 청록색 받침에 옮겨 놓으세요." : "오른쪽의 빛나는 컵을 잡으세요.",
                        cupHeld ? null : art.Cup, art.CupSeat + Vector3.up * .025f,
                        "잡기 · 컵", "놓기 · 추출구 아래", radius: .3f);
                if (Brew.Running)
                {
                    if (!professionalMode)
                        return new StepHint("brew.wait", 6, "추출 중이에요. 36g이 되면 자동으로 멈춰요.");
                    float target = Model.Grounds * professionalBeanRecipe.BrewRatio;
                    return new StepHint("brew.stop", 6,
                        Brew.InCup >= target ? "추천 추출량이에요! 금색 버튼으로 멈추세요." :
                            $"저울을 보세요. 추천 {target:0.0}g에서 금색 버튼으로 멈춰요.",
                        art.BrewButton, targetLabel: "누르기 · 추출 정지", button: EspressoActionButton);
                }
                if (Brew.ShotFinalized || Brew.InFlight > .001f)
                    return new StepHint("brew.wait", 6, "마지막 커피가 컵에 떨어질 때까지 기다려요.");
                return new StepHint("brew.start", 6, "금색 버튼을 눌러 추출을 시작하세요.",
                    art.BrewButton, targetLabel: "누르기 · 추출 시작", button: EspressoActionButton);
            }
            if (Prep.Complete)
                return new StepHint(held ? "machine.place" : "machine.pick", 4,
                    held ? "오른쪽 머신의 청록색 원에 가져다 놓으세요." : "준비된 포터필터의 손잡이를 잡으세요.",
                    held ? null : art.Tool, art.GroupSeat + Vector3.up * .16f,
                    "잡기 · 포터필터", "끼우기 · 머신 헤드");

            if (held || Model.Slot == CraftSlot.Held)
                return new StepHint(Model.Ready ? "grinder.return.place" : "grinder.place", 1,
                    Model.Ready ? "청록색 작업 매트에 내려놓으세요." : "왼쪽 그라인더의 청록색 받침에 놓으세요.",
                    place: Model.Ready ? art.Mat + Vector3.up * .055f : art.Dock + Vector3.up * .025f,
                    placeLabel: Model.Ready ? "놓기 · 작업 매트" : "놓기 · 분쇄 받침");
            if (Model.Slot == CraftSlot.Grinder)
            {
                if (Model.Running)
                    return new StepHint(professionalMode ? "grinder.stop" : "grinder.wait", 1,
                        professionalMode ? "18g 근처에서 금색 버튼을 다시 눌러 멈추세요." : "18g까지 분쇄해요. 자동으로 멈출 때까지 기다려요.",
                        professionalMode ? art.Switch : null, targetLabel: "누르기 · 분쇄 정지",
                        button: professionalMode ? EspressoActionButton : (Rect?)null);
                if (Model.Flying > .001f)
                    return new StepHint("grinder.wait", 1, "마지막 가루가 다 떨어질 때까지 기다려요.");
                if (Model.Ready)
                    return new StepHint("grinder.return.pick", 1, "손잡이를 잡아 청록색 매트로 옮기세요.",
                        art.Tool, art.Mat + Vector3.up * .055f, "잡기 · 포터필터", "놓기 · 작업 매트");
                return new StepHint("grinder.start", 1, "금색 버튼을 눌러 원두를 분쇄하세요.",
                    art.Switch, targetLabel: "누르기 · 분쇄 시작", button: EspressoActionButton);
            }
            if (!Model.Ready)
                return new StepHint("grinder.pick", 1, "금색 손잡이를 잡고 왼쪽 분쇄 받침으로 옮기세요.",
                    art.Tool, art.Dock + Vector3.up * .025f, "잡기 · 포터필터", "놓기 · 분쇄 받침");
            if (professionalMode && !Model.DoseCommitted)
                return new StepHint("dose.confirm", 1, "도징 확정 버튼 또는 Enter를 누르세요.", button: EspressoActionButton);

            if (prepKind == 1 && prepContact && Prep.Leveled)
                return new StepHint("level.release", 2, "표면 정리 완료! 손을 놓으세요.",
                    art.Distributor, targetLabel: "손 놓기 · 표면 정리 도구");
            if (!Prep.Leveled)
                return new StepHint(prepKind == 1 ? prepHeld ? "level.place" : "level.rotate" : "level.pick", 2,
                    prepKind != 1 ? "낮고 둥근 표면 정리 도구를 잡으세요." : prepHeld ?
                        "청록색 바스켓 위에 도구를 놓으세요." : "도구를 누른 채 원을 그리듯 돌리세요.",
                    prepHeld ? null : art.Distributor,
                    prepKind == 1 && prepContact ? (Vector3?)null : art.Tool.position + Vector3.up * .17f,
                    prepKind == 1 ? "돌리기 · 표면 정리 도구" : "잡기 · 표면 정리 도구", "놓기 · 바스켓", radius: .26f);

            bool release = pressing && Prep.TampHoldProgress >= 1f;
            return new StepHint(release ? "tamp.release" : prepKind == 2 ? prepHeld ? "tamp.place" : "tamp.press" : "tamp.pick", 3,
                release ? "충분히 눌렀어요! 손을 놓아 탬핑을 마치세요." : prepKind != 2 ? "손잡이가 높은 탬퍼를 잡으세요." :
                    prepHeld ? "청록색 바스켓 위에 탬퍼를 놓으세요." : "탬퍼 머리를 누른 채 아래로 당겨 꾹 누르세요.",
                prepHeld ? null : art.Tamper,
                prepKind == 2 && prepContact ? (Vector3?)null : art.Tool.position + Vector3.up * .17f,
                release ? "손 놓기 · 탬퍼" : prepKind == 2 ? "누르기 · 탬퍼" : "잡기 · 탬퍼", "놓기 · 바스켓", radius: .26f);
        }

        private void UpdateStepGuidance()
        {
            if (art == null) return;
            if (!StepGlowAllowed) { stepGlow?.Hide(); return; }
            StepHint hint = CurrentStepHint();
            if (hint.Target == null && !hint.Place.HasValue) { stepGlow?.Hide(); return; }
            if (stepGlow == null) stepGlow = new StepTargetGlow();
            stepGlow.Show(hint.Target, hint.Place, hint.Radius, Time.unscaledTime);
        }

        private void DrawStepGuidance()
        {
            if (art == null || paused || professionalMenuOpen || espressoOrderMenuOpen || CoffeeBeanSelectionOpen) return;
            StepHint hint = CurrentStepHint();
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity, Vector3.one * frame.width / 960);
            GUI.color = Color.white;
            try
            {
                if (hint.Button.HasValue && !Moving) DrawGuideButtonFrame(hint.Button.Value);
                if (professionalReportOpen)
                {
                    if (TutorialVisible) return;
                    BeanPanel(new Rect(134, 5, 692, 26), new Color(.04f, .065f, .09f, .97f));
                    GUI.Label(new Rect(145, 6, 670, 24), "다음 · " + hint.Instruction, BeanText(13, GuideGold, true));
                    return;
                }
                Rect card = counterView ? new Rect(18, 345, 300, 92) :
                    latteActive || AmericanoView ? new Rect(18, 60, 300, 92) : new Rect(18, 176, 266, 79);
                // The order dialogue already explains the request beside its highlighted accept button.
                if (!TutorialVisible && counterView && (customerDialogueOpen || counterCustomer.Phase == CounterCustomerPhase.Arriving)) return;
                if (!TutorialVisible)
                {
                BeanPanel(card, new Color(.04f, .065f, .09f, .96f));
                BeanPanel(new Rect(card.x, card.y, 4, card.height), GuideGold);
                GUI.Label(new Rect(card.x + 12, card.y + 5, card.width - 24, 23),
                    $"지금 할 일 · {hint.Stage} / 8  {GuideStages[hint.Stage]}", BeanText(14, GuideGold, true));
                GUI.Label(new Rect(card.x + 12, card.y + 29, card.width - 24, 34),
                    Moving ? "도구가 자리를 잡는 중이에요. 잠깐 기다려요." : hint.Instruction,
                    BeanText(13, EspressoInk, true, true));
                GUI.Label(new Rect(card.x + 12, card.y + card.height - 19, card.width - 24, 17),
                    "금색: 잡기·누르기   청록: 놓을 곳", BeanText(11, EspressoMuted));
                }
                if (!StepGlowAllowed) return;
                if (hint.Target != null)
                {
                    Vector3 anchor = hint.Target == art.Tool ? BasketGrip() : hint.Target.position + Vector3.up * .18f;
                    DrawGuideWorldLabel(anchor, hint.TargetLabel, GuideGold, false);
                }
                if (hint.Place.HasValue)
                    DrawGuideWorldLabel(hint.Place.Value, hint.PlaceLabel, GuideCyan, true);
            }
            finally { GUI.matrix = previous; GUI.color = previousColor; }
        }

        private void DrawGuideButtonFrame(Rect button)
        {
            Color glow = GuideGold;
            glow.a = .65f + .35f * Mathf.Sin(Time.unscaledTime * 3f);
            const float thickness = 3;
            BeanPanel(new Rect(button.x - 4, button.y - 4, button.width + 8, thickness), glow);
            BeanPanel(new Rect(button.x - 4, button.yMax + 1, button.width + 8, thickness), glow);
            BeanPanel(new Rect(button.x - 4, button.y - 1, thickness, button.height + 2), glow);
            BeanPanel(new Rect(button.xMax + 1, button.y - 1, thickness, button.height + 2), glow);
        }

        private void DrawGuideWorldLabel(Vector3 world, string label, Color color, bool destination)
        {
            if (string.IsNullOrEmpty(label)) return;
            Vector3 projected = art.Camera.WorldToViewportPoint(world);
            if (projected.z <= 0) return;
            float x = projected.x * 960;
            float y = (1 - projected.y) * 540;
            bool offscreen = x < 310 || x > 862;
            string direction = offscreen ? x < 310 ? "← " : "→ " : destination ? "↓ " : "";
            float width = Mathf.Clamp(label.Length * 12f + 32, 144, 240);
            Rect tag = new Rect(Mathf.Clamp(x - width * .5f, 310, 944 - width),
                Mathf.Clamp(y + (destination ? 18 : -43), counterView ? 240 : 242, counterView ? 312 : 431), width, 28);
            BeanPanel(tag, new Color(.035f, .055f, .075f, .94f));
            BeanPanel(new Rect(tag.x, tag.yMax - 2, tag.width, 2), color);
            GUI.Label(new Rect(tag.x + 7, tag.y + 3, tag.width - 14, 24), direction + label, BeanText(12, color, true));
        }
    }
}
