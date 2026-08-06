using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PocketCity
{
    /// <summary>
    /// HUD와 건설 팔레트를 전부 코드로 만든다. 프리팹이나 씬 배선이 없으므로
    /// 프로젝트를 열고 Play만 누르면 화면이 완성된다.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private CitySim _sim;
        private CityView _view;

        public string SelectedTool { get; private set; }
        public int Speed = 1;

        private Font _font;
        private Canvas _canvas;

        private Text _moneyText, _popText, _happyText, _tierText, _dateText;
        private Image _powerFill, _waterFill, _rFill, _cFill, _iFill;
        private RectTransform _toolRow, _toastRoot;
        private string _currentCategory = "구역";

        private readonly List<Button> _categoryButtons = new List<Button>();
        private readonly List<Button> _speedButtons = new List<Button>();
        private readonly List<Button> _overlayButtons = new List<Button>();
        private readonly Dictionary<string, Button> _toolButtons = new Dictionary<string, Button>();

        private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.12f, 0.92f);
        private static readonly Color AccentColor = new Color(0.30f, 0.64f, 1f, 1f);
        private static readonly Color DimColor = new Color(0.55f, 0.60f, 0.68f, 1f);
        private static readonly Color TextColor = new Color(0.91f, 0.93f, 0.96f, 1f);

        public void Initialize(CitySim sim, CityView view)
        {
            _sim = sim;
            _view = view;
            _font = LoadFont();
            BuildCanvas();
            BuildTopBar();
            BuildPalette();
            BuildToastArea();
            SelectCategory("구역");
            RefreshStats();
        }

        private static Font LoadFont()
        {
            Font f = null;
            try
            {
                // OS 폰트를 써야 한글이 깨지지 않는다. 안드로이드는 마지막 항목으로 폴백된다.
                f = Font.CreateDynamicFontFromOSFont(
                    new string[] { "Noto Sans CJK KR", "NanumGothic", "Malgun Gothic", "Apple SD Gothic Neo", "Roboto", "Arial" },
                    40);
            }
            catch (Exception) { f = null; }

            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch (Exception) { f = null; }
            }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (Exception) { f = null; }
            }
            return f;
        }

        // ------------------------------------------------------------ 빌더 헬퍼

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image NewPanel(string name, Transform parent, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Text NewText(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, int fontSize, Color bg)
        {
            Image img = NewPanel(name, parent, bg);
            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            Text t = NewText("Label", img.transform, fontSize, TextAnchor.MiddleCenter, TextColor);
            RectTransform trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            t.text = label;
            return btn;
        }

        private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void SetButtonLabel(Button b, string text)
        {
            Text t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        private static void SetButtonActive(Button b, bool active)
        {
            Image img = b.GetComponent<Image>();
            if (img != null) img.color = active ? AccentColor : new Color(1f, 1f, 1f, 0.08f);
            Text t = b.GetComponentInChildren<Text>();
            if (t != null) t.color = active ? new Color(0.03f, 0.07f, 0.12f) : DimColor;
        }

        // ------------------------------------------------------------ 캔버스

        private void BuildCanvas()
        {
            GameObject go = new GameObject("HudCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }

        // ------------------------------------------------------------ 상단

        private void BuildTopBar()
        {
            Image bar = NewPanel("TopBar", _canvas.transform, new Color(0.06f, 0.08f, 0.11f, 0.86f));
            Stretch(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -260f), new Vector2(0f, 0f));

            // 1행: 자금 / 인구 / 행복
            float w = 1080f / 3f;
            _moneyText = MakeStat(bar.transform, "자금", 0f * w, w);
            _popText = MakeStat(bar.transform, "인구", 1f * w, w);
            _happyText = MakeStat(bar.transform, "행복", 2f * w, w);

            // 2행: 단계 · 날짜 · 속도
            _tierText = NewText("Tier", bar.transform, 30, TextAnchor.MiddleLeft, AccentColor);
            Stretch(_tierText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -168f), new Vector2(200f, -128f));

            _dateText = NewText("Date", bar.transform, 28, TextAnchor.MiddleLeft, DimColor);
            Stretch(_dateText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, -168f), new Vector2(430f, -128f));

            string[] speedLabels = { "II", ">", ">>" };
            int[] speedValues = { 0, 1, 3 };
            for (int i = 0; i < 3; i++)
            {
                int value = speedValues[i];
                Button b = NewButton("Speed" + i, bar.transform, speedLabels[i], 26, new Color(1f, 1f, 1f, 0.08f));
                Stretch(b.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-330f + i * 106f, -172f), new Vector2(-234f + i * 106f, -124f));
                b.onClick.AddListener(delegate { SetSpeed(value); });
                _speedButtons.Add(b);
            }

            // 3행: RCI + 오버레이
            _rFill = MakeDemandBar(bar.transform, "주거", 24f, new Color(0.35f, 0.78f, 0.45f));
            _cFill = MakeDemandBar(bar.transform, "상업", 138f, new Color(0.30f, 0.64f, 1f));
            _iFill = MakeDemandBar(bar.transform, "공업", 252f, new Color(0.91f, 0.70f, 0.23f));

            _powerFill = MakeSupplyBar(bar.transform, "전력", 380f, new Color(1f, 0.84f, 0.25f));
            _waterFill = MakeSupplyBar(bar.transform, "수도", 530f, new Color(0.25f, 0.74f, 1f));

            string[] overlays = { "none", "power", "water", "pollution", "land" };
            string[] overlayNames = { "일반", "전력", "수도", "오염", "지가" };
            for (int i = 0; i < overlays.Length; i++)
            {
                string mode = overlays[i];
                Button b = NewButton("Ov" + mode, bar.transform, overlayNames[i], 22, new Color(1f, 1f, 1f, 0.08f));
                Stretch(b.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-390f + i * 78f, -250f), new Vector2(-318f + i * 78f, -198f));
                b.onClick.AddListener(delegate { SetOverlay(mode); });
                _overlayButtons.Add(b);
            }
            SetOverlay("none");
            SetSpeed(1);
        }

        private Text MakeStat(Transform parent, string caption, float x, float width)
        {
            Text cap = NewText("Cap_" + caption, parent, 22, TextAnchor.LowerLeft, DimColor);
            Stretch(cap.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x + 24f, -62f), new Vector2(x + width - 12f, -30f));
            cap.text = caption;

            Text val = NewText("Val_" + caption, parent, 38, TextAnchor.UpperLeft, TextColor);
            Stretch(val.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x + 24f, -116f), new Vector2(x + width - 12f, -62f));
            val.text = "-";
            return val;
        }

        private Image MakeDemandBar(Transform parent, string caption, float x, Color color)
        {
            Text cap = NewText("D_" + caption, parent, 20, TextAnchor.MiddleLeft, DimColor);
            Stretch(cap.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, -252f), new Vector2(x + 64f, -216f));
            cap.text = caption;

            Image bg = NewPanel("DBg_" + caption, parent, new Color(1f, 1f, 1f, 0.12f));
            Stretch(bg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x + 62f, -244f), new Vector2(x + 106f, -226f));

            Image fill = NewPanel("DFill_" + caption, bg.transform, color);
            RectTransform frt = fill.rectTransform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(0f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = new Vector2(22f, 0f);
            return fill;
        }

        private Image MakeSupplyBar(Transform parent, string caption, float x, Color color)
        {
            Text cap = NewText("S_" + caption, parent, 20, TextAnchor.MiddleLeft, DimColor);
            Stretch(cap.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, -252f), new Vector2(x + 64f, -216f));
            cap.text = caption;

            Image bg = NewPanel("SBg_" + caption, parent, new Color(1f, 1f, 1f, 0.12f));
            Stretch(bg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x + 62f, -244f), new Vector2(x + 132f, -226f));

            Image fill = NewPanel("SFill_" + caption, bg.transform, color);
            RectTransform frt = fill.rectTransform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(0f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = new Vector2(70f, 0f);
            return fill;
        }

        // ------------------------------------------------------------ 하단 팔레트

        private void BuildPalette()
        {
            Image bar = NewPanel("Palette", _canvas.transform, PanelColor);
            Stretch(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 300f));

            string[] cats = { "구역", "도로", "전력·수도", "서비스", "공원", "도구" };
            float catWidth = 1080f / cats.Length;
            for (int i = 0; i < cats.Length; i++)
            {
                string cat = cats[i];
                Button b = NewButton("Cat_" + cat, bar.transform, cat, 24, new Color(1f, 1f, 1f, 0.08f));
                Stretch(b.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(i * catWidth + 4f, -76f), new Vector2((i + 1) * catWidth - 4f, -8f));
                b.onClick.AddListener(delegate { SelectCategory(cat); });
                _categoryButtons.Add(b);
            }

            _toolRow = NewRect("Tools", bar.transform);
            Stretch(_toolRow, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 12f), new Vector2(-12f, -88f));

            HorizontalLayoutGroup layout = _toolRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
        }

        private void SelectCategory(string category)
        {
            _currentCategory = category;
            for (int i = 0; i < _categoryButtons.Count; i++)
            {
                Text t = _categoryButtons[i].GetComponentInChildren<Text>();
                bool on = t != null && t.text == category;
                SetButtonActive(_categoryButtons[i], on);
            }
            RebuildTools();
        }

        private void RebuildTools()
        {
            for (int i = _toolRow.childCount - 1; i >= 0; i--)
            {
                Destroy(_toolRow.GetChild(i).gameObject);
            }
            _toolButtons.Clear();

            if (_currentCategory == "도구")
            {
                AddToolButton(Balance.BulldozeKey, "철거", Balance.BulldozeCost, 0);
                return;
            }

            IList<BuildItem> all = Balance.All;
            for (int i = 0; i < all.Count; i++)
            {
                BuildItem item = all[i];
                if (item.Category != _currentCategory) continue;
                AddToolButton(item.Key, item.Name, item.Cost, item.UnlockPop);
            }
        }

        private void AddToolButton(string key, string name, int cost, int unlockPop)
        {
            bool locked = unlockPop > 0 && _sim.Population < unlockPop;
            string label = name + "\n" + (locked ? "인구 " + unlockPop.ToString("N0") : "W " + cost.ToString("N0"));

            Button b = NewButton("Tool_" + key, _toolRow, label, 24, new Color(1f, 1f, 1f, 0.08f));
            LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 190f;
            le.preferredHeight = 150f;

            string toolKey = key;
            b.onClick.AddListener(delegate { OnToolClicked(toolKey, unlockPop); });
            _toolButtons[key] = b;
            SetButtonActive(b, SelectedTool == key);
        }

        private void OnToolClicked(string key, int unlockPop)
        {
            if (unlockPop > 0 && _sim.Population < unlockPop)
            {
                Toast("인구 " + unlockPop.ToString("N0") + "명부터 해금됩니다", "warn");
                return;
            }
            SelectTool(SelectedTool == key ? null : key);
        }

        public void SelectTool(string key)
        {
            SelectedTool = key;
            foreach (KeyValuePair<string, Button> kv in _toolButtons)
            {
                SetButtonActive(kv.Value, kv.Key == key);
            }
        }

        private void SetSpeed(int value)
        {
            Speed = value;
            int[] values = { 0, 1, 3 };
            for (int i = 0; i < _speedButtons.Count && i < values.Length; i++)
            {
                SetButtonActive(_speedButtons[i], values[i] == value);
            }
        }

        private void SetOverlay(string mode)
        {
            if (_view != null) _view.OverlayMode = mode;
            string[] modes = { "none", "power", "water", "pollution", "land" };
            for (int i = 0; i < _overlayButtons.Count && i < modes.Length; i++)
            {
                SetButtonActive(_overlayButtons[i], modes[i] == mode);
            }
        }

        // ------------------------------------------------------------ 토스트

        private void BuildToastArea()
        {
            _toastRoot = NewRect("Toasts", _canvas.transform);
            Stretch(_toastRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-420f, 320f), new Vector2(420f, 560f));

            VerticalLayoutGroup layout = _toastRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
        }

        public void Toast(string message, string kind)
        {
            if (_toastRoot == null) return;
            if (_toastRoot.childCount > 3) Destroy(_toastRoot.GetChild(0).gameObject);

            Color border = kind == "good" ? new Color(0.16f, 0.72f, 0.44f, 0.95f)
                        : kind == "bad" ? new Color(0.85f, 0.28f, 0.25f, 0.95f)
                        : kind == "warn" ? new Color(0.85f, 0.65f, 0.20f, 0.95f)
                        : new Color(0.10f, 0.13f, 0.18f, 0.95f);

            Image panel = NewPanel("Toast", _toastRoot, border);
            LayoutElement le = panel.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 62f;

            Text t = NewText("Msg", panel.transform, 26, TextAnchor.MiddleCenter, Color.white);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            t.text = message;

            Destroy(panel.gameObject, 2.8f);
        }

        // ------------------------------------------------------------ 갱신

        public void RefreshStats()
        {
            if (_sim == null) return;

            _moneyText.text = FormatMoney(_sim.Money);
            _moneyText.color = _sim.Money < 0f ? new Color(1f, 0.42f, 0.42f) : TextColor;
            _popText.text = _sim.Population.ToString("N0");
            _happyText.text = Mathf.RoundToInt(_sim.Happiness) + "%";

            _tierText.text = _sim.TierName + (string.IsNullOrEmpty(_sim.EventName) ? "" : " · " + _sim.EventName);
            _dateText.text = (_sim.Month / 12 + 1) + "년 " + (_sim.Month % 12 + 1) + "월";

            SetFillWidth(_rFill, 22f + Mathf.Clamp(_sim.DemandR, -1f, 1f) * 22f);
            SetFillWidth(_cFill, 22f + Mathf.Clamp(_sim.DemandC, -1f, 1f) * 22f);
            SetFillWidth(_iFill, 22f + Mathf.Clamp(_sim.DemandI, -1f, 1f) * 22f);

            float pRatio = _sim.PowerUsage > 0 ? Mathf.Clamp01((float)_sim.PowerCapacity / _sim.PowerUsage) : 1f;
            float wRatio = _sim.WaterUsage > 0 ? Mathf.Clamp01((float)_sim.WaterCapacity / _sim.WaterUsage) : 1f;
            SetFillWidth(_powerFill, 70f * pRatio);
            SetFillWidth(_waterFill, 70f * wRatio);
            _powerFill.color = pRatio < 1f ? new Color(1f, 0.35f, 0.30f) : new Color(1f, 0.84f, 0.25f);
            _waterFill.color = wRatio < 1f ? new Color(1f, 0.35f, 0.30f) : new Color(0.25f, 0.74f, 1f);

            RefreshToolLocks();
        }

        private void RefreshToolLocks()
        {
            IList<BuildItem> all = Balance.All;
            for (int i = 0; i < all.Count; i++)
            {
                BuildItem item = all[i];
                Button b;
                if (!_toolButtons.TryGetValue(item.Key, out b) || b == null) continue;
                bool locked = item.UnlockPop > 0 && _sim.Population < item.UnlockPop;
                SetButtonLabel(b, item.Name + "\n" + (locked ? "인구 " + item.UnlockPop.ToString("N0") : "W " + item.Cost.ToString("N0")));
            }
        }

        private static void SetFillWidth(Image fill, float width)
        {
            if (fill == null) return;
            RectTransform rt = fill.rectTransform;
            rt.offsetMax = new Vector2(Mathf.Max(2f, width), 0f);
        }

        public static string FormatMoney(float value)
        {
            bool negative = value < 0f;
            float v = Mathf.Abs(value);
            string s;
            if (v >= 100000000f) s = (v / 100000000f).ToString("0.##") + "억";
            else if (v >= 10000f) s = (v / 10000f).ToString("0.#") + "만";
            else s = Mathf.RoundToInt(v).ToString("N0");
            return (negative ? "-W " : "W ") + s;
        }
    }
}
