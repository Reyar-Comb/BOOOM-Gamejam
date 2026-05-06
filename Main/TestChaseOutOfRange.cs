using Godot;
using System.Collections.Generic;

public partial class TestChaseOutOfRange : Node2D
{
    [Export] public VarManager VarManager { get; set; } = null!;
    [Export] public BattleManager BattleManager { get; set; } = null!;

    private const long EscapeTick = 12;
    private static readonly Vector2I AttackerStartCell = new(5, 1);
    private static readonly Vector2I RunnerStartCell = new(5, 2);
    private static readonly Vector2I RunnerEscapeCell = new(5, 8);

    private Var _attacker = null!;
    private Var _runner = null!;
    private Label _infoLabel = null!;
    private bool _escapeIssued = false;

    public override void _Ready()
    {
        _infoLabel = GetNode<Label>("CanvasLayer/InfoLabel");
        BattleManager ??= GetNodeOrNull<BattleManager>("BattleManager");

        _attacker = CreateVar(
            AttackerStartCell,
            Colors.OrangeRed,
            moveSpeed: 130.0f,
            attackRange: CreateAttackRange(new Vector2I(0, 1)),
            detectRange: CreateAttackRange(new Vector2I(0, 1), new Vector2I(0, 2)),
            team: VarStats.Team.Friendly);
        _runner = CreateVar(
            RunnerStartCell,
            Colors.DeepSkyBlue,
            moveSpeed: 170.0f,
            attackRange: CreateAttackRange(),
            detectRange: CreateAttackRange(),
            team: VarStats.Team.Hostile);

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
        DrawTargetLink();
    }

    private Var CreateVar(Vector2I startCell, Color color, float moveSpeed, VarRange attackRange, VarRange detectRange = null, VarStats.Team team = VarStats.Team.Friendly)
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
                Position = Grid.GridToWorld(startCell),
                VarTeam = team,
                AttackDamage = 30
            }
        };

        VarManager.AddVar(var);
        CreateRenderer(var, color);
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

    private void CreateRenderer(Var var, Color color)
    {
        VarRenderer renderer = new()
        {
            BodyRadius = 18.0f,
            BodyColor = color,
            AttackRangeColor = color,
            DetectRangeColor = WithAlpha(color, 0.65f),
            DirectionColor = Colors.White,
            BattleManager = BattleManager,
            RenderVarBody = true,
            RenderAttackRange = true,
            RenderDetectRange = true,
            RenderDirection = true
        };

        AddChild(renderer);
        renderer.SetVar(var);
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
            && _attacker.Stats.AttackRange.ContainsCell(attackerCell, _attacker.Stats.Direction, runnerCell);

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

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = alpha;
        return color;
    }
}
