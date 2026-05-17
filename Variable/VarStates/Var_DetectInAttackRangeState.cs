using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
using Cosmosity.Pathfinders;
public partial class Var_DetectInAttackRange : STNode
{
    public override string Name => "DetectInAttackRange";

    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
    }

    private IReadOnlyList<Var> Vars
    {
        get => _blackboard.Get<IReadOnlyList<Var>>("Vars");
    }

    private IReadOnlyDictionary<Vector2I, Var> EnemyVarsByCell
    {
        get
        {
            string key = Stats.VarTeam == VarStats.Team.Friendly
                ? "HostileVarsByCell"
                : "FriendlyVarsByCell";
            return _blackboard.Get<IReadOnlyDictionary<Vector2I, Var>>(key);
        }
    }

    private GameData GameData => _blackboard.Get<GameData>("GameData");

    private Var CurrentAttackTarget
    {
        get => _blackboard.Get<Var>("CurrentAttackTarget");
        set => _blackboard.Set("CurrentAttackTarget", value);
    }

    private Var Self
    {
        get => _blackboard.Get<Var>("Self");
    }

    private MapData MapData => _blackboard.Get<MapData>("MapData");
    protected override void OnPhysicsUpdate(double delta)
    {
        TryGetEnemyInRange();
    }

    private void TryGetEnemyInRange()
    {
        if (Stats.AttackRange == null || Vars == null || GameData == null)
        {
            return;
        }

        IReadOnlyDictionary<Vector2I, Var> enemiesByCell = EnemyVarsByCell;
        _blackboard.Set("EnemiesByCell", enemiesByCell);
        if (Stats.Type == VarStats.VarType.Bool)
        {
            CurrentAttackTarget = null;
            return;
        }

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        AttackRangeQueryInfo queryInfo = new()
        {
            Source = Self,
            OriginCell = selfCell,
            FacingDirection = Stats.Direction,
            AttackRange = Stats.AttackRange,
            DetectRange = Stats.DetectRange
        };

        IEnumerable<Vector2I> attackCells = GameData.SkillManager.OnAttackRangeQuery(
            queryInfo,
            Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction));

        foreach (Vector2I targetCell in attackCells)
        {
            if (MapData.GetRegion(targetCell.X, targetCell.Y) != MapData.GetRegion(selfCell.X, selfCell.Y))
            {
                continue;
            }
            if (enemiesByCell.TryGetValue(targetCell, out Var enemy))
            {
                CurrentAttackTarget = enemy;
                RequestTransition("Attack");
            }
        }
    }
}
