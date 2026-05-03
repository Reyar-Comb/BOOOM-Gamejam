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

    private Var CurrentAttackTarget
    {
        get => _blackboard.Get<Var>("CurrentAttackTarget");
        set => _blackboard.Set("CurrentAttackTarget", value);
    }

    protected override void OnEnter()
    {
        _timer = 0;
    }

    protected override void OnPhysicsUpdate(double delta)
    {
        if (!IsCurrentTargetInRange())
        {
            CurrentAttackTarget = null;
            RequestTransition("Detect");
            return;
        }

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
    protected override void OnExit()
    {
        Stats.Direction = Stats.AttackDirection.ToFacingDirection();
    }

    private bool IsCurrentTargetInRange()
    {
        if (Stats.AttackRange == null || CurrentAttackTarget?.Stats == null)
        {
            return false;
        }

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        Vector2I targetCell = Grid.WorldToGrid(CurrentAttackTarget.Stats.Position);

        foreach (Vector2I attackCell in Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction))
        {
            if (attackCell != targetCell)
            {
                continue;
            }

            Stats.AttackDirection = (CurrentAttackTarget.Stats.Position - Stats.Position).Normalized();
            return true;
        }

        return false;
    }
}
