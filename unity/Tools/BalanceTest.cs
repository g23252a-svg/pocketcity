using System;
using PocketCity;

// 밸런스 검증 하네스.
// CitySim이 UnityEngine에 의존하지 않으므로 에디터 없이 그대로 돌릴 수 있다.
//
//   mcs -out:balance.exe ../PocketCity3D/Assets/Scripts/Core/*.cs BalanceTest.cs
//   mono balance.exe
//
// (또는 .NET SDK가 있다면 콘솔 프로젝트에 이 세 파일을 넣고 실행)

public static class BalanceTest
{
    private static CitySim _sim;

    public static int Main(string[] args)
    {
        _sim = new CitySim();
        int cx = (int)(Balance.W * 0.58f);
        int cy = Balance.H / 2;

        // 시작 인프라
        for (int i = -4; i <= 4; i++)
        {
            Put("road", cx + i, cy);
            Put("road", cx, cy + i);
        }
        Put("power_coal", cx + 1, cy + 1);
        Put("water_tower", cx - 1, cy + 1);

        Console.WriteLine("연  인구      자금      행복  실업  세율  수지    구역[L0/L1/L2/L3]  단계");
        Console.WriteLine(new string('-', 84));

        bool everNegative = false;
        int roadRing = 1;

        for (int m = 1; m <= 480; m++)
        {
            // --- 신중한 플레이어처럼 행동한다 (분기마다) ---
            if (m % 3 == 0)
            {
                if (_sim.Money > 4000f)
                {
                    if (_sim.PowerCapacity < _sim.PowerUsage * 1.3f)
                    {
                        PutNear(_sim.Population >= 3000 ? "power_solar" : "power_coal", cx, cy);
                    }
                    else if (_sim.WaterCapacity < _sim.WaterUsage * 1.3f)
                    {
                        PutNear("water_tower", cx, cy);
                    }
                    else
                    {
                        float dR = _sim.DemandR, dC = _sim.DemandC, dI = _sim.DemandI;
                        string best = (dR >= dC && dR >= dI) ? "zone_r" : (dC >= dI ? "zone_c" : "zone_i");
                        float bestD = Math.Max(dR, Math.Max(dC, dI));
                        if (bestD > 0.15f)
                        {
                            int placed = 0;
                            for (int attempt = 0; attempt < 8; attempt++)
                            {
                                if (PutNear(best, cx, cy)) placed++;
                            }
                            if (placed < 4)
                            {
                                ExtendRoads(cx, cy, roadRing);
                                roadRing++;
                            }
                        }
                    }
                }

                if (_sim.Money > 9000f && _sim.Income > _sim.Expense * 1.25f)
                {
                    if (_sim.Happiness < 55f) PutNear("park", cx, cy);
                    else if (_sim.Population > 900 && m % 24 == 0) PutNear("hospital", cx, cy);
                    else if (_sim.Population > 400 && m % 18 == 0) PutNear("school", cx, cy);
                    else if (m % 12 == 0) PutNear(_sim.Population > 1500 ? "plaza" : "park", cx, cy);
                    else if (m % 9 == 0) PutNear("fire", cx, cy);
                    else if (m % 15 == 0) PutNear("police", cx, cy);
                }

                if (_sim.Income < _sim.Expense && _sim.Tax < 13f) _sim.Tax += 0.5f;
                else if (_sim.Income > _sim.Expense * 1.6f && _sim.Tax > 7f) _sim.Tax -= 0.5f;
            }

            _sim.Tick();
            if (_sim.Money < 0f) everNegative = true;

            if (m == 12 || m == 36 || m == 60 || m == 120 || m == 240 || m == 360 || m == 480)
            {
                int[] lv = new int[4];
                int zones = 0;
                for (int k = 0; k < _sim.W * _sim.H; k++)
                {
                    if (!Balance.IsZone((Tile)_sim.TileCode[k])) continue;
                    zones++;
                    lv[_sim.Level[k]]++;
                }
                Console.WriteLine(
                    "{0,2}  {1,7:N0}  {2,9:N0}  {3,3:0}%  {4,3:0}%  {5,4:0.0}%  {6,6:N0}  {7,4}[{8}/{9}/{10}/{11}]  {12}",
                    m / 12, _sim.Population, _sim.Money, _sim.Happiness,
                    _sim.Unemployment * 100f, _sim.Tax, _sim.Income - _sim.Expense,
                    zones, lv[0], lv[1], lv[2], lv[3], _sim.TierName);
            }
        }

        Console.WriteLine();
        Console.WriteLine("한 번이라도 적자: " + (everNegative ? "있음" : "없음"));

        // 판정: 도시가 실제로 성장했고 최소한 Lv.2까지는 올라갔는가
        int level2Plus = 0;
        for (int k = 0; k < _sim.W * _sim.H; k++)
        {
            if (Balance.IsZone((Tile)_sim.TileCode[k]) && _sim.Level[k] >= 2) level2Plus++;
        }

        bool pass = _sim.Population > 500 && level2Plus > 0 && _sim.Happiness > 20f;
        Console.WriteLine("Lv.2 이상 건물: " + level2Plus);
        Console.WriteLine("판정: " + (pass ? "통과" : "실패"));
        return pass ? 0 : 1;
    }

    private static bool Put(string key, int x, int y)
    {
        return _sim.Build(key, x, y).Ok;
    }

    /// <summary>중심에서 가까운, 도로에 접한 빈 칸에 짓는다.</summary>
    private static bool PutNear(string key, int cx, int cy)
    {
        for (int r = 1; r < 30; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    int x = cx + dx, y = cy + dy;
                    if (!_sim.InBounds(x, y)) continue;
                    if (_sim.At(x, y) != Tile.Empty) continue;
                    if (!_sim.HasRoadAdjacent(x, y)) continue;
                    if (Put(key, x, y)) return true;
                }
            }
        }
        return false;
    }

    private static void ExtendRoads(int cx, int cy, int ring)
    {
        int r = ring * 4;
        for (int i = -r; i <= r; i++)
        {
            Put("road", cx + i, cy - r);
            Put("road", cx + i, cy + r);
            Put("road", cx - r, cy + i);
            Put("road", cx + r, cy + i);
        }
        for (int i = -r; i <= r; i += 4)
        {
            for (int j = -r; j <= r; j++)
            {
                Put("road", cx + i, cy + j);
                Put("road", cx + j, cy + i);
            }
        }
    }
}
