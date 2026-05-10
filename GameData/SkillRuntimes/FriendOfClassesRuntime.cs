using System;
using System.Collections.Generic;

public class FriendOfClassesRuntime : ISkillRuntime
{
    private const float StatMultiplier = 1.1f;

    private readonly Dictionary<ulong, HashSet<int>> _visitedRegionsByVar = new();

    public void OnWaveStarted()
    {
        _visitedRegionsByVar.Clear();
    }

    public void OnRegionEntered(RegionEnteredInfo info)
    {
        if (info == null || info.ToRegion == 0)
        {
            return;
        }

        Var var = info.Var;
        VarStats stats = var?.Stats;
        if (stats == null)
        {
            return;
        }

        HashSet<int> visitedRegions = GetVisitedRegions(var);
        if (info.FromRegion != 0)
        {
            visitedRegions.Add(info.FromRegion);
        }

        if (!visitedRegions.Add(info.ToRegion))
        {
            return;
        }

        BoostStats(stats);
    }

    private HashSet<int> GetVisitedRegions(Var var)
    {
        ulong id = var.GetInstanceId();
        if (!_visitedRegionsByVar.TryGetValue(id, out HashSet<int> visitedRegions))
        {
            visitedRegions = new HashSet<int>();
            _visitedRegionsByVar[id] = visitedRegions;
        }

        return visitedRegions;
    }

    private static void BoostStats(VarStats stats)
    {
        int previousMaxHealth = stats.MaxHealth;
        stats.MaxHealth = BoostPositiveInt(stats.MaxHealth);
        stats.CurrentHealth += stats.MaxHealth - previousMaxHealth;
        stats.AttackDamage = BoostPositiveInt(stats.AttackDamage);
        stats.Defense = BoostPositiveInt(stats.Defense);
        stats.MoveSpeed = BoostPositiveFloat(stats.MoveSpeed);
        stats.AttackSpeedMult = BoostPositiveFloat(stats.AttackSpeedMult);
    }

    private static int BoostPositiveInt(int value)
    {
        if (value <= 0)
        {
            return value;
        }

        return Math.Max(value + 1, (int)Math.Ceiling(value * StatMultiplier));
    }

    private static float BoostPositiveFloat(float value)
    {
        return value <= 0f ? value : value * StatMultiplier;
    }
}
