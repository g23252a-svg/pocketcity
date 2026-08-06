using System;
using System.Collections.Generic;

namespace PocketCity
{
    public struct BuildResult
    {
        public bool Ok;
        public int Cost;
        public string Reason;
    }

    public class SimEvent
    {
        public string Kind;      // good | warn | bad | info
        public string Message;
        public SimEvent(string kind, string message) { Kind = kind; Message = message; }
    }

    public class TileInfo
    {
        public int X, Y;
        public Tile Code;
        public string Name;
        public int Level;
        public bool HasRoad, HasPower, HasWater;
        public int Land, Pollution, Police, Fire, Education, Health;
        public int Capacity;
    }

    /// <summary>
    /// 도시 시뮬레이션. UnityEngine에 전혀 의존하지 않으므로 에디터 밖에서도
    /// 그대로 돌려 밸런스를 수치로 검증할 수 있다.
    /// </summary>
    public class CitySim
    {
        public readonly int W = Balance.W;
        public readonly int H = Balance.H;

        public byte[] TileCode;
        public byte[] Level;
        private sbyte[] _streak;

        public float Money = Balance.StartMoney;
        public float Tax = Balance.StartTax;
        public int Month;

        public int Population;
        public int JobsCommercial;
        public int JobsIndustrial;
        public float Happiness = 55f;
        public float Unemployment;
        public float Crime;
        public float AverageLand;
        public float AveragePollution;

        public int PowerCapacity, PowerUsage, WaterCapacity, WaterUsage;
        public float DemandR = 1f, DemandC, DemandI;
        public float Income, Expense;
        public int TierIndex;

        public string EventName;
        private float _eventDemand;
        private float _eventIncome = 1f;
        private int _eventMonthsLeft;

        // 필드(반경 효과) 버퍼
        public float[] FieldLand;
        public float[] FieldPollution;
        public float[] FieldPark;
        public float[] FieldPolice;
        public float[] FieldFire;
        public float[] FieldEducation;
        public float[] FieldHealth;
        public byte[] FieldPower;
        public byte[] FieldWater;

        private float[] _terrainPark;
        private bool _terrainDirty = true;

        /// <summary>도시가 바뀔 때마다 증가한다. 뷰는 이 값이 달라질 때만 다시 그린다.</summary>
        public int Revision { get; private set; }

        public CitySim(int seed = 12345)
        {
            int n = W * H;
            TileCode = new byte[n];
            Level = new byte[n];
            _streak = new sbyte[n];

            FieldLand = new float[n];
            FieldPollution = new float[n];
            FieldPark = new float[n];
            FieldPolice = new float[n];
            FieldFire = new float[n];
            FieldEducation = new float[n];
            FieldHealth = new float[n];
            FieldPower = new byte[n];
            FieldWater = new byte[n];
            _terrainPark = new float[n];

            GenerateTerrain();
            ComputeFields();
        }

        public int Index(int x, int y) { return y * W + x; }
        public bool InBounds(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
        public Tile At(int x, int y) { return (Tile)TileCode[Index(x, y)]; }

        // ---------------------------------------------------------------- 지형

        private static float Hash2(int x, int y, int salt)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + salt * 2246822519;
                h = (h ^ (h >> 13)) * 1274126177;
                uint u = (uint)(h ^ (h >> 16));
                return u / 4294967296f;
            }
        }

        private static float Clamp(float v, float a, float b)
        {
            return v < a ? a : (v > b ? b : v);
        }

