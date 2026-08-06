using UnityEngine;
using UnityEngine.EventSystems;

namespace PocketCity
{
    /// <summary>
    /// 터치와 마우스를 하나의 흐름으로 처리한다.
    /// 도구를 안 골랐으면 한 손가락 = 지도 이동, 골랐으면 한 손가락 = 연속 건설.
    /// 두 손가락은 언제나 확대·이동.
    /// </summary>
    public class TouchController : MonoBehaviour
    {
        private CitySim _sim;
        private CameraRig _rig;
        private CityView _view;
        private GameHud _hud;

        private bool _dragging;
        private Vector2 _lastScreen;
        private int _lastTileX = -1, _lastTileY = -1;
        private float _pinchStartDistance;
        private bool _pinching;

        public void Initialize(CitySim sim, CameraRig rig, CityView view, GameHud hud)
        {
            _sim = sim;
            _rig = rig;
            _view = view;
            _hud = hud;
        }

        private static bool PointerOverUI(int fingerId)
        {
            if (EventSystem.current == null) return false;
            return fingerId < 0
                ? EventSystem.current.IsPointerOverGameObject()
                : EventSystem.current.IsPointerOverGameObject(fingerId);
        }

        private void Update()
        {
            if (_sim == null) return;

            if (Input.touchCount >= 2) { HandlePinch(); return; }
            if (_pinching && Input.touchCount < 2) { _pinching = false; _dragging = false; }

            if (Input.touchCount == 1) HandleSingle(Input.GetTouch(0));
            else HandleMouse();
        }

        private void HandlePinch()
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);
            Vector2 mid = (t0.position + t1.position) * 0.5f;

            if (!_pinching)
            {
                _pinching = true;
                _pinchStartDistance = Mathf.Max(1f, dist);
                _lastScreen = mid;
                _dragging = false;
                _view.CursorValid = false;
                return;
            }

            float factor = dist / Mathf.Max(1f, _pinchStartDistance);
            if (!Mathf.Approximately(factor, 1f)) _rig.Zoom(factor, mid);
            _pinchStartDistance = Mathf.Max(1f, dist);

            _rig.DragTo(_lastScreen, mid);
            _lastScreen = mid;
        }

        private void HandleSingle(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (PointerOverUI(touch.fingerId)) return;
                Begin(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Move(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                End();
            }
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (PointerOverUI(-1)) return;
                Begin(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && _dragging)
            {
                Move(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                End();
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _rig.Zoom(1f + scroll * 3f, Input.mousePosition);
            }
        }

        private void Begin(Vector2 screen)
        {
            _dragging = true;
            _lastScreen = screen;
            _lastTileX = -1;
            _lastTileY = -1;

            if (!string.IsNullOrEmpty(_hud.SelectedTool)) ApplyAt(screen);
        }

        private void Move(Vector2 screen)
        {
            if (!_dragging) return;

            if (string.IsNullOrEmpty(_hud.SelectedTool))
            {
                _rig.DragTo(_lastScreen, screen);
                _lastScreen = screen;
                _view.CursorValid = false;
            }
            else
            {
                ApplyAt(screen);
                _lastScreen = screen;
            }
        }

        private void End()
        {
            _dragging = false;
            _pinching = false;
            _view.CursorValid = false;
            _lastTileX = -1;
            _lastTileY = -1;
        }

        private void ApplyAt(Vector2 screen)
        {
            Vector3 world;
            if (!_rig.ScreenToGround(screen, out world)) return;

            int x, y;
            if (!_view.WorldToTile(world, out x, out y)) return;

            _view.CursorX = x;
            _view.CursorY = y;
            _view.CursorValid = true;

            if (x == _lastTileX && y == _lastTileY) return;

            // 드래그가 빠르면 칸을 건너뛴다. 직선으로 이어 칠한다.
            if (_lastTileX >= 0) PaintLine(_lastTileX, _lastTileY, x, y);
            else Paint(x, y);

            _lastTileX = x;
            _lastTileY = y;
        }

        private void PaintLine(int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int guard = 0;

            while (guard++ < 512)
            {
                if (!(x0 == _lastTileX && y0 == _lastTileY)) Paint(x0, y0);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private void Paint(int x, int y)
        {
            string tool = _hud.SelectedTool;
            if (string.IsNullOrEmpty(tool)) return;

            BuildResult r = _sim.Build(tool, x, y);
            if (r.Ok)
            {
                _sim.ComputeFields();
                _hud.RefreshStats();
            }
            else if (!string.IsNullOrEmpty(r.Reason))
            {
                _hud.Toast(r.Reason, "warn");
                if (r.Reason.Contains("자금")) _hud.SelectTool(null);
            }
        }
    }
}
