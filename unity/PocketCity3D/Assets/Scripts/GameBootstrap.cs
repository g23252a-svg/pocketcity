using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PocketCity
{
    /// <summary>
    /// 씬 전체를 코드로 구성한다. 빈 씬에서 Play만 눌러도 게임이 시작되므로
    /// 프리팹 배치나 인스펙터 배선이 전혀 필요 없다.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public CitySim Sim { get; private set; }

        private CityView _view;
        private CameraRig _rig;
        private GameHud _hud;
        private TouchController _touch;
        private float _accumulator;

        private static bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            // FindObjectOfType는 Unity 6에서 obsolete라 정적 플래그로 중복 생성을 막는다
            if (_started) return;
            _started = true;
            GameObject go = new GameObject("PocketCity");
            go.AddComponent<GameBootstrap>();
        }

        private void OnDestroy()
        {
            _started = false;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Sim = new CitySim();

            SetupEnvironment();
            SetupCamera();
            SetupEventSystem();

            GameObject viewGo = new GameObject("CityView");
            viewGo.transform.SetParent(transform, false);
            _view = viewGo.AddComponent<CityView>();
            _view.Initialize(Sim);

            GameObject hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(transform, false);
            _hud = hudGo.AddComponent<GameHud>();
            _hud.Initialize(Sim, _view);

            _touch = gameObject.AddComponent<TouchController>();
            _touch.Initialize(Sim, _rig, _view, _hud);

            _hud.Toast("도로를 깔고 주거 구역을 지정해 보세요", "good");
            _hud.Toast("구역은 도로·전기·수도가 모두 닿아야 자랍니다", "info");
        }

        private void SetupEnvironment()
        {
            GameObject lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(transform, false);
            Light sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            lightGo.transform.rotation = Quaternion.Euler(52f, 40f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.50f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.34f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.18f, 0.20f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.13f, 0.17f, 0.23f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 190f;
        }

        private void SetupCamera()
        {
            GameObject camGo = new GameObject("MainCamera");
            camGo.transform.SetParent(transform, false);
            camGo.tag = "MainCamera";

            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.13f, 0.18f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();

            _rig = camGo.AddComponent<CameraRig>();
            Vector3 pivot = new Vector3(Balance.W * 0.62f * CityView.TileSize, 0f, Balance.H * 0.5f * CityView.TileSize);
            _rig.Initialize(cam, pivot);
        }

        private void SetupEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject es = new GameObject("EventSystem");
            es.transform.SetParent(transform, false);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private void Update()
        {
            if (Sim == null || _hud == null) return;
            if (_hud.Speed <= 0) return;

            _accumulator += Time.deltaTime * _hud.Speed;

            int guard = 0;
            while (_accumulator >= Balance.TickSeconds && guard < 4)
            {
                _accumulator -= Balance.TickSeconds;
                guard++;

                List<SimEvent> events = Sim.Tick();
                for (int i = 0; i < events.Count; i++)
                {
                    _hud.Toast(events[i].Message, events[i].Kind);
                }
            }

            if (guard > 0) _hud.RefreshStats();
        }
    }
}
