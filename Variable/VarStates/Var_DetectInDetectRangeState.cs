using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
using Cosmosity.Pathfinders;
public partial class Var_DetectInDetectRangeState : STNode
{
    public override string Name => "DetectInDetectRange";

    private readonly List<int> _detectionOrder = new();
    private readonly HashSet<int> _seenEnemyIds = new();
    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
    }

    private IReadOnlyList<Var> Vars
    {
        get => _blackboard.Get<IReadOnlyList<Var>>("Vars");
    }

    private Var CurrentAttackTarget
    {
        get => _blackboard.Get<Var>("CurrentAttackTarget");
        set => _blackboard.Set("CurrentAttackTarget", value);
    }

    private Pathfinder Pathfinder
    {
        get => _blackboard.Get<Pathfinder>("Pathfinder");
    }

    private Var Self
    {
        get => _blackboard.Get<Var>("Self");
    }

    private MapData MapData => _blackboard.Get<MapData>("MapData");

    private GameData GameData => _blackboard.Get<GameData>("GameData");
    protected override void OnPhysicsUpdate(double delta)
    {
        TryGetEnemyInRange();
    }

    private void TryGetEnemyInRange()
    {
        if (Stats.DetectRange == null || Vars == null)
        {
            return;
        }

        var enemiesByCell = _blackboard.Get<IReadOnlyDictionary<Vector2I, Var>>("EnemiesByCell");
        if (enemiesByCell == null) return;

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);

        var found = new List<(Var enemy, Vector2I cell)>();
        var foundIds = new HashSet<int>();

        foreach (Vector2I targetCell in Stats.DetectRange.EnumerateTargetCells(selfCell, Stats.Direction))
        {
            if (MapData.GetRegion(targetCell.X, targetCell.Y) != MapData.GetRegion(selfCell.X, selfCell.Y)) continue;
            if (!enemiesByCell.TryGetValue(targetCell, out Var enemy)) continue;
            if (enemy == null || enemy.IsDead) continue;

            int id = (int)enemy.GetInstanceId();
            if (!foundIds.Contains(id))
            {
                found.Add((enemy, targetCell));
                foundIds.Add(id);
            }
        }

        foreach (var (enemy, cell) in found)
        {
            int id = (int)enemy.GetInstanceId();
            if (_seenEnemyIds.Contains(id)) continue;

            _seenEnemyIds.Add(id);
            _detectionOrder.Add(id);
            DetectInfo info = new()
            {
                Detector = Self,
                DetectedVar = enemy
            };
            if (Self.Stats.VarTeam == VarStats.Team.Friendly)
            {
                GameData.SkillManager.OnDetected(info);
            }
            Self.EmitSignal(Var.SignalName.OnDetected, info);
        }

        _detectionOrder.RemoveAll(id => !foundIds.Contains(id));

        if (Stats.Type == VarStats.VarType.Bool)
        {
            CurrentAttackTarget = null;
            return;
        }

        var currentTarget = CurrentAttackTarget;
        if (currentTarget != null)
        {
            if (currentTarget.IsDead || !foundIds.Contains((int)currentTarget.GetInstanceId()))
            {
                CurrentAttackTarget = null;
            }
        }
        if (CurrentAttackTarget == null)
        {
            foreach (int id in _detectionOrder)
            {
                var tuple = found.Find(t => (int)t.enemy.GetInstanceId() == id);
                if (tuple.enemy != null)
                {
                    CurrentAttackTarget = tuple.enemy;
                    Self.SetPath(Pathfinder.Run(selfCell, tuple.cell, MapData.GetRegion(selfCell.X, selfCell.Y)));
                    return;
                }
            }
        }
    }
}
