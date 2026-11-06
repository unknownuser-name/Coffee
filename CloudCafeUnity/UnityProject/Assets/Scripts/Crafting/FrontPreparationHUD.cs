using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Color PrepSuccess = new Color(.53f, .89f, .66f);
        private static readonly Color PrepWarning = new Color(1f, .77f, .35f);
        private static readonly Color PrepTrack = new Color(.24f, .28f, .33f);

        private bool HasGrinderAction => !brewView && Model.Slot == CraftSlot.Grinder && !Model.DoseCommitted;
        private bool HasBrewAction => Brew.Inserted && !Brew.Complete;
        private bool GrinderActionEnabled => !Moving && !held && (Model.Running ||
            (Model.Beans > 0 && Model.DoseInBasketOrFlight <
                (professionalMode ? ProfessionalEspressoModel.MaxDoseGrams : 18f) - .01f));
        private bool BrewActionEnabled => !Moving && !cupHeld && !brewTurning && Brew.Locked &&
            Brew.CupDocked && !Brew.ShotFinalized;

        private bool HandleWorkshopAction()
        {
            if (HasBrewAction)
            {
                if (BrewActionEnabled) SwitchBrew();
                return true;
            }
            if (HasGrinderAction)
            {
                if (GrinderActionEnabled) Switch();
                return true;
            }
            return false;
        }

        private void DrawWorkshopAction()
        {
            string text;
            bool enabled;
            if (Brew.Complete)
            {
                text = finishedDrink.HasValue ? "음료 리포트 [Enter]" : "음료 선택 [Enter]";
                enabled = true;
            }
            else if (HasBrewAction)
            {
                text = Brew.Running ? "추출 멈추기 [Space]" : "추출 시작 [Space]";
                enabled = BrewActionEnabled;
            }
            else if (professionalMode && ShowDoseButton)
            {
                text = "도징 확정 [Enter]";
                enabled = true;
            }
            else if (HasGrinderAction)
            {
                enabled = GrinderActionEnabled;
                text = Model.Running ? "분쇄 멈추기 [Space]" :
                    enabled ? "분쇄 시작 [Space]" : "분쇄 완료 · 매트로 옮기기";
            }
            else return;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && enabled && !paused;
            GUI.Button(EspressoActionButton, text, BeanActionStyle());
            GUI.enabled = wasEnabled;
        }

        private void DrawPreparationHUD()
        {
            if (TutorialVisible) return;
            if (Brew.Inserted) return;
            const float left = 18;
            const float top = 265;
            BeanPanel(new Rect(left, top, 266, 199), new Color(.055f, .075f, .105f, .96f));
            BeanPanel(new Rect(left, top, 4, 199), Prep.Complete ? PrepSuccess : EspressoAccent);
            string phase = Prep.Complete ? "준비 완료" : Prep.Leveled ? "3 / 3  탬핑" :
                DoseReadyForPreparation ? Model.Slot == CraftSlot.Mat ? "2 / 3  표면 정리" : "2 / 3  매트로 이동"
                    : "1 / 3  분쇄·계량";
            GUI.Label(new Rect(31, 273, 240, 28), phase,
                BeanText(18, Prep.Complete ? PrepSuccess : EspressoInk, true));

            if (Prep.Complete)
            {
                GUI.Label(new Rect(31, 310, 240, 30), "✓ 분쇄·계량 완료", BeanText(15, PrepSuccess, true));
                GUI.Label(new Rect(31, 342, 240, 30), "✓ 표면 정리 완료", BeanText(15, PrepSuccess, true));
                GUI.Label(new Rect(31, 374, 240, 30), "✓ 탬핑 완료", BeanText(15, PrepSuccess, true));
                string next = professionalMode && professionalPuckUniformity < .7f
                    ? "수평이 조금 흔들렸어요.\n포터필터를 머신으로 옮기세요."
                    : "포터필터를 들어\n오른쪽 머신에 끼워 주세요.";
                GUI.Label(new Rect(31, 414, 240, 43), next, BeanText(13, EspressoInk, false, true));
                return;
            }
            if (!DoseReadyForPreparation)
            {
                GUI.Label(new Rect(31, 307, 240, 29), $"바스켓  {Model.Grounds:0.0}g / 권장 18g",
                    BeanText(15, EspressoInk, true));
                DrawPrepMeter(new Rect(31, 340, 239, 12), Model.Grounds / 18f, EspressoAccent);
                GUI.Label(new Rect(31, 363, 240, 24), $"떨어지는 가루  {Model.Flying:0.0}g", BeanText(13, EspressoMuted));
                string instruction = professionalMode
                    ? Model.Ready ? "매트에 놓고 [도징 확정]을\n누르면 표면 정리로 넘어갑니다."
                        : "금색 버튼 / Space로 분쇄를 멈추고\n가루가 다 떨어질 때까지 기다려요."
                    : "18g이 모이면 손잡이를 잡아\n오른쪽 매트에 내려놓으세요.";
                GUI.Label(new Rect(31, 401, 240, 52), instruction, BeanText(13, EspressoInk, false, true));
                return;
            }
            if (!Prep.Leveled)
            {
                GUI.Label(new Rect(31, 307, 240, 43),
                    Model.Slot != CraftSlot.Mat ? "바스켓을 먼저\n작업 매트에 내려놓으세요." :
                    prepKind == 1 && prepContact ? "도구를 누른 채\n원을 그리듯 돌려 주세요." :
                        "낮고 둥근 정리 도구를\n바스켓 위에 얹어 주세요.",
                    BeanText(14, EspressoInk, false, true));
                GUI.Label(new Rect(31, 358, 240, 26), $"고르게 펴기  {Prep.LevelProgress:P0}",
                    BeanText(16, EspressoAccent, true));
                DrawPrepMeter(new Rect(31, 391, 239, 12), Prep.LevelProgress, EspressoAccent);
                GUI.Label(new Rect(31, 421, 240, 32), "가득 채우면 탬퍼를 사용할 수 있어요.",
                    BeanText(12, EspressoMuted, false, true));
                return;
            }

            string tampHint = prepKind != 2 || !prepContact ? "탬퍼를 바스켓 위에 얹으세요." :
                !pressing ? "탬퍼를 잡고 아래로 당겨 주세요." :
                Prep.Depth < PuckPrepModel.RequiredTampDepth ? "조금 더 아래로 눌러 주세요." :
                Prep.TampHoldProgress < 1f ? "지금 깊이로 잠깐 유지하세요." : "충분해요! 손을 놓으세요.";
            Color state = Prep.TampHoldProgress >= 1f ? PrepSuccess : PrepWarning;
            GUI.Label(new Rect(31, 305, 240, 36), tampHint, BeanText(14, state, true, true));
            GUI.Label(new Rect(31, 346, 240, 22),
                $"눌림 깊이  {Prep.Depth:P0}  ·  목표 85%", BeanText(13, EspressoInk, true));
            DrawPrepMeter(new Rect(31, 373, 239, 11), Prep.Depth,
                Prep.Depth >= PuckPrepModel.RequiredTampDepth ? PrepSuccess : EspressoAccent);
            BeanPanel(new Rect(31 + 239 * PuckPrepModel.RequiredTampDepth, 370, 2, 17), Color.white);
            GUI.Label(new Rect(31, 393, 240, 22),
                $"유지 시간  {Mathf.Min(Prep.FirmTime, PuckPrepModel.RequiredFirmSeconds):0.00} / 0.25초",
                BeanText(13, EspressoInk, true));
            DrawPrepMeter(new Rect(31, 420, 239, 11), Prep.TampHoldProgress, PrepSuccess);
            string balance = professionalMode && pressing
                ? professionalLiveTampTilt > .3f ? "수평 주의 · 옆으로 밀리지 않게 눌러요." : "수평 안정 · 이대로 유지하세요."
                : "깊이와 유지 시간을 채운 뒤 손을 놓아요.";
            GUI.Label(new Rect(31, 440, 240, 20), balance, BeanText(11,
                professionalMode && pressing && professionalLiveTampTilt > .3f ? PrepWarning : EspressoMuted));
        }

        private bool DoseReadyForPreparation => professionalMode ? Model.DoseCommitted : Model.Ready;

        private static void DrawPrepMeter(Rect rect, float progress, Color fill)
        {
            BeanPanel(rect, PrepTrack);
            BeanPanel(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(progress), rect.height), fill);
        }

        private string CustomerTimeRemaining()
        {
            int seconds = Mathf.CeilToInt(customerVisit.PatienceRemainingSeconds);
            return $"{seconds / 60}:{seconds % 60:00}";
        }
    }
}