        private void GenerateTerrain()
        {
            // 굽이치는 강
            float cx = W * 0.30f;
            for (int y = 0; y < H; y++)
            {
                cx += (float)(Math.Sin(y * 0.19) * 0.85 + Math.Sin(y * 0.061) * 0.6);
                cx = Clamp(cx, 3f, W - 4f);
                int width = 2 + (y % 11 == 0 ? 1 : 0);
                int x0 = (int)Math.Round(cx - width / 2.0);
                int x1 = (int)Math.Round(cx + width / 2.0);
                for (int x = x0; x <= x1; x++)
                {
                    if (x >= 0 && x < W) TileCode[Index(x, y)] = (byte)Tile.Water;
                }
            }

            // 숲 군락
            for (int i = 0; i < 26; i++)
            {
                int gx = (int)(Hash2(i, 7, 1) * W);
                int gy = (int)(Hash2(i, 13, 2) * H);
                int r = 2 + (int)(Hash2(i, 3, 3) * 4);
                for (int y = gy - r; y <= gy + r; y++)
                {
                    for (int x = gx - r; x <= gx + r; x++)
                    {
                        if (!InBounds(x, y)) continue;
                        double d = Math.Sqrt((x - gx) * (x - gx) + (y - gy) * (y - gy));
                        if (d <= r && Hash2(x, y, 4) > d / (r + 1.0) * 0.85)
                        {
                            int k = Index(x, y);
                            if (TileCode[k] == (byte)Tile.Empty) TileCode[k] = (byte)Tile.Tree;
                        }
                    }
                }
            }

            // 시작 도로 (중앙 십자)
            int mx = (int)(W * 0.62f);
            int my = H / 2;
            for (int x = mx - 5; x <= mx + 5; x++) SetStartRoad(x, my);
            for (int y = my - 4; y <= my + 4; y++) SetStartRoad(mx, y);
        }

        private void SetStartRoad(int x, int y)
        {
            if (!InBounds(x, y)) return;
            int k = Index(x, y);
            if (TileCode[k] != (byte)Tile.Water) TileCode[k] = (byte)Tile.Road;
        }

        // ---------------------------------------------------------------- 건설

        public bool HasRoadAdjacent(int x, int y)
        {
            if (x > 0 && TileCode[Index(x - 1, y)] == (byte)Tile.Road) return true;
            if (x < W - 1 && TileCode[Index(x + 1, y)] == (byte)Tile.Road) return true;
            if (y > 0 && TileCode[Index(x, y - 1)] == (byte)Tile.Road) return true;
            if (y < H - 1 && TileCode[Index(x, y + 1)] == (byte)Tile.Road) return true;
            return false;
        }

        public BuildResult CanBuild(string key, int x, int y)
        {
            BuildResult r = new BuildResult();
            if (!InBounds(x, y)) { r.Reason = "맵 밖입니다"; return r; }

            Tile current = At(x, y);
            if (current == Tile.Water) { r.Reason = "물 위에는 지을 수 없습니다"; return r; }

            if (key == Balance.BulldozeKey)
            {
                if (current == Tile.Empty) return r;         // 조용히 무시
                // 철거는 자금이 없어도 허용한다. 부채에 빠진 도시가 유지비를 줄일 유일한 탈출구다.
                r.Ok = true;
                r.Cost = Balance.BulldozeCost;
                return r;
            }

            BuildItem item = Balance.ByKey(key);
            if (item == null) return r;

            if (item.UnlockPop > 0 && Population < item.UnlockPop)
            {
                r.Reason = "인구 " + item.UnlockPop.ToString("N0") + "명부터 해금됩니다";
                return r;
            }
            if (current == item.Code) return r;               // 이미 같은 것

            int extra = current == Tile.Empty ? 0 : (current == Tile.Tree ? 2 : Balance.BulldozeCost);
            int cost = item.Cost + extra;
            if (Money < cost) { r.Reason = "자금이 부족합니다"; r.Cost = cost; return r; }

            r.Ok = true;
            r.Cost = cost;
            return r;
        }

        public BuildResult Build(string key, int x, int y)
        {
            BuildResult r = CanBuild(key, x, y);
            if (!r.Ok) return r;

            int k = Index(x, y);
            if (TileCode[k] == (byte)Tile.Tree) _terrainDirty = true;

            TileCode[k] = key == Balance.BulldozeKey
                ? (byte)Tile.Empty
                : (byte)Balance.ByKey(key).Code;
            Level[k] = 0;
            _streak[k] = 0;
            Money -= r.Cost;
            Revision++;
            return r;
        }

        // ---------------------------------------------------------------- 필드

        private void Stamp(float[] field, int x, int y, int radius, float strength)
        {
            float r2 = radius * radius;
            float inv = 1f / (radius + 0.5f);
            int x0 = Math.Max(0, x - radius), x1 = Math.Min(W - 1, x + radius);
            int y0 = Math.Max(0, y - radius), y1 = Math.Min(H - 1, y + radius);
            for (int j = y0; j <= y1; j++)
            {
                int dy = j - y;
                int dy2 = dy * dy;
                int row = j * W;
                for (int i = x0; i <= x1; i++)
                {
                    int dx = i - x;
                    float d2 = dx * dx + dy2;
                    if (d2 > r2) continue;
                    field[row + i] += strength * (1f - (float)Math.Sqrt(d2) * inv);
                }
            }
        }

