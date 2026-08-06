using System.Collections.Generic;

namespace PocketCity
{
    /// <summary>타일 종류. 10 이상은 플레이어가 짓는 시설이다.</summary>
    public enum Tile : byte
    {
        Empty = 0,
        Water = 1,
        Tree = 2,
        Road = 3,
        Residential = 4,
        Commercial = 5,
        Industrial = 6,
        Rubble = 7,
        PowerCoal = 10,
        PowerSolar = 11,
        WaterTower = 12,
        Park = 13,
        Plaza = 14,
        Police = 15,
        Fire = 16,
        Hospital = 17,
        School = 18
    }

    public enum ServiceKind { None, Police, Fire, Education, Health }

    public class BuildItem
    {
        public string Key;
        public string Name;
        public string Category;
        public string Desc;
        public Tile Code;
        public int Cost;
        public float Upkeep;
        public int UnlockPop;
        public int Power;
        public int Water;
        public int Radius;
        public float Pollution;
        public float LandBonus;
        public ServiceKind Service = ServiceKind.None;
    }

    public class Tier
    {
        public int Pop;
        public string Name;
        public Tier(int pop, string name) { Pop = pop; Name = name; }
    }

    /// <summary>
    /// 밸런스 상수와 건물 카탈로그. 난이도를 바꾸려면 이 파일만 건드리면 된다.
    /// 값들은 웹 프로토타입에서 40년치 시뮬레이션을 돌려 검증한 것을 그대로 옮겼다.
    /// </summary>
    public static class Balance
    {
        public const int W = 64;
        public const int H = 64;
        public const int StartMoney = 12000;
        public const float StartTax = 9f;
        public const float TickSeconds = 2.6f;   // 1개월

        // 구역 1칸이 레벨별로 담는 용량
        public static readonly int[] ResidentCap = { 0, 14, 44, 110 };
        public static readonly int[] CommercialJobs = { 0, 6, 18, 44 };
        public static readonly int[] IndustrialJobs = { 0, 10, 26, 60 };

        // 레벨업에 필요한 지가. 빈 땅 기본 지가가 30이므로 Lv.1은 여유롭고,
        // Lv.2 이상은 공원·서비스 투자가 있어야 도달한다.
        public static readonly float[] LevelRequirement = { 0f, 14f, 40f, 66f };

        public static readonly int[] PowerPerTile = { 0, 6, 16, 34 };
        public static readonly int[] WaterPerTile = { 0, 5, 13, 28 };

        public const float WorkRatio = 0.52f;
        public const int CommuteJobs = 45;      // 도시 밖 통근 보정. 없으면 초기 실업률이 100%가 된다.
        public const float CommercialJobDemand = 0.13f;
        public const float IndustrialJobDemand = 0.17f;

        public const float TaxResidential = 0.070f;
        public const float TaxCommercial = 0.115f;
        public const float TaxIndustrial = 0.100f;

        public const float IndustrialPollution = 2.2f;
        public const int PollutionRadius = 7;

        public const float BaseLandValue = 30f;
        public const float DebtInterest = 0.008f;

        public static readonly Tier[] Tiers =
        {
            new Tier(0, "마을"),
            new Tier(500, "읍"),
            new Tier(2000, "시"),
            new Tier(10000, "대도시"),
            new Tier(50000, "광역시")
        };

        private static readonly List<BuildItem> _items = new List<BuildItem>();
        private static readonly Dictionary<string, BuildItem> _byKey = new Dictionary<string, BuildItem>();
        private static readonly Dictionary<Tile, BuildItem> _byCode = new Dictionary<Tile, BuildItem>();

        public const string BulldozeKey = "bulldoze";
        public const int BulldozeCost = 4;

