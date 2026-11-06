using CloudCafe.Coffee;
using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private FirstOrderTutorialModel tutorial;
        private bool firstOrderTutorialSmoke;
        private string tutorialSuccess;
        private float tutorialSuccessUntil, tutorialCompleteUntil;
        private bool TutorialActive => tutorial != null && tutorial.Active;
        private bool TutorialVisible => TutorialActive || (tutorial != null && tutorial.Completed &&
            customerVisit?.Phase == CustomerVisitPhase.Reaction && Time.unscaledTime < tutorialCompleteUntil);

        private static readonly string[] TutorialTitles =
        {
            "첫 주문 받기", "포터필터 잡기", "분쇄 받침에 놓기", "분쇄 시작하기", "저울을 보고 분쇄 멈추기",
            "마지막 가루 기다리기", "가루가 담긴 손잡이 잡기", "작업 매트에 놓기", "원두 양 확정하기",
            "표면 정리 도구 잡기", "바스켓 위에 놓기", "원을 그리며 고르게 펴기", "정리 도구에서 손 놓기",
            "탬퍼 잡기", "바스켓 위에 탬퍼 놓기", "아래로 눌러 다지기", "탬퍼에서 손 놓기",
            "준비된 포터필터 잡기", "머신 헤드에 끼우기", "손잡이를 돌려 잠그기", "빈 컵 잡기",
            "추출구 아래에 컵 놓기", "추출 시작하기", "저울을 보고 추출 멈추기", "마지막 커피 기다리기",
            "에스프레소 선택하기", "완성 컵을 카운터로 가져가기", "손님에게 줄 컵 잡기",
            "손님 앞 전달 원으로 옮기기", "손을 놓아 직접 건네기", "첫 주문 튜토리얼 완료!"
        };

        private string TutorialTitle => TutorialTitles[(int)tutorial.Step];
        private string TutorialCountLabel => tutorial.Completed ? "30 / 30" : $"{tutorial.CompletedStepCount + 1} / 30";
        private float TutorialProgress => tutorial.CompletedStepCount / (float)FirstOrderTutorialModel.TotalActionSteps;
        private string TutorialSuccessNote => Time.unscaledTime < tutorialSuccessUntil ? tutorialSuccess :
            "직접 성공하면 자동으로 넘어가요.\nR: 주문은 유지하고 제작 다시 시작";

        private string TutorialInstruction
        {
            get
            {
                switch (tutorial.Step)
                {
                    case FirstOrderTutorialStep.AcceptOrder:
                        return "첫 잔은 예가체프 미디엄 에스프레소예요. 주문을 읽고 ‘알겠습니다’를 누르세요.";
                    case FirstOrderTutorialStep.PickPortafilter:
                    case FirstOrderTutorialStep.ReturnPick:
                    case FirstOrderTutorialStep.PickPreparedBasket:
                        return "금색 손잡이 위에서 마우스 왼쪽 버튼을 누르고 있으세요. 손을 떼지 않고 옮겨요.";
                    case FirstOrderTutorialStep.PlaceAtGrinder:
                        return held ? "누른 채 왼쪽 출구 아래 청록색 받침으로 옮긴 뒤 버튼에서 손을 놓으세요." :
                            "아직 받침에 놓이지 않았어요. 손잡이를 다시 잡고 청록색 원에 내려놓으세요.";
                    case FirstOrderTutorialStep.StartGrinder:
                        return "화면의 ‘분쇄 시작’ 또는 기계의 금색 버튼을 한 번 누르세요. Space도 같아요.";
                    case FirstOrderTutorialStep.StopGrinder:
                        return Model.Running ? $"저울 {Model.DoseInBasketOrFlight:0.0}g. 18g 근처에서 같은 버튼이나 Space로 멈추세요." :
                            "가루가 부족해요. 같은 버튼으로 다시 시작해 18g 근처에서 멈추세요. 과하게 갈았다면 R로 다시 해요.";
                    case FirstOrderTutorialStep.WaitGrounds:
                        return "분쇄는 멈췄지만 가루가 떨어지는 중이에요. 다 담길 때까지 잠깐 기다려요.";
                    case FirstOrderTutorialStep.PlaceOnMat:
                        return held ? "손을 누른 채 청록색 작업 매트로 옮긴 다음 놓으세요." :
                            "손잡이를 다시 잡아 청록색 매트에 내려놓으세요. 다른 곳은 성공으로 세지 않아요.";
                    case FirstOrderTutorialStep.CommitDose:
                        return $"바스켓에 {Model.Grounds:0.0}g이 담겼어요. ‘도징 확정’ 버튼이나 Enter를 누르세요.";
                    case FirstOrderTutorialStep.PickLeveler:
                        return "오른쪽의 낮고 둥근 금색 도구를 왼쪽 버튼으로 누른 채 들어 올리세요.";
                    case FirstOrderTutorialStep.PlaceLeveler:
                        return prepHeld ? "누른 채 청록색 바스켓 가운데로 옮기고 손을 놓으세요." :
                            "도구가 돌아갔다면 다시 잡아 청록색 바스켓 위에 놓으세요.";
                    case FirstOrderTutorialStep.RotateLeveler:
                        return $"도구를 누른 채 마우스로 원을 그리세요. 고르게 펴기 {Prep.LevelProgress:P0}. 가득 차도 아직 손을 누르고 있어요.";
                    case FirstOrderTutorialStep.ReleaseLeveler:
                        return "표면 정리가 끝났어요. 마우스 버튼에서 손을 놓으면 도구가 돌아가요.";
                    case FirstOrderTutorialStep.PickTamper:
                        return "이번에는 손잡이가 높은 탬퍼를 누른 채 들어 올리세요. 금색 윤곽선을 따라가요.";
                    case FirstOrderTutorialStep.PlaceTamper:
                        return prepHeld ? "탬퍼를 청록색 바스켓 가운데로 옮기고 손을 놓으세요." :
                            "탬퍼를 다시 잡아 청록색 바스켓 위에 내려놓으세요.";
                    case FirstOrderTutorialStep.PressTamper:
                        return $"탬퍼 머리를 누른 채 아래로 당기세요. 깊이 {Prep.Depth:P0} / 목표 85%. 유지 {Prep.TampHoldProgress:P0}.";
                    case FirstOrderTutorialStep.ReleaseTamper:
                        return "충분히 다졌어요. 마우스 버튼에서 손을 놓으세요. 너무 얕아졌다면 다시 꾹 눌렀다 놓아요.";
                    case FirstOrderTutorialStep.InsertBasket:
                        return held ? "오른쪽으로 옮기면 시야도 따라가요. 머신 헤드의 청록색 원에서 손을 놓으세요." :
                            "손잡이를 다시 잡아 오른쪽 머신 헤드의 청록색 원으로 가져가 놓으세요.";
                    case FirstOrderTutorialStep.LockBasket:
                        return "끼워진 손잡이를 누른 채 마우스를 오른쪽으로 당겨 끝까지 잠그세요.";
                    case FirstOrderTutorialStep.PickCup:
                        return "오른쪽의 빈 컵을 왼쪽 버튼으로 누른 채 들어 올리세요.";
                    case FirstOrderTutorialStep.PlaceCup:
                        return cupHeld ? "컵을 머신 추출구 아래의 청록색 받침으로 옮기고 손을 놓으세요." :
                            "컵을 다시 잡아 추출구 아래의 청록색 받침에 놓으세요.";
                    case FirstOrderTutorialStep.StartExtraction:
                        return "컵까지 준비됐어요. ‘추출 시작’, 기계의 금색 버튼 또는 Space를 누르세요.";
                    case FirstOrderTutorialStep.StopExtraction:
                        return Brew.Running ? $"저울 {Brew.InCup:0.0}g. 추천 {Model.Grounds * professionalBeanRecipe.BrewRatio:0.0}g 근처에서 같은 버튼으로 멈추세요." :
                            "커피가 나오기 전에 멈췄어요. 같은 버튼으로 다시 시작한 뒤 커피가 담기면 멈춰요.";
                    case FirstOrderTutorialStep.WaitExtraction:
                        return "마지막 커피가 떨어지고 있어요. 컵에 다 담기면 음료 선택창이 열려요.";
                    case FirstOrderTutorialStep.ChooseEspresso:
                        return "이 주문은 ‘에스프레소 그대로’를 누르세요. 선택창을 닫았다면 음료 선택 버튼으로 다시 열어요.";
                    case FirstOrderTutorialStep.CarryToCounter:
                        return "‘카운터로 가져가기’를 누르세요. 컵을 가져가는 것과 손님에게 건네는 것은 따로 해요.";
                    case FirstOrderTutorialStep.PickServingCup:
                        return "금색으로 빛나는 완성 컵을 마우스 왼쪽 버튼으로 누른 채 잡으세요.";
                    case FirstOrderTutorialStep.PlaceServingCup:
                        return servingCupHeld ? "버튼을 누른 채 컵을 손님 앞의 청록색 전달 원 안으로 옮기세요." :
                            "전달 원 밖에 놓였어요. 컵을 다시 잡고 청록색 원 안으로 옮기세요.";
                    case FirstOrderTutorialStep.ReleaseServingCup:
                        return servingCupHeld && ServingCupNearCustomer() ? "위치가 맞았어요! 마우스 버튼에서 손을 놓아 직접 건네세요." :
                            "컵을 다시 잡아 청록색 전달 원 안으로 옮기고 손을 놓으세요.";
                    default:
                        return "첫 잔을 직접 만들어 건넸어요! 다음 손님부터는 일반 주문으로 이어집니다.";
                }
            }
        }

        private bool BeginFirstOrderTutorial()
        {
            CoffeeBeanDefinition bean = coffeeBeanCatalog?.Find("yirgacheffe_kurume");
            if (bean == null) return false;
            if (!StationReadyForSettings()) ResetStation();
            ApplyEspressoOrder(new EspressoCustomerOrder(bean.Id, EspressoRoast.Medium, CoffeeDrink.Espresso));
            selectedCoffeeBean = previewCoffeeBean = bean;
            selectedCoffeeRoast = previewCoffeeRoast = EspressoRoast.Medium;
            ApplyRecommendedBeanRecipe();
            professionalPressure = ProfessionalEspressoModel.TargetPressureBar;
            customerVisit = new CustomerVisitModel(0, customerProgress.NextVisitId, CoffeeDrink.Espresso, 420f);
            tutorial ??= new FirstOrderTutorialModel();
            tutorial.Begin();
            tutorialSuccess = null;
            tutorialSuccessUntil = tutorialCompleteUntil = 0;
            BeginCounterVisit();
            return true;
        }

        private void TutorialDid(FirstOrderTutorialStep step, bool success = true)
        {
            if (!TutorialActive || !tutorial.TryCompleteStep(step, success)) return;
            tutorialSuccess = "✓ " + TutorialTitles[(int)step] + " 성공!";
            tutorialSuccessUntil = Time.unscaledTime + 2.5f;
            if (!tutorial.Completed) return;
            tutorialCompleteUntil = Time.unscaledTime + 6f;
            customerProgress.FirstOrderTutorialCompleted = true;
            SaveCustomerProgress();
        }

        private void UpdateFirstOrderTutorial()
        {
            if (!TutorialActive || paused || Moving) return;
            switch (tutorial.Step)
            {
                case FirstOrderTutorialStep.WaitGrounds:
                    TutorialDid(FirstOrderTutorialStep.WaitGrounds, Model.Ready);
                    break;
                case FirstOrderTutorialStep.WaitExtraction:
                    TutorialDid(FirstOrderTutorialStep.WaitExtraction, Brew.Complete);
                    break;
                case FirstOrderTutorialStep.PlaceServingCup:
                    TutorialDid(FirstOrderTutorialStep.PlaceServingCup, servingCupHeld && ServingCupNearCustomer());
                    break;
            }
        }

        private void TutorialPreparationRestarted()
        {
            if (!TutorialActive) return;
            if (tutorial.Step == FirstOrderTutorialStep.AcceptOrder)
            {
                EnterCounterView();
                return;
            }
            tutorial.RestartPreparation();
            tutorialSuccess = "주문은 유지했어요. 도구 잡기부터 다시 해요.";
            tutorialSuccessUntil = Time.unscaledTime + 3f;
        }

        private bool TutorialAllows(string action)
        {
            if (!TutorialActive) return true;
            FirstOrderTutorialStep step = tutorial.Step;
            bool allowed = action switch
            {
                "accept" => step == FirstOrderTutorialStep.AcceptOrder,
                "basket.pick" => step == FirstOrderTutorialStep.PickPortafilter || step == FirstOrderTutorialStep.ReturnPick ||
                    step == FirstOrderTutorialStep.PickPreparedBasket || step == FirstOrderTutorialStep.PlaceAtGrinder ||
                    step == FirstOrderTutorialStep.PlaceOnMat || step == FirstOrderTutorialStep.InsertBasket,
                "basket.drop" => step == FirstOrderTutorialStep.PlaceAtGrinder || step == FirstOrderTutorialStep.PlaceOnMat || step == FirstOrderTutorialStep.InsertBasket,
                "grinder.toggle" => step == FirstOrderTutorialStep.StartGrinder || step == FirstOrderTutorialStep.StopGrinder,
                "dose.confirm" => step == FirstOrderTutorialStep.CommitDose,
                "level.pick" => step == FirstOrderTutorialStep.PickLeveler || step == FirstOrderTutorialStep.PlaceLeveler,
                "tamp.pick" => step == FirstOrderTutorialStep.PickTamper || step == FirstOrderTutorialStep.PlaceTamper,
                "prep.drop" => step == FirstOrderTutorialStep.PlaceLeveler || step == FirstOrderTutorialStep.PlaceTamper,
                "level.rotate" => step == FirstOrderTutorialStep.RotateLeveler || step == FirstOrderTutorialStep.ReleaseLeveler,
                "level.release" => step == FirstOrderTutorialStep.RotateLeveler || step == FirstOrderTutorialStep.ReleaseLeveler,
                "tamp.press" or "tamp.release" => step == FirstOrderTutorialStep.PressTamper || step == FirstOrderTutorialStep.ReleaseTamper,
                "machine.lock" => step == FirstOrderTutorialStep.LockBasket,
                "cup.pick" => step == FirstOrderTutorialStep.PickCup || step == FirstOrderTutorialStep.PlaceCup ||
                    step == FirstOrderTutorialStep.ChooseEspresso || step == FirstOrderTutorialStep.CarryToCounter,
                "cup.drop" => step == FirstOrderTutorialStep.PlaceCup,
                "brew.toggle" => step == FirstOrderTutorialStep.StartExtraction || step == FirstOrderTutorialStep.StopExtraction,
                "drink.choose" => step == FirstOrderTutorialStep.ChooseEspresso,
                "counter.carry" or "counter.toggle" => step == FirstOrderTutorialStep.CarryToCounter,
                "serve.pick" => step == FirstOrderTutorialStep.PickServingCup || step == FirstOrderTutorialStep.PlaceServingCup || step == FirstOrderTutorialStep.ReleaseServingCup,
                "serve.drop" => step == FirstOrderTutorialStep.PlaceServingCup || step == FirstOrderTutorialStep.ReleaseServingCup,
                _ => false
            };
            if (!allowed) message = "첫 손님 튜토리얼 · 먼저 ‘" + TutorialTitle + "’ 단계를 해 보세요.";
            return allowed;
        }

        private StepHint TutorialStepHint()
        {
            if (tutorial.Step == FirstOrderTutorialStep.AcceptOrder && counterCustomer.Phase == CounterCustomerPhase.Arriving)
                return DefaultStepHint();
            StepHint normal = DefaultStepHint();
            string id = "tutorial." + tutorial.Step;
            switch (tutorial.Step)
            {
                case FirstOrderTutorialStep.StartGrinder:
                case FirstOrderTutorialStep.StopGrinder:
                    return new StepHint(id, 1, TutorialInstruction, art.Switch, targetLabel: "누르기 · 분쇄 버튼", button: EspressoActionButton);
                case FirstOrderTutorialStep.RotateLeveler:
                case FirstOrderTutorialStep.ReleaseLeveler:
                    return new StepHint(id, 2, TutorialInstruction, art.Distributor, targetLabel: "조작 · 표면 정리 도구");
                case FirstOrderTutorialStep.PressTamper:
                case FirstOrderTutorialStep.ReleaseTamper:
                    return new StepHint(id, 3, TutorialInstruction, art.Tamper, targetLabel: "조작 · 탬퍼");
                case FirstOrderTutorialStep.StartExtraction:
                case FirstOrderTutorialStep.StopExtraction:
                    return new StepHint(id, 6, TutorialInstruction, art.BrewButton, targetLabel: "누르기 · 추출 버튼", button: EspressoActionButton);
                case FirstOrderTutorialStep.WaitGrounds:
                    return new StepHint(id, 1, TutorialInstruction);
                case FirstOrderTutorialStep.WaitExtraction:
                    return new StepHint(id, 6, TutorialInstruction);
                default:
                    return new StepHint(id, normal.Stage, TutorialInstruction, normal.Target, normal.Place,
                        normal.TargetLabel, normal.PlaceLabel, normal.Button, normal.Radius);
            }
        }
    }
}
