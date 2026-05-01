using Godot;
using System;

public enum BattleState
{
    Running,
    Paused,
    Choice,
    Replay,
    End
}

public readonly struct BattleTickContext
{
    public long Tick { get; }
    public double TickDelta { get; }
    public BattleManager Manager { get; }

    public BattleTickContext(long tick, double tickDelta, BattleManager manager)
    {
        Tick = tick;
        TickDelta = tickDelta;
        Manager = manager;
    }
}

public partial class BattleManager : Node
{
    [Export] public int TickRate = 20;

    [Export] public float TickScale = 1f;

    public long CurrentTick { get; private set; } = 0;

    public BattleState State { get; private set; } = BattleState.Running;

    public double TickInterval => 1.0 / TickRate;

    private double _accumulator = 0.0;

    private bool _isTicking = false;

    // TODO: Integrate varManager here

    public override void _Process(double delta)
    {
        if (State != BattleState.Running) return;

        _accumulator += delta * TickScale;
        
        while (_accumulator >= TickInterval)
        {
            Tick();
            _accumulator -= TickInterval;
        }
    }

    private void Tick()
    {
        CurrentTick++;
        _isTicking = true;

        var context = new BattleTickContext(CurrentTick, TickInterval, this);
    }
}