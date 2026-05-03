using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using Cosmosity.Pathfinders;
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

    private Var Self
    {
        get => _blackboard.Get<Var>("Self");
    }

    private Pathfinder Pathfinder
    {
        get => _blackboard.Get<Pathfinder>("Pathfinder");
    }

    private bool IsWalking
    {
        get => _blackboard.Get<bool>("IsWalking");
        set => _blackboard.Set("IsWalking", value);
    }
    protected override void OnEnter()
    {
        _timer = 0;
        IsWalking = false;
    }

    protected override void OnPhysicsUpdate(double delta)
    {
        if (!IsCurrentTargetInRange())
        {
            Var chaseTarget = CurrentAttackTarget;
            if (chaseTarget?.Stats == null || Pathfinder == null)
            {
                CurrentAttackTarget = null;
                RequestTransition("Idle");
                return;
            }

            Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
            Vector2I targetCell = Grid.WorldToGrid(chaseTarget.Stats.Position);
            var chasePath = Pathfinder.Run(selfCell, targetCell);
            if (chasePath == null || chasePath.Count == 0)
            {
                CurrentAttackTarget = null;
                RequestTransition("Idle");
                return;
            }

            // Resume movement through the usual Idle -> Move chain.
            Self.SetPath(chasePath);
            RequestTransition("Idle");
            return;
        }

        if (_timer == 0)
        {
            Attack();
            _timer = (int)(Stats.AttackFrameInterval / Stats.AttackSpeedMult);
        }
        else
        {
            _timer--;
        }
    }
    private void Attack()
    {
        if (CurrentAttackTarget == null || CurrentAttackTarget.Stats == null) return;

        GD.Print("Attack!");
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
