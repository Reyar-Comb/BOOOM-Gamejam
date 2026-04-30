using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
public partial class Var_AttackState : STNode
{
    public override string Name => "Attack";
    private int _timer = 0;
    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
    }
    protected override void OnEnter()
    {
        _timer = 0;
    }
    protected override void OnPhysicsUpdate(double delta)
    {
        if (_timer == 0)
        {
            GD.Print("Attack!");
            _timer = (int)(Stats.AttackFrameInterval / Stats.AttackSpeedMult);
        }
        else
        {
            _timer--;
        }
    }
}
