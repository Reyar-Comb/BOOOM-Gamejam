using Godot;
using System.Collections.Generic;

public partial class TestChaseOutOfRange : Node2D
{
    [Export] public VarManager VarManager { get; set; } = null!;
    [Export] public BattleManager BattleManager { get; set; } = null!;

    private const long EscapeTick = 12;
    private static readonly Vector2I AttackerStartCell = new(5, 4);
    private static readonly Vector2I RunnerStartCell = new(5, 5);
    private static readonly Vector2I RunnerEscapeCell = new(5, 6);

    private readonly List<DebugVar> _debugVars = new();

    private Var _attacker = null!;
    private Var _runner = null!;
    private Label _infoLabel = null!;
    private bool _escapeIssued = false;

    public override void _Ready()
    {
        _infoLabel = GetNode<Label>("CanvasLayer/InfoLabel");

        _attacker = CreateVar(
            AttackerStartCell,
            Colors.OrangeRed,
            moveSpeed: 130.0f,
            attackRange: CreateAttackRange(new Vector2I(0, 1)),
            detectRange: CreateAttackRange(new Vector2I(0, 1), new Vector2I(0, 2)));
        _runner = CreateVar(
            RunnerStartCell,
            Colors.DeepSkyBlue,
            moveSpeed: 80.0f,
            attackRange: CreateAttackRange(),
            detectRange: CreateAttackRange());

        UpdateInfoLabel();
    }

    public override void _Process(double delta)
    {
        if (!_escapeIssued && BattleManager != null && BattleManager.CurrentTick >= EscapeTick)
        {
            _runner.SetPath(new List<Vector2I> { RunnerEscapeCell });
            _escapeIssued = true;
        }

        UpdateInfoLabel();
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawGrid(new Rect2I(3, 2, 5, 6));

        foreach (DebugVar debugVar in _debugVars)
        {
            if (debugVar.Var?.Stats == null)
            {
                continue;
            }

            DrawCircle(debugVar.Var.Stats.Position, 18.0f, debugVar.Color);
        }

        DrawAttackCells(_attacker, Colors.OrangeRed);
        DrawTargetLink();
    }

    private Var CreateVar(Vector2I startCell, Color color, float moveSpeed, VarRange attackRange, VarRange detectRange = null)
    {
        Var var = new()
        {
            Stats = new VarStats
            {
                MaxHealth = 100,
                AttackSpeedMult = 1.0f,
                AttackFrameInterval = 20,
                MoveSpeed = moveSpeed,
                AttackRange = attackRange,
                DetectRange = detectRange ?? attackRange,
                Position = Grid.GridToWorld(startCell)
            }
        };

        VarManager.AddVar(var);
        _debugVars.Add(new DebugVar(var, color));
        return var;
    }

    private void DrawGrid(Rect2I area)
    {
        for (int x = area.Position.X; x < area.End.X; x++)
        {
            for (int y = area.Position.Y; y < area.End.Y; y++)
            {
                Vector2 topLeft = Grid.GridToWorld(new Vector2I(x, y)) - Vector2.One * Grid.CellSize / 2.0f;
                DrawRect(new Rect2(topLeft, Vector2.One * Grid.CellSize), new Color(1, 1, 1, 0.06f), false, 1.0f);
            }
        }
    }

    private void DrawAttackCells(Var var, Color color)
    {
        if (var?.Stats?.AttackRange == null)
        {
            return;
        }

        Vector2I originCell = Grid.WorldToGrid(var.Stats.Position);
        foreach (Vector2I cell in var.Stats.AttackRange.EnumerateTargetCells(originCell, var.Stats.Direction.ToVector2()))
        {
            Vector2 cellCenter = Grid.GridToWorld(cell);
            Vector2 cellSize = Vector2.One * Grid.CellSize;
            Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);
            Color fillColor = color;
            fillColor.A = 0.15f;

            DrawRect(cellRect, fillColor);
            DrawRect(cellRect, color, false, 2.0f);
        }
    }

    private void DrawTargetLink()
    {
        if (_attacker?.Stats == null || _runner?.Stats == null)
        {
            return;
        }

        DrawLine(_attacker.Stats.Position, _runner.Stats.Position, new Color(1, 1, 1, 0.35f), 2.0f);
    }

    private void UpdateInfoLabel()
    {
        if (_infoLabel == null || _attacker?.Stats == null || _runner?.Stats == null)
        {
            return;
        }

        Vector2I attackerCell = Grid.WorldToGrid(_attacker.Stats.Position);
        Vector2I runnerCell = Grid.WorldToGrid(_runner.Stats.Position);
        bool runnerInRange = _attacker.Stats.AttackRange != null
            && _attacker.Stats.AttackRange.ContainsCell(attackerCell, _attacker.Stats.Direction.ToVector2(), runnerCell);

        string phase = !_escapeIssued
            ? $"Waiting for runner to leave on tick {EscapeTick}."
            : runnerInRange
                ? "Runner is back in attack range."
                : "Runner is out of range. Attacker should be chasing.";

        _infoLabel.Text =
            "Out-of-range chase test\n" +
            "Red: attacker  Blue: runner\n" +
            $"Tick: {BattleManager.CurrentTick}\n" +
            $"Attacker cell: {attackerCell}  Runner cell: {runnerCell}\n" +
            $"{phase}\n" +
            "Expected: blue moves one cell down, then red follows.";
    }

    private static VarRange CreateAttackRange(params Vector2I[] relativeCells)
    {
        var cells = new Godot.Collections.Array<Vector2I>();
        foreach (Vector2I relativeCell in relativeCells)
        {
            cells.Add(relativeCell);
        }

        return new VarRange
        {
            RelativeCells = cells
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
