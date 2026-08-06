using System.Collections.Generic;
using UnityEngine;

namespace PocketCity
{
    /// <summary>
    /// 도시를 3D로 그린다. 타일마다 GameObject를 만들면 수천 개가 되어 폰에서 버티지
    /// 못하므로, 종류별로 모아 Graphics.DrawMeshInstanced로 한 번에 그린다.
    /// </summary>
    public class CityView : MonoBehaviour
    {
        public const float TileSize = 1f;
        private const int BatchLimit = 1023;   // DrawMeshInstanced 1회 상한

        private CitySim _sim;
        private Mesh _cube;
        private Mesh _groundQuad;

        private Material _matGround;
        private Material _matWater;
        private Material _matRoad;
        private Material _matTreeTrunk;
        private Material _matTreeLeaf;
        private Material _matRubble;
        private Material _matZoneEmptyR, _matZoneEmptyC, _matZoneEmptyI;
        private Material[] _matResidential;
        private Material[] _matCommercial;
        private Material[] _matIndustrial;
        private Material _matFacilityBase;
        private Dictionary<Tile, Material> _matFacility;
        private Material _matCursor;

        private readonly Dictionary<Material, List<Matrix4x4>> _batches =
            new Dictionary<Material, List<Matrix4x4>>();

        private float[] _visualHeight;      // 성장 애니메이션용 현재 높이
        private GameObject _ground;

        public string OverlayMode = "none";  // none | power | water | pollution | land
        private Material _matOverlayGood, _matOverlayBad, _matOverlayWarn;

        public void Initialize(CitySim sim)
        {
            _sim = sim;
            _cube = MeshFactory.CreateCube();
            _visualHeight = new float[sim.W * sim.H];

            _matGround = MeshFactory.CreateMaterial(new Color(0.24f, 0.40f, 0.26f), 0.05f, 0f);
            _matWater = MeshFactory.CreateMaterial(new Color(0.12f, 0.30f, 0.52f), 0.75f, 0.1f);
            _matRoad = MeshFactory.CreateMaterial(new Color(0.20f, 0.21f, 0.23f), 0.15f, 0f);
            _matTreeTrunk = MeshFactory.CreateMaterial(new Color(0.30f, 0.22f, 0.14f), 0.05f, 0f);
            _matTreeLeaf = MeshFactory.CreateMaterial(new Color(0.20f, 0.45f, 0.24f), 0.05f, 0f);
            _matRubble = MeshFactory.CreateMaterial(new Color(0.32f, 0.28f, 0.22f), 0.05f, 0f);

            _matZoneEmptyR = MeshFactory.CreateMaterial(new Color(0.30f, 0.52f, 0.34f), 0.05f, 0f);
            _matZoneEmptyC = MeshFactory.CreateMaterial(new Color(0.26f, 0.40f, 0.58f), 0.05f, 0f);
            _matZoneEmptyI = MeshFactory.CreateMaterial(new Color(0.52f, 0.44f, 0.24f), 0.05f, 0f);

            _matResidential = new Material[]
            {
                MeshFactory.CreateMaterial(new Color(0.42f, 0.68f, 0.46f), 0.25f, 0f),
                MeshFactory.CreateMaterial(new Color(0.36f, 0.74f, 0.50f), 0.30f, 0f),
                MeshFactory.CreateMaterial(new Color(0.44f, 0.86f, 0.62f), 0.40f, 0.05f)
            };
            _matCommercial = new Material[]
            {
                MeshFactory.CreateMaterial(new Color(0.34f, 0.52f, 0.78f), 0.35f, 0.05f),
                MeshFactory.CreateMaterial(new Color(0.30f, 0.60f, 0.90f), 0.45f, 0.10f),
                MeshFactory.CreateMaterial(new Color(0.42f, 0.72f, 1.00f), 0.60f, 0.20f)
            };
            _matIndustrial = new Material[]
            {
                MeshFactory.CreateMaterial(new Color(0.62f, 0.50f, 0.26f), 0.20f, 0.05f),
                MeshFactory.CreateMaterial(new Color(0.74f, 0.58f, 0.26f), 0.25f, 0.10f),
                MeshFactory.CreateMaterial(new Color(0.86f, 0.68f, 0.30f), 0.30f, 0.15f)
            };

            _matFacilityBase = MeshFactory.CreateMaterial(new Color(0.88f, 0.90f, 0.93f), 0.30f, 0f);
            _matFacility = new Dictionary<Tile, Material>();
            _matFacility[Tile.PowerCoal] = MeshFactory.CreateMaterial(new Color(0.45f, 0.42f, 0.44f), 0.2f, 0.3f);
            _matFacility[Tile.PowerSolar] = MeshFactory.CreateMaterial(new Color(0.25f, 0.32f, 0.45f), 0.7f, 0.4f);
            _matFacility[Tile.WaterTower] = MeshFactory.CreateMaterial(new Color(0.35f, 0.70f, 0.90f), 0.5f, 0.1f);
            _matFacility[Tile.Park] = MeshFactory.CreateMaterial(new Color(0.28f, 0.60f, 0.32f), 0.1f, 0f);
            _matFacility[Tile.Plaza] = MeshFactory.CreateMaterial(new Color(0.72f, 0.68f, 0.58f), 0.2f, 0f);
            _matFacility[Tile.Police] = MeshFactory.CreateMaterial(new Color(0.25f, 0.35f, 0.72f), 0.3f, 0f);
            _matFacility[Tile.Fire] = MeshFactory.CreateMaterial(new Color(0.78f, 0.24f, 0.20f), 0.3f, 0f);
            _matFacility[Tile.Hospital] = MeshFactory.CreateMaterial(new Color(0.92f, 0.94f, 0.96f), 0.3f, 0f);
            _matFacility[Tile.School] = MeshFactory.CreateMaterial(new Color(0.85f, 0.62f, 0.32f), 0.3f, 0f);

            _matCursor = MeshFactory.CreateTransparent(new Color(1f, 1f, 1f, 0.35f));
            _matOverlayGood = MeshFactory.CreateTransparent(new Color(0.35f, 0.85f, 0.45f, 0.30f));
            _matOverlayBad = MeshFactory.CreateTransparent(new Color(0.9f, 0.25f, 0.2f, 0.32f));
            _matOverlayWarn = MeshFactory.CreateTransparent(new Color(1f, 0.8f, 0.25f, 0.30f));

            BuildGround();
        }

