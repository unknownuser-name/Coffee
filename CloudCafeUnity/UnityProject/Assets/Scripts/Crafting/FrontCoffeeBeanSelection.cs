using CloudCafe.Coffee;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Rect CoffeeBeanButton = new Rect(505, 7, 166, 28);
        private CoffeeBeanCatalog coffeeBeanCatalog;
        private CoffeeBeanDefinition selectedCoffeeBean;
        private CoffeeBeanDefinition previewCoffeeBean;
        private EspressoRoast selectedCoffeeRoast = EspressoRoast.Medium;
        private EspressoRoast previewCoffeeRoast;
        private Vector2 coffeeBeanScroll;
        private bool coffeeBeanBrowserOpen;
        private bool coffeeBeanInputReleaseGuard;
        private CursorLockMode coffeePriorCursorLock;
        private bool coffeePriorCursorVisible;

        public CoffeeBeanDefinition SelectedCoffeeBean => selectedCoffeeBean;
        public EspressoRoast SelectedCoffeeRoast => selectedCoffeeRoast;
        public bool CoffeeBeanSelectionOpen => coffeeBeanBrowserOpen;

        public bool ShowCoffeeBeanSelection()
        {
            OpenCoffeeBeanBrowser();
            return coffeeBeanBrowserOpen;
        }

        private void InitializeCoffeeBeans()
        {
            if (smoke && !professionalSmoke) return;
            coffeeBeanCatalog = CoffeeBeanCatalog.Load();
            if (coffeeBeanCatalog == null || coffeeBeanCatalog.Beans == null) return;
            selectedCoffeeBean = coffeeBeanCatalog.Find("yirgacheffe_kurume");
            if (selectedCoffeeBean == null)
            {
                foreach (CoffeeBeanDefinition bean in coffeeBeanCatalog.Beans)
                {
                    if (bean == null) continue;
                    selectedCoffeeBean = bean;
                    break;
                }
            }
            previewCoffeeBean = selectedCoffeeBean;
            ApplyRecommendedBeanRecipe();
        }

        private bool CanChangeCoffeeBeans()
        {
            return coffeeBeanCatalog != null && selectedCoffeeBean != null &&
                (!smoke || professionalSmoke) && !professionalMenuOpen && !espressoOrderMenuOpen && !customerDialogueOpen &&
                StationReadyForSettings();
        }

        private bool StationReadyForSettings()
        {
            return !latteActive && !paused && !Moving &&
                !held && !prepHeld && !prepContact && prepKind == 0 &&
                !rotating && !pressing && !cupHeld && !brewTurning &&
                !Model.Running && Model.Grounds <= .001f && Model.Flying <= .001f &&
                !Brew.Inserted && !Brew.Running && Brew.InFlight <= .001f && Brew.InCup <= .001f;
        }

        private void OpenCoffeeBeanBrowser()
        {
            if (coffeeBeanBrowserOpen || !CanChangeCoffeeBeans()) return;
            previewCoffeeBean = selectedCoffeeBean;
            previewCoffeeRoast = selectedCoffeeRoast;
            coffeePriorCursorLock = Cursor.lockState;
            coffeePriorCursorVisible = Cursor.visible;
            coffeeBeanBrowserOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            motor.volume = 0;
        }

        private void CloseCoffeeBeanBrowser()
        {
            if (!coffeeBeanBrowserOpen) return;
            coffeeBeanBrowserOpen = false;
            coffeeBeanInputReleaseGuard = true;
            Cursor.lockState = Application.isFocused ? coffeePriorCursorLock : CursorLockMode.None;
            Cursor.visible = Application.isFocused ? coffeePriorCursorVisible : true;
        }

        public bool TrySelectCoffeeBean(string id) => TrySelectCoffeeBean(id, selectedCoffeeRoast);

        public bool TrySelectCoffeeBean(string id, EspressoRoast roast)
        {
            if (!CanChangeCoffeeBeans() || !Enum.IsDefined(typeof(EspressoRoast), roast)) return false;
            CoffeeBeanDefinition bean = coffeeBeanCatalog.Find(id);
            if (bean == null) return false;
            bool changed = selectedCoffeeBean != bean || selectedCoffeeRoast != roast;
            selectedCoffeeBean = previewCoffeeBean = bean;
            selectedCoffeeRoast = previewCoffeeRoast = roast;
            if (changed) ApplyRecommendedBeanRecipe();
            string selection = $"{OriginName(bean)} · {EspressoBeanProfiles.RoastNameKo(roast)}";
            message = professionalMode && changed
                ? selection + "의 추천값을 적용했어요. D에서 조정할 수 있어요."
                : selection + " 선택. 손잡이를 잡아 분쇄를 시작하세요.";
            CloseCoffeeBeanBrowser();
            return true;
        }

        private bool HandleCoffeeBeanInput()
        {
            if (smoke || latteActive || AmericanoView || espressoOrderMenuOpen) return false;
            if (coffeeBeanInputReleaseGuard)
            {
                if (!Input.GetMouseButton(0)) coffeeBeanInputReleaseGuard = false;
                return true;
            }
            if (coffeeBeanBrowserOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Escape))
                    CloseCoffeeBeanBrowser();
                return true;
            }
            if (selectedCoffeeBean == null) return false;
            bool keyboard = Input.GetKeyDown(KeyCode.B);
            bool button = Input.GetMouseButtonDown(0) && CoffeeBeanButton.Contains(StationGuiPointer());
            if (!keyboard && !button) return false;
            if (CanChangeCoffeeBeans()) OpenCoffeeBeanBrowser();
            else message = "원두와 로스팅은 분쇄 전에 고를 수 있어요. R로 새 잔을 준비하세요.";
            // Consume the GUI button's mouse-down before the world's raycast runs.
            return true;
        }

        private bool UpdateCoffeeBeanCursor()
        {
            if (!coffeeBeanBrowserOpen) return false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (handCursor != null) handCursor.Set(HandPose.Open, Vector2.zero, false);
            return true;
        }

        private static string OriginName(CoffeeBeanDefinition bean)
        {
            if (bean == null) return string.Empty;
            return string.IsNullOrEmpty(bean.RegionKo) ? bean.Region : bean.RegionKo;
        }

        private GUIStyle BeanText(int size, Color color, bool bold = false, bool wrap = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = wrap,
                normal = { textColor = color }
            };
        }

        private GUIStyle BeanActionStyle()
        {
            return new GUIStyle(GUI.skin.button) { font = font, fontSize = 13 };
        }

        private static void BeanPanel(Rect rectangle, Color color)
        {
            Color prior = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
            GUI.color = prior;
        }

        private void DrawCoffeeBeanBadge()
        {
            if (smoke || latteActive || selectedCoffeeBean == null) return;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            try
            {
                GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                    Vector3.one * frame.width / 960f);
                GUI.color = Color.white;
                var style = new GUIStyle(GUI.skin.button)
                {
                    font = font,
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.enabled = CanChangeCoffeeBeans();
                string origin = OriginName(selectedCoffeeBean);
                if (origin.Length > 6) origin = origin.Substring(0, 5) + "…";
                if (GUI.Button(CoffeeBeanButton,
                    new GUIContent($"{origin} · {EspressoBeanProfiles.RoastNameKo(selectedCoffeeRoast)} [B]",
                        "빈 바스켓에서 원두와 로스팅 고르기"), style))
                    OpenCoffeeBeanBrowser();
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                GUI.enabled = oldEnabled;
            }
        }

        private bool DrawCoffeeBeanBrowser()
        {
            if (!coffeeBeanBrowserOpen) return false;
            GUI.DrawTexture(frame, art.Pixels, ScaleMode.StretchToFill, false);
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            try
            {
                GUI.matrix = Matrix4x4.TRS(new Vector3(frame.x, frame.y), Quaternion.identity,
                    Vector3.one * frame.width / 960f);
                GUI.color = Color.white;
                GUI.enabled = true;
                Color ink = new Color(.22f, .20f, .17f);
                Color quiet = new Color(.48f, .43f, .35f);
                Color accent = new Color(.30f, .45f, .36f);
                BeanPanel(new Rect(0, 0, 960, 540), new Color(.035f, .04f, .05f, .82f));
                BeanPanel(new Rect(18, 20, 924, 500), new Color(.95f, .92f, .86f));
                GUI.Label(new Rect(38, 35, 550, 30), "원두와 로스팅 · " + coffeeBeanCatalog.Beans.Length + "종",
                    BeanText(22, ink, true));
                GUI.Label(new Rect(39, 68, 700, 22), "원두를 고르고 이번 잔에 쓸 로스팅을 선택하세요.", BeanText(13, quiet));
                if (GUI.Button(new Rect(877, 34, 44, 30), "닫기", BeanActionStyle())) CloseCoffeeBeanBrowser();

                DrawCoffeeBeanGrid(ink, quiet, accent);
                DrawCoffeeBeanDetails(ink, quiet, accent);
                GUI.Label(new Rect(38, 480, 505, 24), "B / Esc 닫기 · 마우스 휠로 원두 목록 보기", BeanText(12, quiet));
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                GUI.enabled = oldEnabled;
            }
            return true;
        }

        private void DrawCoffeeBeanGrid(Color ink, Color quiet, Color accent)
        {
            const int columns = 3;
            const float cellWidth = 153;
            const float cellHeight = 177;
            int rows = (coffeeBeanCatalog.Beans.Length + columns - 1) / columns;
            Rect viewport = new Rect(38, 99, 506, 371);
            Rect content = new Rect(0, 0, 483, rows * cellHeight);
            coffeeBeanScroll = GUI.BeginScrollView(viewport, coffeeBeanScroll, content);
            try
            {
                for (int i = 0; i < coffeeBeanCatalog.Beans.Length; i++)
                {
                    CoffeeBeanDefinition bean = coffeeBeanCatalog.Beans[i];
                    if (bean == null) continue;
                    Rect cell = new Rect(i % columns * 161, i / columns * cellHeight, cellWidth, cellHeight - 8);
                    bool active = previewCoffeeBean == bean;
                    BeanPanel(cell, active ? new Color(.79f, .86f, .77f) : new Color(.99f, .98f, .95f));
                    if (GUI.Button(cell, GUIContent.none, GUIStyle.none)) previewCoffeeBean = bean;
                    if (bean.BagTexture != null)
                        GUI.DrawTexture(new Rect(cell.x + 9, cell.y + 5, cell.width - 18, 112),
                            bean.BagTexture, ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(cell.x + 8, cell.y + 118, cell.width - 16, 23), OriginName(bean),
                        BeanText(13, ink, true));
                    string country = string.IsNullOrEmpty(bean.CountryKo) ? bean.Country : bean.CountryKo;
                    GUI.Label(new Rect(cell.x + 8, cell.y + 142, cell.width - 16, 20), country,
                        BeanText(11, quiet));
                    if (selectedCoffeeBean == bean)
                        GUI.Label(new Rect(cell.x + cell.width - 40, cell.y + 6, 34, 20), "선택", BeanText(11, accent, true));
                }
            }
            finally { GUI.EndScrollView(); }
        }

        private void DrawCoffeeBeanDetails(Color ink, Color quiet, Color accent)
        {
            CoffeeBeanDefinition bean = previewCoffeeBean;
            if (bean == null) return;
            string country = string.IsNullOrEmpty(bean.CountryKo) ? bean.Country : bean.CountryKo;
            GUI.Label(new Rect(565, 99, 350, 22), country + " / " + bean.Country, BeanText(13, quiet));
            GUI.Label(new Rect(565, 125, 350, 38), OriginName(bean), BeanText(23, ink, true));
            GUI.Label(new Rect(565, 162, 350, 23), bean.Variety + " · " + bean.VarietyKo, BeanText(13, quiet));
            BeanPanel(new Rect(565, 195, 160, 190), new Color(.99f, .98f, .95f));
            if (bean.BagTexture != null)
                GUI.DrawTexture(new Rect(569, 199, 152, 182), bean.BagTexture, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(741, 199, 172, 23), "봉투의 향미", BeanText(15, ink, true));
            GUI.Label(new Rect(741, 226, 172, 54), string.Join(" · ", bean.TastingNotes ?? new string[0]),
                BeanText(14, ink, false, true));
            DrawCoffeeTaste(new Rect(741, 281, 172, 23), "산미", bean.Acidity, ink, accent);
            DrawCoffeeTaste(new Rect(741, 307, 172, 23), "단맛", bean.Sweetness, ink, accent);
            DrawCoffeeTaste(new Rect(741, 333, 172, 23), "바디", bean.Body, ink, accent);

            GUI.Label(new Rect(565, 389, 70, 26), "로스팅", BeanText(13, ink, true));
            DrawRoastOption(EspressoRoast.Light, 637);
            DrawRoastOption(EspressoRoast.Medium, 732);
            DrawRoastOption(EspressoRoast.Dark, 827);
            EspressoBeanProfile profile = EspressoBeanProfiles.ForBean(bean.Id, previewCoffeeRoast);
            GUI.Label(new Rect(565, 420, 350, 20),
                $"프로 추천  {profile.GrindSetting}/9 · {profile.TemperatureCelsius:0.0}°C · 1:{profile.BrewRatio:0.00}",
                BeanText(12, accent, true));
            GUI.Label(new Rect(565, 443, 350, 34), bean.Description, BeanText(11, quiet, false, true));
            bool oldEnabled = GUI.enabled;
            GUI.enabled = CanChangeCoffeeBeans();
            if (GUI.Button(new Rect(738, 480, 180, 27), "선택한 조합으로 준비", BeanActionStyle()))
                TrySelectCoffeeBean(bean.Id, previewCoffeeRoast);
            GUI.enabled = oldEnabled;
            if (GUI.Button(new Rect(565, 480, 155, 27), "돌아가기", BeanActionStyle())) CloseCoffeeBeanBrowser();
        }

        private void DrawRoastOption(EspressoRoast roast, float x)
        {
            string label = EspressoBeanProfiles.RoastNameKo(roast);
            if (previewCoffeeRoast == roast) label += " ✓";
            if (GUI.Button(new Rect(x, 390, 90, 25), label, BeanActionStyle()))
                previewCoffeeRoast = roast;
        }

        private void DrawCoffeeTaste(Rect row, string name, int value, Color ink, Color accent)
        {
            value = Mathf.Clamp(value, 1, 5);
            GUI.Label(new Rect(row.x, row.y, 46, row.height), name, BeanText(12, ink));
            for (int segment = 0; segment < 5; segment++)
                BeanPanel(new Rect(row.x + 46 + segment * 17, row.y + 6, 13, 8),
                    segment < value ? accent : new Color(.81f, .79f, .72f));
            GUI.Label(new Rect(row.x + 138, row.y, 35, row.height), value + "/5", BeanText(11, ink));
        }

        private string coffeeBeanSmokeDirectory;
        private float coffeeBeanScreenshotNonblackFraction;

        private bool CoffeeBeanSmokeCheck(bool condition, string reason)
        {
            if (condition) return true;
            string safeReason = reason.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
            File.WriteAllText(Path.Combine(coffeeBeanSmokeDirectory, "selection-validation.json"),
                "{\"passed\":false,\"reason\":\"" + safeReason + "\"}");
            Debug.LogError("COFFEE_BEAN_SELECTION_FAILED " + reason);
            Application.Quit(2);
            return false;
        }

        private void CoffeeBeanSmokeException(string text, string stack, LogType type)
        {
            if (type == LogType.Exception) CoffeeBeanSmokeCheck(false, text + " " + stack);
        }

        private bool CaptureCoffeeBeanSelection()
        {
            Texture2D screenshot = null;
            try
            {
                // Capture the final game view, including OnGUI. art.Pixels alone
                // contains only the scene camera and would omit the bean picker.
                screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                if (screenshot == null || screenshot.width < 480 || screenshot.height < 270)
                    return CoffeeBeanSmokeCheck(false, "game-view screenshot is unavailable");
                File.WriteAllBytes(Path.Combine(coffeeBeanSmokeDirectory, "selection.png"), screenshot.EncodeToPNG());
                Color32[] pixels = screenshot.GetPixels32();
                int samples = 0;
                int nonblack = 0;
                int minimum = 255;
                int maximum = 0;
                for (int i = 0; i < pixels.Length; i += 100)
                {
                    Color32 pixel = pixels[i];
                    int brightness = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                    minimum = Mathf.Min(minimum, brightness);
                    maximum = Mathf.Max(maximum, brightness);
                    if (brightness > 16) nonblack++;
                    samples++;
                }
                coffeeBeanScreenshotNonblackFraction = samples == 0 ? 0f : (float)nonblack / samples;
                if (!CoffeeBeanSmokeCheck(coffeeBeanScreenshotNonblackFraction >= .05f && maximum - minimum >= 24,
                    "game-view screenshot is blank: nonblack fraction " + coffeeBeanScreenshotNonblackFraction +
                    ", brightness range " + minimum + ".." + maximum)) return false;
                return true;
            }
            catch (Exception exception)
            {
                return CoffeeBeanSmokeCheck(false, "screenshot: " + exception.Message);
            }
            finally
            {
                if (screenshot != null) Destroy(screenshot);
            }
        }

        private IEnumerator CoffeeBeanSmoke()
        {
            bool skipCapture = Array.IndexOf(Environment.GetCommandLineArgs(), "-coffeeBeanSkipCapture") >= 0;
            coffeeBeanSmokeDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Review/CoffeeAssets"));
            Directory.CreateDirectory(coffeeBeanSmokeDirectory);
            Application.logMessageReceived += CoffeeBeanSmokeException;
            float splashDeadline = Time.realtimeSinceStartup + 10f;
            while (!UnityEngine.Rendering.SplashScreen.isFinished && Time.realtimeSinceStartup < splashDeadline)
                yield return null;
            if (!CoffeeBeanSmokeCheck(UnityEngine.Rendering.SplashScreen.isFinished, "Unity splash screen finishes before capture")) yield break;
            yield return new WaitForSecondsRealtime(.3f);
            for (int warmup = 0; warmup < 3; warmup++) yield return null;
            yield return null;
            if (!CoffeeBeanSmokeCheck(coffeeBeanCatalog != null && coffeeBeanCatalog.Beans != null &&
                coffeeBeanCatalog.Beans.Length == 18, "18 registered beans load through Resources")) yield break;
            var ids = new HashSet<string>();
            foreach (CoffeeBeanDefinition bean in coffeeBeanCatalog.Beans)
                if (!CoffeeBeanSmokeCheck(bean != null && !string.IsNullOrEmpty(bean.Id) && ids.Add(bean.Id) &&
                    bean.BagSprite != null && bean.BagTexture != null, "unique bean IDs and real sprite references")) yield break;
            if (!CoffeeBeanSmokeCheck(selectedCoffeeBean != null && selectedCoffeeBean.Id == "yirgacheffe_kurume",
                "default origin is Yirgacheffe")) yield break;
            CursorLockMode beforeLock = Cursor.lockState;
            bool beforeVisible = Cursor.visible;
            if (!CoffeeBeanSmokeCheck(ShowCoffeeBeanSelection(), "empty-basket selector opens")) yield break;
            yield return null;
            yield return new WaitForEndOfFrame();
            if (!CoffeeBeanSmokeCheck(coffeeBeanBrowserOpen && Cursor.lockState == CursorLockMode.None && Cursor.visible &&
                (handCursor == null || !handCursor.Visible), "browser uses a visible OS cursor without the hand cursor")) yield break;
            if (!skipCapture && !CaptureCoffeeBeanSelection()) yield break;
            if (!CoffeeBeanSmokeCheck(TrySelectCoffeeBean("caturra") && selectedCoffeeBean.Id == "caturra" &&
                !coffeeBeanBrowserOpen, "Antioquia selection applies and closes the browser")) yield break;
            CursorLockMode expectedLock = Application.isFocused ? beforeLock : CursorLockMode.None;
            bool expectedVisible = Application.isFocused ? beforeVisible : true;
            if (!CoffeeBeanSmokeCheck(Cursor.lockState == expectedLock && Cursor.visible == expectedVisible,
                "prior cursor mode is restored")) yield break;
            yield return null;
            if (!CoffeeBeanSmokeCheck(ShowCoffeeBeanSelection() && TrySelectCoffeeBean("kilimanjaro_bourbon") &&
                selectedCoffeeBean.Id == "kilimanjaro_bourbon", "Kilimanjaro selection applies")) yield break;
            if (!CoffeeBeanSmokeCheck(!TrySelectCoffeeBean("missing-bean") && selectedCoffeeBean.Id == "kilimanjaro_bourbon",
                "invalid product does not change selection")) yield break;
            Model.Grounds = 1f;
            bool blocked = !ShowCoffeeBeanSelection() && !TrySelectCoffeeBean("yirgacheffe_kurume") &&
                selectedCoffeeBean.Id == "kilimanjaro_bourbon";
            Model.Grounds = 0f;
            if (!CoffeeBeanSmokeCheck(blocked, "mid-dose bean changes are blocked")) yield break;
            string captureReport = skipCapture
                ? "\"screenshot\":null,\"screenshot_verified_nonblank\":false,\"visual_capture_executed\":false,\"screenshot_nonblack_fraction\":null,\"visual_capture_reason\":\"capture skipped for hidden-window environment\",\"render\":null,\"scope\":\"scripted UI API only; visual capture skipped; not OS mouse input\""
                : "\"screenshot\":\"selection.png\",\"screenshot_verified_nonblank\":true,\"visual_capture_executed\":true,\"screenshot_nonblack_fraction\":" +
                    coffeeBeanScreenshotNonblackFraction.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"visual_capture_reason\":\"captured and checked nonblank\",\"render\":\"actual game view including OnGUI\",\"scope\":\"scripted UI API and nonblank game-view capture; not OS mouse input\"";
            File.WriteAllText(Path.Combine(coffeeBeanSmokeDirectory, "selection-validation.json"),
                "{\"passed\":true,\"api_checks_passed\":true,\"registeredBeans\":18,\"defaultId\":\"yirgacheffe_kurume\",\"selectionId\":\"kilimanjaro_bourbon\",\"realSpriteReferences\":true,\"cursorRestore\":true,\"emptyBasketGate\":true," +
                captureReport + "}");
            Application.logMessageReceived -= CoffeeBeanSmokeException;
            Debug.Log("COFFEE_BEAN_SELECTION_PASSED");
            Application.Quit(0);
        }
    }
}
