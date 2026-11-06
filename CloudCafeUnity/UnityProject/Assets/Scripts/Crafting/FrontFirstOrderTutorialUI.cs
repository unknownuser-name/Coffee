using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Color TutorialPaper = new Color(.055f, .075f, .105f, .98f);
        private static readonly Color TutorialCompleteInk = new Color(.57f, .91f, .70f);

        private void DrawFirstOrderTutorial()
        {
            if (!TutorialVisible || paused || professionalMenuOpen || CoffeeBeanSelectionOpen ||
                espressoOrderMenuOpen || frame.width <= 0f || frame.height <= 0f) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                Vector3.one * frame.width / 960f);
            GUI.color = Color.white;
            GUI.enabled = true;
            try
            {
                if (professionalReportOpen) DrawTutorialReportStrip();
                else if (counterView) DrawTutorialCounterCard();
                else DrawTutorialWorkshopCard();
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
            }
        }

        private void DrawTutorialWorkshopCard()
        {
            BeanPanel(new Rect(18, 176, 266, 283), TutorialPaper);
            BeanPanel(new Rect(18, 176, 4, 283), EspressoAccent);
            GUI.Label(new Rect(31, 186, 240, 25), "첫 손님 튜토리얼", BeanText(17, EspressoAccent, true));
            GUI.Label(new Rect(31, 215, 240, 22), TutorialCountLabel, BeanText(13, EspressoMuted, true));
            DrawTutorialProgress(new Rect(31, 244, 240, 7));
            GUI.Label(new Rect(31, 265, 240, 42), TutorialTitle, BeanText(17, EspressoInk, true, true));
            GUI.Label(new Rect(31, 315, 240, 81), TutorialInstruction, BeanText(14, EspressoInk, false, true));
            if (!string.IsNullOrEmpty(TutorialSuccessNote))
                GUI.Label(new Rect(31, 405, 240, 44), TutorialSuccessNote,
                    BeanText(12, TutorialCompleteInk, true, true));
        }

        private void DrawTutorialCounterCard()
        {
            BeanPanel(new Rect(18, 337, 316, 112), TutorialPaper);
            BeanPanel(new Rect(18, 337, 4, 112), EspressoAccent);
            GUI.Label(new Rect(30, 344, 206, 23), "첫 손님 튜토리얼", BeanText(15, EspressoAccent, true));
            var countStyle = BeanText(12, EspressoMuted, true);
            countStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(239, 345, 82, 21), TutorialCountLabel, countStyle);
            DrawTutorialProgress(new Rect(30, 373, 292, 5));
            GUI.Label(new Rect(30, 384, 292, 20), TutorialTitle, BeanText(14, EspressoInk, true));
            GUI.Label(new Rect(30, 405, 292, 40), TutorialInstruction, BeanText(12, EspressoInk, false, true));
        }

        private void DrawTutorialReportStrip()
        {
            BeanPanel(new Rect(134, 5, 692, 26), TutorialPaper);
            BeanPanel(new Rect(134, 5, 3, 26), EspressoAccent);
            GUI.Label(new Rect(145, 8, 91, 20), TutorialCountLabel, BeanText(12, EspressoAccent, true));
            string instruction = (TutorialInstruction ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            GUI.Label(new Rect(241, 8, 572, 20), instruction, BeanText(13, EspressoInk, true));
        }

        private void DrawTutorialProgress(Rect rect)
        {
            BeanPanel(rect, new Color(.25f, .29f, .34f));
            BeanPanel(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(TutorialProgress), rect.height),
                EspressoAccent);
        }
    }
}
