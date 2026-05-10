using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class BerserkLongRuntime : ISkillRuntime
{
    public void OnBeforeAttack(AttackInfo info)
    {
        if (IsBerserk(info?.Source, info))
        {
            info.Damage *= 2;
        }

        if (IsBerserk(info?.Target, info))
        {
            info.Defense /= 2;
        }
    }

    private static bool IsBerserk(Var var, AttackInfo info)
    {
        if (var?.Stats == null || var.Stats.Type != VarStats.VarType.Long)
        {
            return false;
        }

        if (var.Stats.DetectRange == null || info?.Vars == null || info.MapData == null)
        {
            return false;
        }

        return CountEnemiesInDetectRange(var, info.Vars, info.MapData) == 1;
    }

    private static int CountEnemiesInDetectRange(Var detector, IReadOnlyList<Var> vars, MapData mapData)
    {
        VarStats stats = detector.Stats;
        Vector2I detectorCell = Grid.WorldToGrid(stats.Position);
        int detectorRegion = mapData.GetRegion(detectorCell.X, detectorCell.Y);

        HashSet<Vector2I> detectCells = stats.DetectRange
            .EnumerateTargetCells(detectorCell, stats.Direction)
            .Where(cell => mapData.GetRegion(cell.X, cell.Y) == detectorRegion)
            .ToHashSet();

        int count = 0;
        foreach (Var candidate in vars)
        {
            if (candidate == null || candidate == detector || candidate.IsDead || candidate.Stats == null)
            {
                continue;
            }

            if (candidate.Stats.VarTeam == stats.VarTeam)
            {
                continue;
            }

            Vector2I candidateCell = Grid.WorldToGrid(candidate.Stats.Position);
            if (!detectCells.Contains(candidateCell))
            {
                continue;
            }

            count++;
            if (count > 1)
            {
                return count;
            }
        }

        return count;
    }
}