        static Balance()
        {
            Add(new BuildItem
            {
                Key = "road", Name = "도로", Category = "도로", Code = Tile.Road,
                Cost = 10, Upkeep = 0.4f,
                Desc = "모든 건물은 도로에 닿아야 작동합니다."
            });

            Add(new BuildItem
            {
                Key = "zone_r", Name = "주거", Category = "구역", Code = Tile.Residential,
                Cost = 20, Upkeep = 0f,
                Desc = "시민이 들어와 삽니다. 인구의 원천."
            });
            Add(new BuildItem
            {
                Key = "zone_c", Name = "상업", Category = "구역", Code = Tile.Commercial,
                Cost = 25, Upkeep = 0f,
                Desc = "일자리와 세수를 만듭니다. 인구가 있어야 수요가 생깁니다."
            });
            Add(new BuildItem
            {
                Key = "zone_i", Name = "공업", Category = "구역", Code = Tile.Industrial,
                Cost = 25, Upkeep = 0f,
                Desc = "일자리가 많지만 오염이 심합니다. 주거와 떨어뜨리세요."
            });

            Add(new BuildItem
            {
                Key = "power_coal", Name = "화력발전", Category = "전력·수도", Code = Tile.PowerCoal,
                Cost = 900, Upkeep = 45f, Power = 900, Pollution = 6f, Radius = 14,
                Desc = "전력 900. 주변 오염이 큽니다."
            });
            Add(new BuildItem
            {
                Key = "power_solar", Name = "태양광", Category = "전력·수도", Code = Tile.PowerSolar,
                Cost = 2400, Upkeep = 35f, Power = 600, Radius = 14, UnlockPop = 3000,
                Desc = "전력 600, 무공해. 인구 3,000명부터."
            });
            Add(new BuildItem
            {
                Key = "water_tower", Name = "급수탑", Category = "전력·수도", Code = Tile.WaterTower,
                Cost = 320, Upkeep = 16f, Water = 700, Radius = 12,
                Desc = "수도 700 공급."
            });

            Add(new BuildItem
            {
                Key = "park", Name = "공원", Category = "공원", Code = Tile.Park,
                Cost = 130, Upkeep = 8f, LandBonus = 22f, Radius = 6,
                Desc = "주변 지가와 행복도를 올립니다."
            });
            Add(new BuildItem
            {
                Key = "plaza", Name = "광장", Category = "공원", Code = Tile.Plaza,
                Cost = 600, Upkeep = 26f, LandBonus = 40f, Radius = 10, UnlockPop = 1500,
                Desc = "넓은 범위의 지가를 크게 올립니다. 인구 1,500명부터."
            });

            Add(new BuildItem
            {
                Key = "police", Name = "경찰서", Category = "서비스", Code = Tile.Police,
                Cost = 520, Upkeep = 32f, Radius = 11, Service = ServiceKind.Police,
                Desc = "범죄를 낮춰 지가를 지킵니다."
            });
            Add(new BuildItem
            {
                Key = "fire", Name = "소방서", Category = "서비스", Code = Tile.Fire,
                Cost = 520, Upkeep = 32f, Radius = 11, Service = ServiceKind.Fire,
                Desc = "화재 발생을 막습니다. 없으면 건물이 불탑니다."
            });
            Add(new BuildItem
            {
                Key = "school", Name = "학교", Category = "서비스", Code = Tile.School,
                Cost = 760, Upkeep = 52f, Radius = 12, Service = ServiceKind.Education, UnlockPop = 400,
                Desc = "교육은 상업 발전의 조건입니다. 인구 400명부터."
            });
            Add(new BuildItem
            {
                Key = "hospital", Name = "병원", Category = "서비스", Code = Tile.Hospital,
                Cost = 1000, Upkeep = 64f, Radius = 13, Service = ServiceKind.Health, UnlockPop = 900,
                Desc = "건강과 행복도를 올립니다. 인구 900명부터."
            });
        }

        private static void Add(BuildItem item)
        {
            _items.Add(item);
            _byKey[item.Key] = item;
            _byCode[item.Code] = item;
        }

        public static IList<BuildItem> All { get { return _items; } }

        public static BuildItem ByKey(string key)
        {
            BuildItem item;
            return _byKey.TryGetValue(key, out item) ? item : null;
        }

        public static BuildItem ByCode(Tile code)
        {
            BuildItem item;
            return _byCode.TryGetValue(code, out item) ? item : null;
        }

        public static readonly string[] Categories = { "구역", "도로", "전력·수도", "서비스", "공원" };

        public static bool IsZone(Tile t)
        {
            return t == Tile.Residential || t == Tile.Commercial || t == Tile.Industrial;
        }

        public static bool IsFacility(Tile t)
        {
            return (byte)t >= 10;
        }
    }
}
