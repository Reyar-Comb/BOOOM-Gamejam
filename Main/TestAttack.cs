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
            DrawAttackCells(debugVar);
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
                AttackRange = CreateAttackRange(
                    new Vector2I(0, 1),
                    new Vector2I(0, 2),
                    new Vector2I(1, 1),
                    new Vector2I(-1, 1)),
                Position = Grid.GridToWorld(startPosition)
            }
        };

        VarManager.AddVar(var);
        var.SetPath(path);
        return var;
    }

    private void DrawAttackCells(DebugVar debugVar)
    {
        if (debugVar.Var.Stats.AttackRange == null)
        {
            return;
        }

        Vector2I originCell = Grid.WorldToGrid(debugVar.Var.Stats.Position);
        foreach (Vector2I cell in debugVar.Var.Stats.AttackRange.EnumerateTargetCells(originCell, debugVar.Var.Stats.Direction))
        {
            DrawAttackCell(cell, debugVar.Color);
        }
    }

    private void DrawAttackCell(Vector2I cell, Color color)
    {
        Vector2 cellCenter = Grid.GridToWorld(cell);
        Vector2 cellSize = Vector2.One * Grid.CellSize;
        Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);
        Color fillColor = color;
        fillColor.A = 0.15f;

        DrawRect(cellRect, fillColor);
        DrawRect(cellRect, color, false, 2.0f);
    }

    private static VarAttackRange CreateAttackRange(params Godot.Collections.Array<Vector2I> relativeCells)
    {
        return new VarAttackRange
        {
            RelativeCells = relativeCells
        };
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
