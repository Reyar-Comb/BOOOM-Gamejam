using System;
using System.Collections.Generic;

public class FriendOfClassesRuntime : ISkillRuntime
{
    private readonly SkillResource _resource;
    private readonly Dictionary<ulong, HashSet<int>> _visitedRegionsByVar = new();

    public FriendOfClassesRuntime(SkillResource resource)
    {
        _resource = resource;
    }

    public void OnWaveStarted()
    {
        _visitedRegionsByVar.Clear();
    }

    public void OnRegionEntered(RegionEnteredInfo info)
    {
        if (info == null || info.ToRegion == 0 || info.Var.Stats.VarTeam == VarStats.Team.Hostile)
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

    private void BoostStats(VarStats stats)
    {
        int previousMaxHealth = stats.MaxHealth;
        stats.MaxHealth = AddPositiveInt(stats.MaxHealth, HealthBonus);
        stats.CurrentHealth += stats.MaxHealth - previousMaxHealth;
        stats.AttackDamage = AddPositiveInt(stats.AttackDamage, AttackBonus);
        stats.Defense = AddPositiveInt(stats.Defense, DefenseBonus);
        stats.MoveSpeed = AddPositiveFloat(stats.MoveSpeed, MoveSpeedBonus);
        stats.AttackSpeedMult = AddPositiveFloat(stats.AttackSpeedMult, AttackSpeedBonus);
    }

    private static int AddPositiveInt(int value, int bonus)
    {
        return value <= 0 ? value : value + bonus;
    }

    private static float AddPositiveFloat(float value, float bonus)
    {
        return value <= 0f ? value : value + bonus;
    }

    private int HealthBonus => (int)_resource.GetValue("HealthBonus");
    private int AttackBonus => (int)_resource.GetValue("AttackBonus");
    private int DefenseBonus => (int)_resource.GetValue("DefenseBonus");
    private float MoveSpeedBonus => _resource.GetValue("MoveSpeedBonus");
    private float AttackSpeedBonus => _resource.GetValue("AttackSpeedBonus");
}
