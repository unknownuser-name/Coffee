using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Rect DifficultyButton = new Rect(350, 7, 146, 28);
        private static readonly Rect EspressoActionButton = new Rect(30, 119, 144, 30);
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
        private float tampStartX, tampTiltIntegral, tampFirmSeconds, professionalPuckUniformity = 1;
        private ProfessionalEspressoModel professionalShot;
        private bool ShowDoseButton => !brewView && !Moving && Model.Slot == CraftSlot.Mat &&
            Model.Ready && !Model.DoseCommitted;

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
            else if (latteActive || !professionalMode || (!ShowDoseButton && !Brew.Complete) ||
                !EspressoActionButton.Contains(StationGuiPointer()))
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
            if (smoke || latteActive || CoffeeBeanSelectionOpen) return false;

            bool click = Input.GetMouseButtonDown(0);
            Vector2 pointer = StationGuiPointer();
            if (Input.GetKeyDown(KeyCode.D) || (click && DifficultyButton.Contains(pointer)))
            {
                OpenDifficultyMenu();
                return true;
            }
            if (!professionalMode || paused) return false;
            if (Brew.Complete && click && EspressoActionButton.Contains(pointer))
            {
                OpenShotReport();
                return true;
            }
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
            if (depth < .85f)
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
            tampTiltIntegral = tampFirmSeconds = 0;
        }

        private void ResetProfessionalShot()
        {
            if (professionalMenuOpen || professionalReportOpen) CloseProfessionalOverlay();
            professionalShot = null;
            professionalReportShown = false;
            tampTiltIntegral = tampFirmSeconds = 0;
            professionalPuckUniformity = 1;
        }

        private void PrepareProfessionalBrew()
        {
            if (!professionalMode || professionalShot != null || !Brew.Locked || !Brew.CupDocked) return;
            var recipe = new ProfessionalEspressoModel(Model.Grounds, professionalGrind,
                professionalPuckUniformity, professionalTemperature, professionalPressure);
            if (Brew.ConfigureProfessional(recipe)) professionalShot = recipe;
        }

        private void StepStationBrew(float dt)
        {
            Brew.Step(dt);
            if (professionalMode && !professionalReportShown && Brew.Complete)
            {
                professionalReportShown = true;
                OpenShotReport();
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
            if (!professionalMode) return;

            BeanPanel(new Rect(18, 52, 289, 64), new Color(.07f, .09f, .14f, .94f));
            GUI.Label(new Rect(29, 57, 272, 24), brewView ? "추출 목표  36g · 25–30초" : "도징 목표  18g · 범위 12–22g",
                BeanText(15, EspressoAccent, true));
            GUI.Label(new Rect(29, 84, 272, 23),
                $"분쇄도 {professionalGrind}/9  ·  {professionalTemperature:0.0}°C  ·  {professionalPressure:0.0} bar",
                BeanText(13, EspressoInk));
            if (Brew.Complete)
                GUI.Button(EspressoActionButton, "샷 리포트", BeanActionStyle());
            else if (ShowDoseButton)
                GUI.Button(EspressoActionButton, "도징 확정 [Enter]", BeanActionStyle());
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
                BeanPanel(new Rect(208, 44, 544, 452), new Color(.10f, .12f, .16f));
                if (professionalMenuOpen) DrawDifficultyMenu();
                else DrawShotReport();
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
                ? "도징부터 추출 정지까지 직접 조절하고 샷 리포트를 확인합니다."
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
                GUI.Label(new Rect(234, 396, 490, 26), "연습 레시피  18g → 36g · 25–30초", BeanText(14, EspressoAccent, true));
            }
            if (GUI.Button(new Rect(474, 440, 109, 34), "취소 [Esc]", BeanActionStyle()))
                CloseProfessionalOverlay();
            if (GUI.Button(new Rect(594, 440, 130, 34), "선택 적용", BeanActionStyle()) &&
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
            GUI.Label(new Rect(234, 67, 480, 37), "프로페셔널 샷 리포트", BeanText(25, EspressoInk, true));
            GUI.Label(new Rect(234, 117, 480, 38), $"레시피 점수  {result.Score:0} / 100", BeanText(24, EspressoAccent, true));
            GUI.Label(new Rect(234, 167, 480, 27),
                $"원두 {result.DoseGrams:0.0}g → 음료 {result.BeverageGrams:0.0}g   ·   1 : {result.BrewRatio:0.00}",
                BeanText(17, EspressoInk));
            GUI.Label(new Rect(234, 203, 480, 27),
                $"추출 {result.ElapsedSeconds:0.0}초   ·   {professionalTemperature:0.0}°C   ·   {professionalPressure:0.0} bar",
                BeanText(17, EspressoInk));
            GUI.Label(new Rect(234, 239, 480, 27),
                $"예상 농도 (TDS) {result.TdsPercent:0.0}%   ·   예상 추출 수율 (EY) {result.ExtractionYieldPercent:0.0}%",
                BeanText(16, EspressoInk));
            GUI.Label(new Rect(234, 275, 480, 25),
                $"채널링 위험 {result.ChannelingRisk:P0}  ·  물이 퍽의 일부로 몰릴 가능성",
                BeanText(14, EspressoMuted));
            BeanPanel(new Rect(234, 319, 490, 74), new Color(.18f, .20f, .24f));
            GUI.Label(new Rect(246, 328, 467, 56), ProfessionalFeedback(result.Feedback), BeanText(15, EspressoInk, false, true));

            if (GUI.Button(new Rect(234, 440, 126, 34), "닫기 [Esc]", BeanActionStyle()))
                CloseProfessionalOverlay();
            if (GUI.Button(new Rect(441, 440, 119, 34), "새 샷 [R]", BeanActionStyle()))
            {
                CloseProfessionalOverlay();
                ResetStation();
            }
            if (GUI.Button(new Rect(572, 440, 152, 34), "라떼로 이어가기", BeanActionStyle()))
            {
                CloseProfessionalOverlay();
                BeginLatte();
            }
        }

        private static string ProfessionalFeedback(ProfessionalEspressoFeedback feedback)
        {
            switch (feedback)
            {
                case ProfessionalEspressoFeedback.UnevenPuck: return "물이 퍽의 일부로 몰렸어요. 가루를 고르게 펴고 수직으로 눌러 보세요. 탬핑이 수평이었다면 분쇄를 한 단계 굵게 조정해 보세요.";
                case ProfessionalEspressoFeedback.DoseOffTarget: return "이번 연습 레시피는 18g을 기준으로 해요. 분쇄를 멈출 때 저울을 확인하세요.";
                case ProfessionalEspressoFeedback.RatioTooShort: return "원두 양에 비해 음료가 적어요. 저울이 목표 무게에 가까워질 때까지 추출해 보세요.";
                case ProfessionalEspressoFeedback.RatioTooLong: return "원두 양에 비해 음료가 많아요. 목표 무게에 가까워지면 추출을 멈춰 보세요.";
                case ProfessionalEspressoFeedback.UnderExtracted: return "예상 추출 수율이 낮아요. 흐름이 빠르면 분쇄도를 조금 더 곱게 조정해 보세요.";
                case ProfessionalEspressoFeedback.OverExtracted: return "예상 추출 수율이 높아요. 분쇄를 조금 더 굵게 하거나 음료 양을 줄여 보세요.";
                case ProfessionalEspressoFeedback.FlowTooFast: return "목표 무게에 빠르게 도달했어요. 다음 잔에서 분쇄를 조금 더 곱게 조정해 보세요.";
                case ProfessionalEspressoFeedback.FlowTooSlow: return "추출이 느렸어요. 다음 잔에서 분쇄를 조금 더 굵게 조정해 보세요.";
                case ProfessionalEspressoFeedback.OnTarget: return "이번 잔은 연습 레시피 목표에 가까워요.";
                default: return "저울을 확인하고 목표량에 맞춰 추출해 보세요.";
            }
        }
    }
}
