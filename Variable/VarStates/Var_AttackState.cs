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

    private bool IsWalking
    {
        get => _blackboard.Get<bool>("IsWalking");
        set => _blackboard.Set("IsWalking", value);
    }
    protected override void OnEnter()
    {
        _timer = 0;
    }

    protected override void OnPhysicsUpdate(double delta)
    {
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
        if (CurrentAttackTarget == null || CurrentAttackTarget.IsDead)
        {
            CurrentAttackTarget = null;
            return;
        }

        AttackInfo atkInfo = new()
        {
            Damage = Stats.AttackDamage,
            Source = Self
        };
        CurrentAttackTarget.ReceiveDamage(atkInfo);
    }
    protected override void OnExit()
    {
        Stats.Direction = Stats.Direction.ToFacingDirection();
    }
}
