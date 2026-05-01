using Godot;
using System.Collections.Generic;

public partial class TestAttack : Node2D
{
    [Export] public VarManager VarManager { get; set; } = null!;

    private readonly List<DebugVar> _debugVars = new();

    public override void _Ready()
    {
        _debugVars.Add(new DebugVar(
            CreateVar(
            new Vector2I(2, 2),
            new List<Vector2I>
            {
                new(2, 2),
                new(4, 2),
                new(5, 2)
            }),
            Colors.OrangeRed));

        _debugVars.Add(new DebugVar(
            CreateVar(
            new Vector2I(10, 2),
            new List<Vector2I>
            {
                new(10, 2),
                new(8, 2),
                new(6, 2)
            }),
            Colors.DeepSkyBlue));
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (DebugVar debugVar in _debugVars)
        {
            if (debugVar.Var?.Stats == null)
            {
                continue;
            }

            DrawCircle(debugVar.Var.Stats.Position, 20.0f, debugVar.Color);
            DrawArc(debugVar.Var.Stats.Position, debugVar.Var.Stats.AttackRange, 0.0f, Mathf.Tau, 48, debugVar.Color, 2.0f);
        }
    }

    private Var CreateVar(Vector2I startPosition, List<Vector2I> path)
    {
        Var var = new()
        {
            Stats = new VarStats
            {
                MaxHealth = 100,
                AttackSpeedMult = 1.0f,
                AttackFrameInterval = 20,
                MoveSpeed = 120.0f,
                AttackRange = 80.0f,
                Position = Grid.GridToWorld(startPosition)
            }
        };

        VarManager.AddVar(var);
        var.SetPath(path);
        return var;
    }

    private sealed class DebugVar
    {
        public DebugVar(Var var, Color color)
        {
            Var = var;
            Color = color;
        }

        public Var Var { get; }
        public Color Color { get; }
    }
}