        private void MarkCircle(byte[] field, int x, int y, int radius)
        {
            float r2 = radius * radius;
            int x0 = Math.Max(0, x - radius), x1 = Math.Min(W - 1, x + radius);
            int y0 = Math.Max(0, y - radius), y1 = Math.Min(H - 1, y + radius);
            for (int j = y0; j <= y1; j++)
            {
                int dy = j - y;
                int dy2 = dy * dy;
                int row = j * W;
                for (int i = x0; i <= x1; i++)
                {
                    int dx = i - x;
                    if (dx * dx + dy2 <= r2) field[row + i] = 1;
                }
            }
        }

        public void ComputeFields()
        {
            int n = W * H;

            // 강·숲의 쾌적도는 지형이 바뀔 때만 다시 계산한다.
            if (_terrainDirty)
            {
                Array.Clear(_terrainPark, 0, n);
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        Tile c = At(x, y);
                        if (c == Tile.Tree) Stamp(_terrainPark, x, y, 2, 5f);
                        else if (c == Tile.Water) Stamp(_terrainPark, x, y, 3, 3f);
                    }
                }
                _terrainDirty = false;
            }

            Array.Clear(FieldPollution, 0, n);
            Array.Clear(FieldPolice, 0, n);
            Array.Clear(FieldFire, 0, n);
            Array.Clear(FieldEducation, 0, n);
            Array.Clear(FieldHealth, 0, n);
            Array.Clear(FieldPower, 0, n);
            Array.Clear(FieldWater, 0, n);
            Array.Copy(_terrainPark, FieldPark, n);

            int powerCap = 0, waterCap = 0;
            List<int> powerSources = new List<int>();
            List<int> waterSources = new List<int>();

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int k = Index(x, y);
                    Tile c = (Tile)TileCode[k];

                    if (c == Tile.Tree || c == Tile.Water) continue;   // terrainPark로 이미 반영

                    if (c == Tile.Industrial && Level[k] > 0)
                    {
                        Stamp(FieldPollution, x, y, Balance.PollutionRadius,
                              Balance.IndustrialPollution * Level[k]);
                        continue;
                    }

                    if (!Balance.IsFacility(c)) continue;

                    BuildItem item = Balance.ByCode(c);
                    if (item == null) continue;
                    if (!HasRoadAdjacent(x, y)) continue;   // 도로에 안 닿으면 작동하지 않는다

                    if (item.Power > 0) { powerCap += item.Power; powerSources.Add(k); }
                    if (item.Water > 0) { waterCap += item.Water; waterSources.Add(k); }
                    if (item.Pollution > 0f) Stamp(FieldPollution, x, y, item.Radius / 2, item.Pollution);
                    if (item.LandBonus > 0f) Stamp(FieldPark, x, y, item.Radius, item.LandBonus);

                    if (item.Service == ServiceKind.Police) Stamp(FieldPolice, x, y, item.Radius, 24f);
                    else if (item.Service == ServiceKind.Fire) Stamp(FieldFire, x, y, item.Radius, 24f);
                    else if (item.Service == ServiceKind.Education) Stamp(FieldEducation, x, y, item.Radius, 22f);
                    else if (item.Service == ServiceKind.Health) Stamp(FieldHealth, x, y, item.Radius, 22f);
                }
            }

            for (int i = 0; i < powerSources.Count; i++)
            {
                int k = powerSources[i];
                BuildItem item = Balance.ByCode((Tile)TileCode[k]);
                MarkCircle(FieldPower, k % W, k / W, item.Radius);
            }
            for (int i = 0; i < waterSources.Count; i++)
            {
                int k = waterSources[i];
                BuildItem item = Balance.ByCode((Tile)TileCode[k]);
                MarkCircle(FieldWater, k % W, k / W, item.Radius);
            }

            PowerCapacity = powerCap;
            WaterCapacity = waterCap;

            float sumLand = 0f, sumPollution = 0f;
            int zoneCount = 0;
            for (int k = 0; k < n; k++)
            {
                float v = Balance.BaseLandValue;
                v += Math.Min(38f, FieldPark[k] * 0.85f);
                v += Math.Min(9f, FieldPolice[k] * 0.35f);
                v += Math.Min(6f, FieldFire[k] * 0.22f);
                v += Math.Min(11f, FieldEducation[k] * 0.45f);
                v += Math.Min(9f, FieldHealth[k] * 0.38f);
                v -= Math.Min(34f, FieldPollution[k] * 1.15f);
                v -= Crime;
                FieldLand[k] = Clamp(v, 0f, 100f);

                if (Balance.IsZone((Tile)TileCode[k]))
                {
                    sumLand += FieldLand[k];
                    sumPollution += FieldPollution[k];
                    zoneCount++;
                }
            }
            AverageLand = zoneCount > 0 ? sumLand / zoneCount : 0f;
            AveragePollution = zoneCount > 0 ? sumPollution / zoneCount : 0f;
        }

        // ---------------------------------------------------------------- 한 달 진행

        public List<SimEvent> Tick()
        {
            List<SimEvent> events = new List<SimEvent>();
            ComputeFields();

            // 1) 수요
            float workers = Population * Balance.WorkRatio;
            float jobs = JobsCommercial + JobsIndustrial;
            float boom = _eventMonthsLeft > 0 ? _eventDemand : 0f;

            DemandR = Clamp(((jobs + Balance.CommuteJobs) - workers) / (jobs + workers + 45f) * 1.4f + boom, -1f, 1f);
            float needC = Population * Balance.CommercialJobDemand;
            float needI = Population * Balance.IndustrialJobDemand + 6f;
            DemandC = Clamp((needC - JobsCommercial) / (needC + 22f) + boom, -1f, 1f);
            DemandI = Clamp((needI - JobsIndustrial) / (needI + 22f) + boom, -1f, 1f);

            // 불행한 도시는 사람이 떠난다. 표본이 작은 초기 도시에는 적용하지 않는다.
            if (Happiness < 30f && Population > 250)
            {
                DemandR -= 0.4f;
                DemandC -= 0.2f;
            }

            // 2) 전력·수도 수요
            int powerUse = 0, waterUse = 0;
            int total = W * H;
            for (int k = 0; k < total; k++)
            {
                Tile c = (Tile)TileCode[k];
                if (Balance.IsZone(c))
                {
                    powerUse += Balance.PowerPerTile[Level[k]];
                    waterUse += Balance.WaterPerTile[Level[k]];
                }
                else if (Balance.IsFacility(c) && c != Tile.PowerCoal && c != Tile.PowerSolar)
                {
                    powerUse += 12;
                    waterUse += 8;
                }
            }
            PowerUsage = powerUse;
            WaterUsage = waterUse;
            float powerRatio = powerUse > 0 ? Math.Min(1f, (float)PowerCapacity / powerUse) : 1f;
            float waterRatio = waterUse > 0 ? Math.Min(1f, (float)WaterCapacity / waterUse) : 1f;

            // 3) 구역 성장/쇠퇴
            int pop = 0, jobsC = 0, jobsI = 0;
            int developed = 0, blackout = 0, dry = 0;

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int k = Index(x, y);
                    Tile c = (Tile)TileCode[k];
                    if (!Balance.IsZone(c)) continue;

                    int lv = Level[k];
                    bool road = HasRoadAdjacent(x, y);
                    // 공급이 모자라면 타일별 안정 해시로 일부만 단전/단수시킨다.
                    bool powered = FieldPower[k] == 1 && Hash2(x, y, 11) < powerRatio;
                    bool watered = FieldWater[k] == 1 && Hash2(x, y, 22) < waterRatio;

                    if (lv > 0)
                    {
                        developed++;
                        if (!powered) blackout++;
                        if (!watered) dry++;
                    }

                    float demand = c == Tile.Residential ? DemandR
                                 : c == Tile.Commercial ? DemandC : DemandI;
                    float land = FieldLand[k];
                    bool serviced = road && powered && watered;

                    if (!serviced)
                    {
                        _streak[k] = (sbyte)Math.Max(-9, _streak[k] - 1);
                        if (_streak[k] <= -3 && lv > 0) { Level[k] = (byte)(lv - 1); _streak[k] = 0; Revision++; }
                    }
                    else
                    {
                        int next = Math.Min(3, lv + 1);
                        float need = Balance.LevelRequirement[next];
                        bool gate = true;
                        if (c == Tile.Commercial && next >= 3) gate = FieldEducation[k] > 7f;
                        if (c == Tile.Residential && next >= 3) gate = FieldHealth[k] > 6f;

                        if (lv < 3 && demand > 0.06f && land >= need && gate)
                        {
                            _streak[k] = (sbyte)Math.Min(9, _streak[k] + 1);
                            float chance = 0.16f + demand * 0.30f + (land - need) * 0.006f;
                            if (Hash2(x, y, Month) < chance) { Level[k] = (byte)(lv + 1); _streak[k] = 0; Revision++; }
                        }
                        else if (demand < -0.28f && lv > 0)
                        {
                            _streak[k] = (sbyte)Math.Max(-9, _streak[k] - 1);
                            if (_streak[k] <= -4) { Level[k] = (byte)(lv - 1); _streak[k] = 0; Revision++; }
                        }
                        else
                        {
                            _streak[k] = 0;
                        }
                    }

                    lv = Level[k];
                    if (c == Tile.Residential) pop += Balance.ResidentCap[lv];
                    else if (c == Tile.Commercial) jobsC += Balance.CommercialJobs[lv];
                    else jobsI += Balance.IndustrialJobs[lv];
                }
            }

            Population = pop;
            JobsCommercial = jobsC;
            JobsIndustrial = jobsI;

            // 4) 실업·범죄·행복
            workers = pop * Balance.WorkRatio;
            float jobsEffective = jobsC + jobsI + Balance.CommuteJobs;
            Unemployment = workers > 0f ? Clamp((workers - jobsEffective) / workers, 0f, 1f) : 0f;

            float policeSum = 0f;
            int cnt = 0;
            for (int k = 0; k < total; k++)
            {
                if (!Balance.IsZone((Tile)TileCode[k]) || Level[k] == 0) continue;
                policeSum += FieldPolice[k];
                cnt++;
            }
            float avgPolice = cnt > 0 ? policeSum / cnt : 0f;
            Crime = Clamp(4f + Unemployment * 22f - avgPolice * 0.5f, 0f, 30f);

            float blackoutRatio = developed > 0 ? (float)blackout / developed : 0f;
            float dryRatio = developed > 0 ? (float)dry / developed : 0f;

            float target = 55f
                + (AverageLand - 42f) * 0.55f
                - Unemployment * 55f
                - Math.Max(0f, Tax - 9f) * 3.2f
                + Math.Max(0f, 9f - Tax) * 1.4f
                - blackoutRatio * 30f
                - dryRatio * 24f
                - Crime * 0.6f
                - (Money < 0f ? 10f : 0f);
            Happiness += (Clamp(target, 0f, 100f) - Happiness) * 0.34f;

            // 5) 재정
            float efficiency = 0.55f + (Happiness / 100f) * 0.55f;
            float income = (pop * Balance.TaxResidential
                          + jobsC * Balance.TaxCommercial
                          + jobsI * Balance.TaxIndustrial) * Tax * efficiency;
            if (_eventMonthsLeft > 0) income *= _eventIncome;

            float expense = 0f;
            for (int k = 0; k < total; k++)
            {
                Tile c = (Tile)TileCode[k];
                if (c == Tile.Road) expense += 0.4f;
                else if (Balance.IsFacility(c))
                {
                    BuildItem item = Balance.ByCode(c);
                    if (item != null) expense += item.Upkeep;
                }
            }

            Income = income;
            Expense = expense;
            Money += income - expense;
            // 적자는 부채로 쌓이고 이자가 붙는다. 시설을 강제로 없애지는 않는다 —
            // 발전소가 사라지면 도시 전체가 정전되어 회복이 불가능해지기 때문이다.
            if (Money < 0f) Money += Money * Balance.DebtInterest;

            Month++;

            // 6) 사건
            if (_eventMonthsLeft > 0)
            {
                _eventMonthsLeft--;
                if (_eventMonthsLeft == 0)
                {
                    events.Add(new SimEvent("info", EventName == "호황" ? "호황이 끝났습니다." : "불황에서 벗어났습니다."));
                    EventName = null;
                    _eventDemand = 0f;
                    _eventIncome = 1f;
                }
            }
            else if (Month > 24 && Hash2(Month, 99, 5) < 0.035f)
            {
                bool good = Hash2(Month, 7, 6) > 0.45f;
                EventName = good ? "호황" : "불황";
                _eventDemand = good ? 0.22f : -0.26f;
                _eventIncome = good ? 1.12f : 0.86f;
                _eventMonthsLeft = (good ? 10 : 8) + (int)(Hash2(Month, 1, 7) * 8);
                events.Add(new SimEvent(good ? "good" : "warn",
                    good ? "호황 시작 — 수요와 세수가 늘어납니다." : "불황 시작 — 수요와 세수가 줄어듭니다."));
            }

            // 7) 화재 — 확률은 소방 사각지대 건물 수에 비례한다
            if (Month > 18)
            {
                List<int> victims = new List<int>();
                for (int k = 0; k < total; k++)
                {
                    if (Balance.IsZone((Tile)TileCode[k]) && Level[k] > 0 && FieldFire[k] < 3f) victims.Add(k);
                }
                float fireChance = Math.Min(0.07f, victims.Count * 0.0016f);
                if (victims.Count > 0 && Hash2(Month, 41, 9) < fireChance)
                {
                    int vk = victims[(int)(Hash2(Month, 17, 10) * victims.Count)];
                    TileCode[vk] = (byte)Tile.Rubble;
                    Level[vk] = 0;
                    _streak[vk] = 0;
                    Revision++;
                    events.Add(new SimEvent("bad", "화재 발생! 소방 사각지대의 건물이 전소했습니다."));
                }
            }

            // 8) 부채 경고
            if (Money < 0f && Month % 3 == 0)
            {
                events.Add(new SimEvent("bad", Money < -8000f
                    ? "부채 " + Math.Round(-Money).ToString("N0") + "원. 이자가 불어납니다 — 철거로 시설을 줄이세요."
                    : "적자입니다. 세율을 올리거나 도로·시설을 줄이세요."));
            }

            // 9) 단계 승급
            int tier = 0;
            for (int i = 0; i < Balance.Tiers.Length; i++)
            {
                if (pop >= Balance.Tiers[i].Pop) tier = i;
            }
            if (tier > TierIndex)
            {
                events.Add(new SimEvent("good", Balance.Tiers[tier].Name + " 승격! 새 시설이 해금되었습니다."));
            }
            TierIndex = tier;

            return events;
        }

        public string TierName { get { return Balance.Tiers[TierIndex].Name; } }

        public TileInfo Inspect(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            int k = Index(x, y);
            Tile c = (Tile)TileCode[k];

            string name;
            switch (c)
            {
                case Tile.Empty: name = "빈 땅"; break;
                case Tile.Water: name = "강"; break;
                case Tile.Tree: name = "숲"; break;
                case Tile.Road: name = "도로"; break;
                case Tile.Rubble: name = "잔해"; break;
                case Tile.Residential: name = "주거 구역"; break;
                case Tile.Commercial: name = "상업 구역"; break;
                case Tile.Industrial: name = "공업 구역"; break;
                default:
                    BuildItem it = Balance.ByCode(c);
                    name = it != null ? it.Name : "?";
                    break;
            }

            int lv = Level[k];
            int capacity = 0;
            if (c == Tile.Residential) capacity = Balance.ResidentCap[lv];
            else if (c == Tile.Commercial) capacity = Balance.CommercialJobs[lv];
            else if (c == Tile.Industrial) capacity = Balance.IndustrialJobs[lv];

            return new TileInfo
            {
                X = x, Y = y, Code = c, Name = name, Level = lv,
                HasRoad = HasRoadAdjacent(x, y),
                HasPower = FieldPower[k] == 1,
                HasWater = FieldWater[k] == 1,
                Land = (int)Math.Round(FieldLand[k]),
                Pollution = (int)Math.Round(FieldPollution[k]),
                Police = (int)Math.Round(FieldPolice[k]),
                Fire = (int)Math.Round(FieldFire[k]),
                Education = (int)Math.Round(FieldEducation[k]),
                Health = (int)Math.Round(FieldHealth[k]),
                Capacity = capacity
            };
        }
    }
}
