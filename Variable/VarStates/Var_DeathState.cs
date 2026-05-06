using Godot;
using System;
using StarlightStateTree;
using StarlightBT.Data;
public partial class Var_DeathState : STNode
{
    public override string Name => "Death";
    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
    }

    private Var CurrentAttackTarget
    {
        get => _blackboard.Get<Var>("CurrentAttackTarget");
        set => _blackboard.Set("CurrentAttackTarget", value);
    }

    private Var Self
    {
        get => _blackboard.Get<Var>("Self");
        set => _blackboard.Set("Self", value);
    }

    protected override void OnEnter()
    {
        GD.Print("Var has died.");
    }
}