        private void BuildGround()
        {
            _ground = new GameObject("Ground");
            _ground.transform.SetParent(transform, false);
            _groundQuad = MeshFactory.CreateQuad(_sim.W * TileSize);
            MeshFilter mf = _ground.AddComponent<MeshFilter>();
            mf.sharedMesh = _groundQuad;
            MeshRenderer mr = _ground.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _matGround;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ground.transform.position = new Vector3(_sim.W * TileSize * 0.5f, -0.02f, _sim.H * TileSize * 0.5f);
        }

        public Vector3 TileToWorld(int x, int y)
        {
            return new Vector3((x + 0.5f) * TileSize, 0f, (y + 0.5f) * TileSize);
        }

        public bool WorldToTile(Vector3 world, out int x, out int y)
        {
            x = Mathf.FloorToInt(world.x / TileSize);
            y = Mathf.FloorToInt(world.z / TileSize);
            return _sim.InBounds(x, y);
        }

        private void Push(Material mat, Matrix4x4 m)
        {
            List<Matrix4x4> list;
            if (!_batches.TryGetValue(mat, out list))
            {
                list = new List<Matrix4x4>();
                _batches[mat] = list;
            }
            list.Add(m);
        }

        private static Matrix4x4 Box(Vector3 center, float sizeX, float height, float sizeZ)
        {
            return Matrix4x4.TRS(center, Quaternion.identity, new Vector3(sizeX, height, sizeZ));
        }

        public int CursorX = -1;
        public int CursorY = -1;
        public bool CursorValid;

        private void LateUpdate()
        {
            if (_sim == null) return;

            foreach (KeyValuePair<Material, List<Matrix4x4>> kv in _batches) kv.Value.Clear();

            float dt = Time.deltaTime;

            for (int y = 0; y < _sim.H; y++)
            {
                for (int x = 0; x < _sim.W; x++)
                {
                    int k = _sim.Index(x, y);
                    Tile c = (Tile)_sim.TileCode[k];
                    if (c == Tile.Empty) continue;

                    Vector3 p = TileToWorld(x, y);

                    switch (c)
                    {
                        case Tile.Water:
                            Push(_matWater, Box(new Vector3(p.x, -0.30f, p.z), TileSize, 0.30f, TileSize));
                            break;

                        case Tile.Road:
                            Push(_matRoad, Box(p, TileSize, 0.06f, TileSize));
                            break;

                        case Tile.Tree:
                            Push(_matTreeTrunk, Box(p, 0.14f, 0.30f, 0.14f));
                            Push(_matTreeLeaf, Box(new Vector3(p.x, 0.28f, p.z), 0.55f, 0.50f, 0.55f));
                            break;

                        case Tile.Rubble:
                            Push(_matRubble, Box(p, 0.7f, 0.12f, 0.7f));
                            break;

                        case Tile.Residential:
                        case Tile.Commercial:
                        case Tile.Industrial:
                            DrawZone(k, c, p, dt);
                            break;

                        default:
                            DrawFacility(x, y, c, p);
                            break;
                    }

                    DrawOverlay(k, p);
                }
            }

            if (CursorValid && _sim.InBounds(CursorX, CursorY))
            {
                Vector3 cp = TileToWorld(CursorX, CursorY);
                Push(_matCursor, Box(new Vector3(cp.x, 0.02f, cp.z), TileSize * 0.98f, 0.5f, TileSize * 0.98f));
            }

            Flush();
        }

