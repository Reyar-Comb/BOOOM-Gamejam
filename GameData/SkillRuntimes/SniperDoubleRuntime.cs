using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class SniperDoubleRuntime : ISkillRuntime
{
    public IEnumerable<Vector2I> OnAttackRangeQuery(AttackRangeQueryInfo info, IEnumerable<Vector2I> rangeCells)
    {
        if (info?.Source?.Stats == null || info.Source.Stats.Type != VarStats.VarType.Double)
        {
            return rangeCells;
        }

        if (info.AttackRange == null || info.DetectRange == null)
        {
            return rangeCells;
        }

        IEnumerable<Vector2I> sameAxisDetectCells = info.DetectRange
            .EnumerateTargetCells(info.OriginCell, info.FacingDirection)
            .Where(cell => cell.X == info.OriginCell.X || cell.Y == info.OriginCell.Y);

        return rangeCells.Concat(sameAxisDetectCells);
    }
}
