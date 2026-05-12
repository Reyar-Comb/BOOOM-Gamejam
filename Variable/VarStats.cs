using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class VarStats : Resource
{
    public enum Team
    {
        Friendly,
        Hostile,
        Neutral
    }

    public enum VarType
    {
        Int,
        Float,
        Long,
        Double,
        LongDouble,
        Char,
        Bool,
        Dummy
    }

    public VarType Type { get; set; }

    public string Name { get; set; }

    [Signal] public delegate void OnDeathEventHandler();
    [Export]
    public int MaxHealth
    {
        get => field;
        set
        {
            field = value;
            if (!_isInitialized)
            {
                CurrentHealth = MaxHealth;
                _isInitialized = true;
            }
            CurrentHealth = Math.Min(CurrentHealth, value);
        }
    }
    [Export] public float AttackSpeedMult { get; set; }
    [Export] public int AttackFrameInterval { get; set; }
    [Export] public float MoveSpeed { get; set; }
    [Export] public int Defense { get; set; }
    [Export] public VarRange AttackRange { get; set; } = null!;
    [Export] public VarRange DetectRange { get; set; } = null!;
    [Export] public int AttackDamage { get; set; }
    [Export] public int TokenCost { get; set; }
    public int CurrentHealth
    {
        get => field;
        set
        {
            field = value;
            if (field <= 0 && !IsDead)
            {
                EmitSignal(SignalName.OnDeath);
                IsDead = true;
                foreach (var tag in _tags)
                {
                    tag.OnDeath();
                }
            }
        }
    }
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; set; }
    // public Vector2 AttackDirection;
    public Team VarTeam { get; set; }
    public bool IsDead { get; set; } = false;
    private List<VarTag> _tags { get; set; } = new();
    private bool _isInitialized = false;
    public void AddTag(VarTag tag)
    {
        _tags.Add(tag);
    }
    public void SetGridPosition(Vector2I gridPos)
    {
        Position = Grid.GridToWorld(gridPos);
    }
}
