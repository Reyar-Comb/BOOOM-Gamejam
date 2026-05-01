using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
public partial class Var_DetectState : STNode
{
    public override string Name => "Detect";
    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
    }
    private IReadOnlyList<Var> Vars
    {
        get => _blackboard.Get<IReadOnlyList<Var>>("Vars");
    }
    protected override void OnPhysicsUpdate(double delta)
    {
        if (IsEnemyInRange())
        {
            RequestTransition("Attack");
        }
    }
    private bool IsEnemyInRange()
    {
        foreach (var var in Vars)
        {
            if (var == null || var.Stats == null) continue;
            if (var.Stats == Stats) continue;
            if (Stats.Position.DistanceSquaredTo(var.Stats.Position) <= Stats.AttackRange * Stats.AttackRange)
            {
                return true;
            }
        }
        return false;
    }
}
