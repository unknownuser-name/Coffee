using CloudCafe.Coffee;
using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Rect DifficultyButton = new Rect(350, 6, 146, 36);
        private static readonly Rect EspressoActionButton = new Rect(26, 128, 232, 40);
        private static readonly Color EspressoInk = new Color(.98f, .92f, .81f);
        private static readonly Color EspressoMuted = new Color(.78f, .75f, .69f);
        private static readonly Color EspressoAccent = new Color(.99f, .75f, .46f);

        private bool professionalMode, professionalMenuOpen, professionalReportOpen, professionalReportShown;
        private bool professionalReleaseGuard, draftProfessional;
        private CursorLockMode professionalCursorLock;
        private bool professionalCursorVisible;
        private int professionalGrind = ProfessionalEspressoModel.NominalGrindSetting, draftGrind;
        private float professionalTemperature = ProfessionalEspressoModel.TargetTemperatureCelsius, draftTemperature;
        private float professionalPressure = ProfessionalEspressoModel.TargetPressureBar, draftPressure;
        private float tampStartX, tampTiltIntegral, tampFirmSeconds, professionalLiveTampTilt, professionalPuckUniformity = 1;
        private EspressoBeanProfile professionalBeanRecipe = EspressoBeanProfiles.ForBean(null, EspressoRoast.Medium);
        private EspressoBeanProfile professionalShotBeanRecipe;
        private ProfessionalEspressoModel professionalShot;
        private bool ShowDoseButton => !brewView && !Moving && Model.Slot == CraftSlot.Mat &&
            Model.Ready && !Model.DoseCommitted;

        private void ApplyRecommendedBeanRecipe()
        {
            professionalBeanRecipe = EspressoBeanProfiles.ForBean(selectedCoffeeBean?.Id, selectedCoffeeRoast);
            professionalGrind = professionalBeanRecipe.GrindSetting;
            professionalTemperature = professionalBeanRecipe.TemperatureCelsius;
        }

        private void SetDraftBeanRecommendation()
        {
            draftGrind = professionalBeanRecipe.GrindSetting;
            draftTemperature = professionalBeanRecipe.TemperatureCelsius;
        }

        private string ProfessionalBeanName(EspressoRoast roast)
        {
            if (selectedCoffeeBean == null) return "기본 레시피";
            return $"{OriginName(selectedCoffeeBean)} · {selectedCoffeeBean.VarietyKo} · {EspressoBeanProfiles.RoastNameKo(roast)}";
        }

        private Vector2 StationGuiPointer()
        {
            return new Vector2((Input.mousePosition.x - frame.x) * 960f / frame.width,
                (Screen.height - Input.mousePosition.y - frame.y) * 540f / frame.height);
        }

        private bool TryApplyDifficulty(bool professional, int grind, float temperature, float pressure)
        {
            if (!StationReadyForSettings() || !Model.SetManualDose(professional)) return false;
            professionalMode = professional;
            professionalGrind = grind;
            professionalTemperature = temperature;
            professionalPressure = pressure;
            if (professionalMode) EnsureEspressoOrder();
            else
            {
                CloseCustomerDialogue();
                LeaveCounterView();
                art.SetCounterVisible(false);
            }
            message = professionalMode
                ? "프로페셔널 모드. 저울을 보며 분쇄를 멈추고 도징을 확정하세요."
                : "기본 모드. 손잡이를 잡아 들어 올려 보세요.";
            return true;
        }

        private void OpenDifficultyMenu()
        {
            if (!StationReadyForSettings())
            {
                message = "난이도와 레시피는 분쇄 전 설정할 수 있어요. R로 새 잔을 준비하세요.";
                return;
            }
            draftProfessional = professionalMode;
            draftGrind = professionalGrind;
            draftTemperature = professionalTemperature;
            draftPressure = professionalPressure;
            SaveProfessionalCursor();
            professionalMenuOpen = true;
        }

        private void SaveProfessionalCursor()
        {
            professionalCursorLock = Cursor.lockState;
            professionalCursorVisible = Cursor.visible;
        }

        private void CloseProfessionalOverlay()
        {
            professionalMenuOpen = professionalReportOpen = false;
            professionalReleaseGuard = true;
            Cursor.lockState = Application.isFocused ? professionalCursorLock : CursorLockMode.None;
            Cursor.visible = Application.isFocused ? professionalCursorVisible : true;
        }

        private void OpenShotReport()
        {
            SaveProfessionalCursor();
            professionalReportOpen = true;
        }

        private bool UpdateProfessionalCursor()
        {
            if (professionalMenuOpen || professionalReportOpen)
                Cursor.lockState = CursorLockMode.None;
            else if (counterView || latteActive || AmericanoView ||
                !EspressoActionButton.Contains(StationGuiPointer()) ||
                !(Brew.Complete || HasGrinderAction || HasBrewAction || (professionalMode && ShowDoseButton)))
                return false;
            Cursor.visible = true;
            handCursor?.Set(HandPose.Open, Vector2.zero, false);
            return true;
        }

        private bool HandleProfessionalInput()
        {
            if (professionalReleaseGuard)
            {
                if (!Input.GetMouseButton(0)) professionalReleaseGuard = false;
                return true;
            }
            if (professionalMenuOpen || professionalReportOpen)
            {
                if (professionalReportOpen && Input.GetKeyDown(KeyCode.R))
                {
                    CloseProfessionalOverlay();
                    ResetStation();
                }
                else if (Input.GetKeyDown(KeyCode.Escape) ||
                    (professionalMenuOpen && Input.GetKeyDown(KeyCode.D)))
                {
                    CloseProfessionalOverlay();
                }
                return true;
            }
            if (smoke || counterView || latteActive || AmericanoView || CoffeeBeanSelectionOpen) return false;

            bool click = Input.GetMouseButtonDown(0);
            Vector2 pointer = StationGuiPointer();
            if (Input.GetKeyDown(KeyCode.D) || (click && DifficultyButton.Contains(pointer)))
            {
                OpenDifficultyMenu();
                return true;
            }
            if (paused) return false;
            if (click && EspressoActionButton.Contains(pointer) && HandleWorkshopAction())
                return true;
            if (Brew.Complete && (Input.GetKeyDown(KeyCode.Return) ||
                (click && EspressoActionButton.Contains(pointer))))
            {
                TryOpenDrinkReport();
                return true;
            }
            if (!professionalMode) return false;
            if (!brewView && !Model.DoseCommitted &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    (click && ShowDoseButton && EspressoActionButton.Contains(pointer))))
            {
                CommitProfessionalDose();
                return true;
            }
            return false;
        }

        private bool CommitProfessionalDose()
        {
            if (Moving || !Model.CommitDose())
            {
                message = "분쇄를 멈추고 가루가 다 떨어지면 바스켓을 매트에 놓으세요. 최소 12g이 필요해요.";
                return false;
            }
            message = $"도징 {Model.Grounds:0.0}g 확정. 분배 도구로 표면을 고르게 펴 주세요.";
            return true;
        }

        private void RecordProfessionalTamp(float depth, float dt, float tilt)
        {
            if (!professionalMode) return;
            professionalLiveTampTilt = Mathf.Clamp01(tilt);
            if (depth < PuckPrepModel.RequiredTampDepth)
            {
                tampTiltIntegral = tampFirmSeconds = 0;
                return;
            }
            float sampleTime = Mathf.Clamp(dt, 0, .05f);
            tampTiltIntegral += Mathf.Clamp01(tilt) * sampleTime;
            tampFirmSeconds += sampleTime;
        }

        private void FinishProfessionalTamp()
        {
            if (professionalMode && Prep.Complete && tampFirmSeconds > 0)
                professionalPuckUniformity = 1 - tampTiltIntegral / tampFirmSeconds;
            tampTiltIntegral = tampFirmSeconds = professionalLiveTampTilt = 0;
        }

        private void ResetProfessionalShot()
        {
            ResetCounterShot();
            if (customerVisit?.Phase != CustomerVisitPhase.Talking) CloseCustomerDialogue();
            if (professionalMenuOpen || professionalReportOpen) CloseProfessionalOverlay();
            professionalShot = null;
            professionalShotBeanRecipe = default;
            professionalReportShown = false;
            tampTiltIntegral = tampFirmSeconds = professionalLiveTampTilt = 0;
            professionalPuckUniformity = 1;
            ClearEspressoOrderShot();
        }

        private void PrepareProfessionalBrew()
        {
            if (!professionalMode || professionalShot != null || !Brew.Locked || !Brew.CupDocked) return;
            var recipe = new ProfessionalEspressoModel(Model.Grounds, professionalGrind,
                professionalPuckUniformity, professionalTemperature, professionalPressure, professionalBeanRecipe);
            if (!Brew.ConfigureProfessional(recipe)) return;
            professionalShot = recipe;
            professionalShotBeanRecipe = professionalBeanRecipe;
            CaptureEspressoOrder(selectedCoffeeBean?.Id, selectedCoffeeRoast);
        }

        private void StepStationBrew(float dt)
        {
            Brew.Step(dt);
            if ((!smoke || professionalSmoke) && !professionalReportShown && Brew.Complete)
            {
                professionalReportShown = true;
                TryOpenDrinkReport();
            }
        }

        private string ProfessionalReadout()
        {
            if (brewView) return $"추출 {Brew.InCup:0.0}g · {Brew.ElapsedSeconds:0.0}초";
            return $"바스켓 {Model.Grounds:0.0}g · 낙하 {Model.Flying:0.0}g";
        }

        private string ProfessionalControls()
        {
            if (brewView) return "컵 놓기 → 금색 버튼 / Space로 추출 시작·정지  |  R 새 잔 · Esc 쉬기";
            return "금색 버튼 / Space로 분쇄 시작·정지 → 매트에서 Enter로 도징 확정  |  R 새 잔 · Esc 쉬기";
        }

        private void DrawProfessionalToolbar()
        {
            if (!smoke)
                GUI.Button(DifficultyButton, professionalMode ? "프로페셔널 [D]" : "난이도 · 기본 [D]", BeanActionStyle());
            if (!professionalMode)
            {
                DrawWorkshopAction();
                return;
            }

            BeanPanel(new Rect(18, 52, 289, 64), new Color(.07f, .09f, .14f, .94f));
            float targetMass = Model.Grounds * professionalBeanRecipe.BrewRatio;
            string goal = Model.DoseCommitted
                ? $"추출 목표  {targetMass:0.0}g · 1:{professionalBeanRecipe.BrewRatio:0.00}"
                : $"도징 목표  18g · 추천 1:{professionalBeanRecipe.BrewRatio:0.00}";
            GUI.Label(new Rect(29, 57, 272, 24), goal,
                BeanText(15, EspressoAccent, true));
            GUI.Label(new Rect(29, 84, 272, 23),
                $"분쇄도 {professionalGrind}/9  ·  {professionalTemperature:0.0}°C  ·  {professionalPressure:0.0} bar",
                BeanText(13, EspressoInk));
            DrawWorkshopAction();
            DrawEspressoOrderTicket();
        }

        private bool DrawProfessionalOverlay()
        {
            if (!professionalMenuOpen && !professionalReportOpen) return false;
            GUI.DrawTexture(frame, art.Pixels, ScaleMode.StretchToFill, false);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                Vector3.one * frame.width / 960);
            try
            {
                BeanPanel(new Rect(0, 0, 960, 540), new Color(.03f, .04f, .06f, .79f));
                Color surface = new Color(.10f, .12f, .16f);
                if (professionalMenuOpen)
                {
                    BeanPanel(new Rect(208, 44, 544, 452), surface);
                    DrawDifficultyMenu();
                }
                else if (!professionalMode)
                {
                    DrawBasicDrinkReport();
                }
                else
                {
                    BeanPanel(new Rect(109, 25, 742, 490), surface);
                    DrawShotReport();
                }
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
            return true;
        }

        private void DrawDifficultyMenu()
        {
            GUI.Label(new Rect(234, 66, 482, 35), "난이도 선택", BeanText(25, EspressoInk, true));
            GUI.Label(new Rect(234, 108, 482, 25), "이번 잔에 적용할 플레이 방식을 고르세요.", BeanText(14, EspressoMuted));
            if (GUI.Button(new Rect(234, 145, 231, 43), draftProfessional ? "기본" : "기본  ✓", BeanActionStyle()))
                draftProfessional = false;
            if (GUI.Button(new Rect(483, 145, 241, 43), draftProfessional ? "프로페셔널  ✓" : "프로페셔널", BeanActionStyle()))
                draftProfessional = true;
            string description = draftProfessional
                ? ProfessionalBeanName(selectedCoffeeRoast)
                : "분쇄 18g과 추출 36g에서 자동으로 멈춥니다.";
            GUI.Label(new Rect(234, 200, 490, 25), description, BeanText(14, EspressoInk));
            if (draftProfessional)
            {
                draftGrind = Mathf.RoundToInt(DrawRecipeSlider(242, $"분쇄도  {draftGrind}/9", "1 고움 · 9 굵음",
                    draftGrind, ProfessionalEspressoModel.MinGrindSetting, ProfessionalEspressoModel.MaxGrindSetting));
                draftTemperature = Mathf.Round(DrawRecipeSlider(293, $"물 온도  {draftTemperature:0.0}°C", "90.5–96.0°C",
                    draftTemperature, ProfessionalEspressoModel.MinTemperatureCelsius, ProfessionalEspressoModel.MaxTemperatureCelsius) * 2) / 2;
                draftPressure = Mathf.Round(DrawRecipeSlider(344, $"펌프 압력  {draftPressure:0.0} bar", "8.5–9.5 bar",
                    draftPressure, ProfessionalEspressoModel.MinPressureBar, ProfessionalEspressoModel.MaxPressureBar) * 10) / 10;
                GUI.Label(new Rect(234, 396, 350, 26),
                    $"원두 추천  {professionalBeanRecipe.GrindSetting}/9 · {professionalBeanRecipe.TemperatureCelsius:0.0}°C · 1:{professionalBeanRecipe.BrewRatio:0.00}",
                    BeanText(14, EspressoAccent, true));
                if (GUI.Button(new Rect(594, 395, 130, 27), "추천값 불러오기", BeanActionStyle()))
                    SetDraftBeanRecommendation();
            }
            if (GUI.Button(new Rect(474, 440, 109, 40), "취소 [Esc]", BeanActionStyle()))
                CloseProfessionalOverlay();
            if (GUI.Button(new Rect(594, 440, 130, 40), "선택 적용", BeanActionStyle()) &&
                TryApplyDifficulty(draftProfessional, draftGrind, draftTemperature, draftPressure))
                CloseProfessionalOverlay();
        }

        private float DrawRecipeSlider(float y, string name, string range, float value, float min, float max)
        {
            GUI.Label(new Rect(234, y, 188, 24), name, BeanText(15, EspressoInk, true));
            GUI.Label(new Rect(234, y + 21, 188, 20), range, BeanText(12, EspressoMuted));
            return GUI.HorizontalSlider(new Rect(430, y + 12, 290, 22), value, min, max);
        }

        private void DrawShotReport()
        {
            ProfessionalEspressoResult result = professionalShot.Evaluate(Brew.InCup, Brew.ElapsedSeconds);
            EspressoBeanProfile shotRecipe = professionalShotBeanRecipe;
            string title = finishedDrink.HasValue ? CoffeeDrinkName(finishedDrink.Value) + " · 샷 리포트" : "음료 선택 · 샷 리포트";
            GUI.Label(new Rect(134, 43, 425, 37), title, BeanText(25, EspressoInk, true));
            GUI.Label(new Rect(617, 48, 209, 32), $"레시피 점수  {result.Score} / 100", BeanText(19, EspressoAccent, true));
            GUI.Label(new Rect(134, 84, 690, 25), ProfessionalBeanName(shotRecipe.Roast), BeanText(15, EspressoAccent, true));
            GUI.Label(new Rect(134, 112, 690, 25),
                $"원두 {result.DoseGrams:0.0}g → 음료 {result.BeverageGrams:0.0}g   ·   추출 {result.ElapsedSeconds:0.0}초 "
                + $"(참고 {professionalShot.RecommendedTimeMinSeconds:0}–{professionalShot.RecommendedTimeMaxSeconds:0}초)   ·   {professionalShot.PressureBar:0.0} bar",
                BeanText(15, EspressoInk));
            GUI.Label(new Rect(134, 141, 690, 24),
                $"예상 TDS {result.TdsPercent:0.0}%   ·   예상 EY {result.ExtractionYieldPercent:0.0}% "
                + $"(기준 {professionalShot.RecommendedExtractionYieldPercent:0.0}%)   ·   채널링 위험 {result.ChannelingRisk:P0}",
                BeanText(14, EspressoMuted));

            GUIStyle headings = BeanText(13, EspressoAccent, true);
            GUI.Label(new Rect(143, 178, 80, 22), "항목", headings);
            GUI.Label(new Rect(225, 178, 85, 22), "이번 샷", headings);
            GUI.Label(new Rect(315, 178, 85, 22), "추천", headings);
            GUI.Label(new Rect(405, 178, 405, 22), "이 원두와 로스팅에 추천하는 이유", headings);
            DrawRecipeReportRow(202, "분쇄도", $"{professionalShot.GrindSetting}/9",
                $"{shotRecipe.GrindSetting}/9", shotRecipe.GrindReasonKo);
            DrawRecipeReportRow(254, "물 온도", $"{professionalShot.TemperatureCelsius:0.0}°C",
                $"{shotRecipe.TemperatureCelsius:0.0}°C", shotRecipe.TemperatureReasonKo);
            DrawRecipeReportRow(306, "추출 비율", $"1:{result.BrewRatio:0.00}",
                $"1:{shotRecipe.BrewRatio:0.00}", shotRecipe.RatioReasonKo);

            BeanPanel(new Rect(134, 358, 692, 47), new Color(.18f, .20f, .24f));
            GUI.Label(new Rect(145, 363, 76, 25), "다음 샷", headings);
            GUI.Label(new Rect(224, 362, 590, 41), ProfessionalFeedback(result.Feedback),
                BeanText(14, EspressoInk, false, true));

            DrawDrinkChoiceOrSummary(409);
            Rect orderStatus = new Rect(134, 448, 692, 25);
            if (finishedDrink.HasValue)
                DrawEspressoOrderReport(orderStatus, new Rect(273, 475, 254, 40), finishedDrink.Value);
            else if (espressoShotOrder.HasRequest)
                GUI.Label(orderStatus, "손님 주문 · " + CoffeeDrinkName(espressoShotOrder.Drink), BeanText(13, EspressoMuted));
            DrawDrinkReportNavigation(475);
        }

        private void DrawRecipeReportRow(float y, string name, string actual, string recommended, string reason)
        {
            BeanPanel(new Rect(134, y, 692, 49), new Color(.14f, .16f, .20f));
            GUI.Label(new Rect(143, y + 8, 80, 32), name, BeanText(14, EspressoInk, true));
            GUI.Label(new Rect(225, y + 8, 85, 32), actual, BeanText(14, EspressoInk));
            GUI.Label(new Rect(315, y + 8, 85, 32), recommended, BeanText(14, EspressoAccent, true));
            GUI.Label(new Rect(405, y + 4, 408, 42), reason, BeanText(13, EspressoInk, false, true));
        }

        private string ProfessionalFeedback(ProfessionalEspressoFeedback feedback)
        {
            switch (feedback)
            {
                case ProfessionalEspressoFeedback.UnevenPuck: return "물이 퍽의 일부로 몰렸어요. 가루를 고르게 펴고 수직으로 눌러 보세요. 탬핑이 수평이었다면 분쇄를 한 단계 굵게 조정해 보세요.";
                case ProfessionalEspressoFeedback.DoseOffTarget: return "이번 연습 레시피는 18g을 기준으로 해요. 분쇄를 멈출 때 저울을 확인하세요.";
                case ProfessionalEspressoFeedback.RatioTooShort: return "원두 양에 비해 음료가 적어요. 저울이 목표 무게에 가까워질 때까지 추출해 보세요.";
                case ProfessionalEspressoFeedback.RatioTooLong: return "원두 양에 비해 음료가 많아요. 목표 무게에 가까워지면 추출을 멈춰 보세요.";
                case ProfessionalEspressoFeedback.UnderExtracted:
                    return professionalShot.TemperatureCelsius < professionalShotBeanRecipe.TemperatureCelsius
                        ? $"이 로스팅의 기준보다 예상 수율이 낮아요. 물을 추천 온도인 {professionalShotBeanRecipe.TemperatureCelsius:0.0}°C 쪽으로 높여 보세요."
                        : "이 로스팅의 기준보다 예상 수율이 낮아요. 분쇄도를 조금 더 곱게 조정해 보세요.";
                case ProfessionalEspressoFeedback.OverExtracted:
                    return professionalShot.TemperatureCelsius > professionalShotBeanRecipe.TemperatureCelsius
                        ? $"이 로스팅의 기준보다 예상 수율이 높아요. 물을 추천 온도인 {professionalShotBeanRecipe.TemperatureCelsius:0.0}°C 쪽으로 낮춰 보세요."
                        : "이 로스팅의 기준보다 예상 수율이 높아요. 분쇄도를 조금 더 굵게 조정해 보세요.";
                case ProfessionalEspressoFeedback.FlowTooFast: return "목표 무게에 빠르게 도달했어요. 다음 잔에서 분쇄를 조금 더 곱게 조정해 보세요.";
                case ProfessionalEspressoFeedback.FlowTooSlow: return "추출이 느렸어요. 다음 잔에서 분쇄를 조금 더 굵게 조정해 보세요.";
                case ProfessionalEspressoFeedback.OnTarget: return "이번 잔은 선택한 원두와 로스팅의 추천 범위에 가까워요.";
                default: return "저울을 확인하고 목표량에 맞춰 추출해 보세요.";
            }
        }
    }
}
