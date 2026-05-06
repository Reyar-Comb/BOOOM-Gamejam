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
    [Export] public float AttackSpeedMult;
    [Export] public int AttackFrameInterval;
    [Export] public float MoveSpeed;
    [Export] public VarRange AttackRange { get; set; } = null!;
    [Export] public VarRange DetectRange { get; set; } = null!;
    [Export] public int AttackDamage;

    public int CurrentHealth
    {
        get => field;
        set
        {
            field = value;
            if (field <= 0 && !_isDead)
            {
                EmitSignal(SignalName.OnDeath);
                _isDead = true;
                foreach (var tag in _tags)
                {
                    tag.OnDeath();
                }
            }
        }
    }
    public Vector2 Position;
    public Vector2 Direction;
    // public Vector2 AttackDirection;
    public Team VarTeam;
    private List<VarTag> _tags { get; set; } = new();
    private bool _isInitialized = false;
    private bool _isDead = false;
    public void AddTag(VarTag tag)
    {
        _tags.Add(tag);
    }
}
