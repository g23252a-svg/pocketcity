// UnityEngine 최소 스텁 — 내 코드가 컴파일되는지 확인하기 위한 것이다.
// 실제 Unity API 시그니처를 그대로 따라 적었다. 이걸로 컴파일이 통과하면
// 최소한 "내 코드 안의 오타·타입 불일치"는 없다는 뜻이다.
// (Unity API 자체와의 미세한 차이는 여전히 잡지 못한다)
using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float f) { return new Vector2(a.x * f, a.y * f); }
        public static float Distance(Vector2 a, Vector2 b) { return 0f; }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 back { get { return new Vector3(0, 0, -1); } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float f) { return new Vector3(a.x * f, a.y * f, a.z * f); }
        public static Vector3 operator *(Quaternion q, Vector3 v) { return v; }
    }

    public struct Quaternion
    {
        public static Quaternion identity { get { return new Quaternion(); } }
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion(); }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
    }

    public struct Matrix4x4
    {
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) { return new Matrix4x4(); }
    }

    public struct Ray
    {
        public Vector3 GetPoint(float d) { return Vector3.zero; }
    }

    public struct Plane
    {
        public Plane(Vector3 normal, Vector3 point) { }
        public bool Raycast(Ray ray, out float enter) { enter = 0f; return true; }
    }

    public static class Mathf
    {
        public const float Infinity = float.PositiveInfinity;
        public static float Clamp(float v, float a, float b) { return v; }
        public static int Clamp(int v, int a, int b) { return v; }
        public static float Clamp01(float v) { return v; }
        public static float Min(float a, float b) { return a; }
        public static int Min(int a, int b) { return a; }
        public static float Max(float a, float b) { return a; }
        public static float Abs(float v) { return v; }
        public static int Abs(int v) { return v; }
        public static int FloorToInt(float v) { return 0; }
        public static int RoundToInt(float v) { return 0; }
        public static float MoveTowards(float cur, float target, float delta) { return target; }
        public static bool Approximately(float a, float b) { return false; }
    }

    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public override bool Equals(object o) { return base.Equals(o); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }

    public class Component : Object
    {
        public Transform transform { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public T GetComponent<T>() where T : class { return null; }
        public T GetComponentInChildren<T>() where T : class { return null; }
    }

    public class Behaviour : Component { public bool enabled; }

    public class MonoBehaviour : Behaviour { }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public int childCount { get { return 0; } }
        public Transform GetChild(int i) { return null; }
        public void SetParent(Transform p, bool worldPositionStays) { }
    }

    public class GameObject : Object
    {
        public string tag;
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform { get { return null; } }
        public T AddComponent<T>() where T : Component { return null; }
        public T GetComponent<T>() where T : class { return null; }
    }

    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public Vector3[] normals { get; set; }
        public Vector2[] uv { get; set; }
        public int[] triangles { get; set; }
        public void RecalculateBounds() { }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) { return null; }
    }

    public class Material : Object
    {
        public Material(Shader s) { }
        public Color color { get; set; }
        public bool enableInstancing { get; set; }
        public int renderQueue { get; set; }
        public bool HasProperty(string name) { return true; }
        public void SetFloat(string n, float v) { }
        public void SetColor(string n, Color v) { }
        public void SetInt(string n, int v) { }
        public void EnableKeyword(string k) { }
        public void DisableKeyword(string k) { }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
    }

    public class MeshRenderer : Renderer { }
    public class MeshFilter : Component { public Mesh sharedMesh { get; set; } }

    public static class Graphics
    {
        public static void DrawMeshInstanced(Mesh mesh, int submesh, Material mat, Matrix4x4[] matrices, int count) { }
    }

    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }

    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public Ray ScreenPointToRay(Vector3 pos) { return new Ray(); }
        public Ray ScreenPointToRay(Vector2 pos) { return new Ray(); }
    }

    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; }
        public static Color fogColor { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
    }

    public class AudioListener : Behaviour { }

    // 실제 Unity에서 SleepTimeout은 enum이 아니라 const int를 담은 정적 클래스다
    public static class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    public static class Application
    {
        public static int targetFrameRate { get; set; }
    }

    public static class Screen
    {
        public static int sleepTimeout { get; set; }
        public static int width { get { return 0; } }
        public static int height { get { return 0; } }
    }

    public static class Time
    {
        public static float deltaTime { get { return 0f; } }
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public struct Touch
    {
        public int fingerId { get { return 0; } }
        public Vector2 position { get { return Vector2.zero; } }
        public TouchPhase phase { get { return TouchPhase.Began; } }
    }

    public static class Input
    {
        public static int touchCount { get { return 0; } }
        public static Touch GetTouch(int i) { return new Touch(); }
        public static bool GetMouseButton(int b) { return false; }
        public static bool GetMouseButtonDown(int b) { return false; }
        public static bool GetMouseButtonUp(int b) { return false; }
        public static Vector3 mousePosition { get { return Vector3.zero; } }
        public static float GetAxis(string name) { return 0f; }
    }

    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string[] names, int size) { return null; }
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
    }

    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }

    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 sizeDelta { get; set; }
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, BeforeSplashScreen, SubsystemRegistration, AfterAssembliesLoaded }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public enum BlendMode { Zero, One, DstColor, SrcColor, OneMinusDstColor, SrcAlpha, OneMinusSrcColor, DstAlpha, OneMinusDstAlpha, SrcAlphaSaturate, OneMinusSrcAlpha }
    public class RenderPipelineAsset : Object { }
    public static class GraphicsSettings
    {
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
    }
}

namespace UnityEngine.UI
{
    public class Graphic : UnityEngine.Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
    }

    public class Image : Graphic { }

    public class Text : Graphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
    }

    public class ButtonClickedEvent
    {
        public void AddListener(UnityEngine.Events.UnityAction call) { }
    }

    public class Selectable : UnityEngine.Behaviour
    {
        public Graphic targetGraphic { get; set; }
    }

    public class Button : Selectable
    {
        public ButtonClickedEvent onClick { get { return null; } }
    }

    public class CanvasScaler : UnityEngine.Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : UnityEngine.Behaviour { }

    public class LayoutElement : UnityEngine.Behaviour
    {
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
    }

    public class LayoutGroup : UnityEngine.Behaviour
    {
        public TextAnchor childAlignment { get; set; }
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : UnityEngine.Behaviour
    {
        public static EventSystem current { get { return null; } }
        public bool IsPointerOverGameObject() { return false; }
        public bool IsPointerOverGameObject(int pointerId) { return false; }
    }

    public class StandaloneInputModule : UnityEngine.Behaviour { }
}
