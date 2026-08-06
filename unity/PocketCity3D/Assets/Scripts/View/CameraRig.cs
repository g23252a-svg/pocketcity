using UnityEngine;

namespace PocketCity
{
    /// <summary>
    /// 비스듬히 내려다보는 카메라. 지면(y=0) 평면과의 교점으로 드래그를 계산하므로
    /// 손가락 아래의 지점이 그대로 따라온다.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public float Yaw = 45f;
        public float Pitch = 52f;
        public float Distance = 34f;
        public float MinDistance = 10f;
        public float MaxDistance = 90f;

        public Vector3 Pivot;

        private Camera _cam;
        private readonly Plane _ground = new Plane(Vector3.up, Vector3.zero);

        public Camera Cam { get { return _cam; } }

        public void Initialize(Camera cam, Vector3 pivot)
        {
            _cam = cam;
            Pivot = pivot;
            Apply();
        }

        public void Apply()
        {
            if (_cam == null) return;
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -Distance);
            _cam.transform.position = Pivot + offset;
            _cam.transform.rotation = rot;
        }

        public bool ScreenToGround(Vector2 screen, out Vector3 world)
        {
            world = Vector3.zero;
            if (_cam == null) return false;
            Ray ray = _cam.ScreenPointToRay(screen);
            float dist;
            if (!_ground.Raycast(ray, out dist)) return false;
            world = ray.GetPoint(dist);
            return true;
        }

        /// <summary>화면상의 두 지점이 가리키는 지면 좌표 차이만큼 피벗을 옮긴다.</summary>
        public void DragTo(Vector2 fromScreen, Vector2 toScreen)
        {
            Vector3 a, b;
            if (!ScreenToGround(fromScreen, out a)) return;
            if (!ScreenToGround(toScreen, out b)) return;
            Pivot += a - b;
            ClampPivot();
            Apply();
        }

        public void Zoom(float factor, Vector2 focusScreen)
        {
            Vector3 before;
            bool hadBefore = ScreenToGround(focusScreen, out before);

            Distance = Mathf.Clamp(Distance / Mathf.Max(0.01f, factor), MinDistance, MaxDistance);
            Apply();

            // 확대 중심이 손가락 아래에 유지되도록 보정
            Vector3 after;
            if (hadBefore && ScreenToGround(focusScreen, out after))
            {
                Pivot += before - after;
                ClampPivot();
                Apply();
            }
        }

        private void ClampPivot()
        {
            float w = Balance.W * CityView.TileSize;
            float h = Balance.H * CityView.TileSize;
            Pivot = new Vector3(
                Mathf.Clamp(Pivot.x, -10f, w + 10f),
                0f,
                Mathf.Clamp(Pivot.z, -10f, h + 10f));
        }
    }
}
