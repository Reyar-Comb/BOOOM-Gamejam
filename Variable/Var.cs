using Godot;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
using System;

public partial class Var : RefCounted, ICleanable
{
    public VarStats Stats { get; set; }
    public bool IsDead { get; set; } = false;
    protected STRoot _stateTree = null!;
    protected Blackboard _blackboard = null!;
    private bool _isInitialized = false;
    private Callable _onDeathCallable;
    private bool _hasOnDeathCallable = false;
    private bool _isCleanedUp = false;

    private readonly HashSet<Var> _attackers = new();




    [Signal] 
    public delegate void OnDetectedEventHandler(Var detectedVar);
    
    [Signal] 
    public delegate void OnAttackedEventHandler(int damage, Var source);

    [Signal]
    public delegate void OnOutOfDetectEventHandler(Var target);


    public void Initialize(Blackboard parentBlackboard)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _blackboard = new()
        {
            ParentBlackboard = parentBlackboard
        };
        SetupStateTree();

        _onDeathCallable = Callable.From(OnDeath);
        Stats.Connect(VarStats.SignalName.OnDeath, _onDeathCallable);
        _hasOnDeathCallable = true;
    }
    public void Cleanup()
    {
        if (_isCleanedUp) return;
        _isCleanedUp = true;

        DisconnectOnDeath();
        Stats = null;
        _stateTree?.Cleanup();
        _stateTree = null;
        _blackboard?.Cleanup();
        _blackboard = null;
    }
    private void OnDeath()
    {
        _stateTree?.ForceTransition("Death");
    }
    private void DisconnectOnDeath()
    {
        if (Stats == null || !_hasOnDeathCallable)
        {
            return;
        }

        if (Stats.IsConnected(VarStats.SignalName.OnDeath, _onDeathCallable))
        {
            Stats.Disconnect(VarStats.SignalName.OnDeath, _onDeathCallable);
        }

        _onDeathCallable = default;
        _hasOnDeathCallable = false;
    }
    public void SetPath(List<Vector2I> path)
    {
        if (path == null || path.Count == 0) return;

        InitializeFacingFromPath(path);
        _blackboard.Set("CurrentPath", path);
        _blackboard.Set("CurrentPathIndex", 0);
        _blackboard.Set("HasPendingMove", true);
    }
    public void PhysicsUpdate(double delta)
    {
        _stateTree.PhysicsUpdate(delta);
    }
    public void FrameUpdate(double delta)
    {
        _stateTree.FrameUpdate(delta);
    }
    public void ReceiveDamage(AttackInfo atkInfo)
    {
        int finalDamage = atkInfo.Damage;

        //directionFactor is in [1, 2], 1 if attacked from front, 2 if from back.
        Vector2 facingDirection = Stats.Direction;
        float directionFactor = atkInfo.GetFromDirection(Stats.Position).Dot(facingDirection) * -0.5f + 1.5f;
        finalDamage = (int)(finalDamage * directionFactor);

        if (!_attackers.Contains(atkInfo.Source))
        {
            _attackers.Add(atkInfo.Source);
            EmitSignal(SignalName.OnAttacked, finalDamage, atkInfo.Source);
        }

        Stats.CurrentHealth -= finalDamage;
    }
    protected virtual void SetupStateTree()
    {
        var idleState = new Var_IdleState();
        var moveState = new Var_MoveState();
        var attackState = new Var_AttackState();
        var detectOutOfRangeState = new Var_DetectOutOfRangeState();
        var detectInDetectRangeState = new Var_DetectInDetectRangeState();
        var detectInAttackRangeState = new Var_DetectInAttackRange();
        var deathState = new Var_DeathState();

        _stateTree = new STRoot
        {
            InitialState = "Idle",
            AllowRepeatedEnterAndExit = false
        };
        _stateTree.AddChild(detectInAttackRangeState);
        detectInAttackRangeState.AddChild(detectInDetectRangeState);
        detectInDetectRangeState.AddChild(idleState);
        detectInDetectRangeState.AddChild(moveState);

        _stateTree.AddChild(detectOutOfRangeState);
        detectOutOfRangeState.AddChild(attackState);

        _stateTree.AddChild(deathState);

        _blackboard.Set("Stats", Stats);
        _blackboard.Set("CurrentPath", new List<Vector2I>());
        _blackboard.Set("IsWalking", false);
        _blackboard.Set("HasPendingMove", false);
        _blackboard.Set("CurrentPathIndex", 0);
        _blackboard.Set("CurrentAttackTarget", (Var)null);
        _blackboard.Set("Self", this);
        _blackboard.Set("IsAttacked", false);

        _stateTree.Initialize(_blackboard);
    }
    private void InitializeFacingFromPath(List<Vector2I> path)
    {
        if (Stats == null)
        {
            return;
        }

        Vector2 currentPosition = Stats.Position;

        foreach (Vector2I gridPoint in path)
        {
            Vector2 nextPosition = Grid.GridToWorld(gridPoint);
            Vector2 nextDirection = nextPosition - currentPosition;

            if (nextDirection.LengthSquared() <= MathConstants.EpsilonSquared)
            {
                currentPosition = nextPosition;
                continue;
            }

            Stats.Direction = nextDirection.ToFacingDirection();
            return;
        }
    }
}