        private void DrawZone(int k, Tile c, Vector3 p, float dt)
        {
            int lv = _sim.Level[k];
            float target = lv == 0 ? 0.08f : (lv == 1 ? 0.55f : (lv == 2 ? 1.25f : 2.30f));
            _visualHeight[k] = Mathf.MoveTowards(_visualHeight[k], target, dt * 1.6f);
            float h = _visualHeight[k];
            if (h <= 0.001f) h = 0.02f;

            if (lv == 0)
            {
                Material flat = c == Tile.Residential ? _matZoneEmptyR
                              : (c == Tile.Commercial ? _matZoneEmptyC : _matZoneEmptyI);
                Push(flat, Box(p, TileSize * 0.86f, h, TileSize * 0.86f));
                return;
            }

            Material[] set = c == Tile.Residential ? _matResidential
                           : (c == Tile.Commercial ? _matCommercial : _matIndustrial);
            Material mat = set[Mathf.Clamp(lv - 1, 0, set.Length - 1)];

            float footprint = lv == 1 ? 0.62f : (lv == 2 ? 0.72f : 0.80f);
            Push(mat, Box(p, TileSize * footprint, h, TileSize * footprint));

            // 옥상 — 살짝 더 밝은 층을 얹어 실루엣을 또렷하게
            if (lv >= 2)
            {
                Push(_matFacilityBase,
                     Box(new Vector3(p.x, h, p.z), TileSize * footprint * 0.55f, 0.10f, TileSize * footprint * 0.55f));
            }
        }

        private void DrawFacility(int x, int y, Tile c, Vector3 p)
        {
            Material mat;
            if (!_matFacility.TryGetValue(c, out mat)) mat = _matFacilityBase;

            bool working = _sim.HasRoadAdjacent(x, y);
            float h;
            float w = 0.80f;

            if (c == Tile.Park || c == Tile.Plaza) h = 0.14f;
            else if (c == Tile.PowerCoal) h = 1.10f;
            else if (c == Tile.PowerSolar) { h = 0.20f; w = 0.92f; }
            else if (c == Tile.WaterTower) h = 1.30f;
            else h = 0.70f;

            Push(mat, Box(p, TileSize * w, h, TileSize * w));

            if (c == Tile.WaterTower)
            {
                Push(mat, Box(new Vector3(p.x, h, p.z), TileSize * 0.95f, 0.35f, TileSize * 0.95f));
            }
            else if (c == Tile.PowerCoal)
            {
                Push(_matRubble, Box(new Vector3(p.x - 0.18f, h, p.z), 0.22f, 0.55f, 0.22f));
                Push(_matRubble, Box(new Vector3(p.x + 0.18f, h, p.z), 0.22f, 0.42f, 0.22f));
            }

            // 도로에 닿지 않아 작동하지 않으면 빨간 표시를 띄운다
            if (!working)
            {
                Push(_matOverlayBad, Box(new Vector3(p.x, h + 0.35f, p.z), 0.34f, 0.34f, 0.34f));
            }
        }

        private void DrawOverlay(int k, Vector3 p)
        {
            if (OverlayMode == "none") return;

            Material mat = null;
            if (OverlayMode == "power")
            {
                mat = _sim.FieldPower[k] == 1 ? _matOverlayGood : _matOverlayBad;
            }
            else if (OverlayMode == "water")
            {
                mat = _sim.FieldWater[k] == 1 ? _matOverlayGood : _matOverlayBad;
            }
            else if (OverlayMode == "pollution")
            {
                if (_sim.FieldPollution[k] > 1f) mat = _matOverlayBad;
            }
            else if (OverlayMode == "land")
            {
                float v = _sim.FieldLand[k];
                mat = v >= 60f ? _matOverlayGood : (v >= 35f ? _matOverlayWarn : _matOverlayBad);
            }

            if (mat != null)
            {
                Push(mat, Box(new Vector3(p.x, 0.05f, p.z), TileSize, 0.02f, TileSize));
            }
        }

        private readonly Matrix4x4[] _scratch = new Matrix4x4[BatchLimit];

        private void Flush()
        {
            foreach (KeyValuePair<Material, List<Matrix4x4>> kv in _batches)
            {
                List<Matrix4x4> list = kv.Value;
                int count = list.Count;
                if (count == 0) continue;

                int offset = 0;
                while (offset < count)
                {
                    int n = Mathf.Min(BatchLimit, count - offset);
                    for (int i = 0; i < n; i++) _scratch[i] = list[offset + i];
                    Graphics.DrawMeshInstanced(_cube, 0, kv.Key, _scratch, n);
                    offset += n;
                }
            }
        }
    }
}
