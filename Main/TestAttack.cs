using Godot;
using System.Collections.Generic;

public partial class TestAttack : Node2D
{
    [Export] public VarManager VarManager { get; set; } = null!;

    [Export] public BattleManager BattleManager { get; set; } = null!;

    public override void _Ready()
    {
        BattleManager ??= GetNodeOrNull<BattleManager>("Node");

        Var friendly = CreateVar(
            new Vector2I(2, 2),
            new List<Vector2I>
            {
                new(2, 2),
                new(4, 2),
                new(5, 2)
            },
            VarStats.Team.Friendly
            );
        CreateRenderer(friendly, Colors.OrangeRed);

        Var hostile = CreateVar(
            new Vector2I(10, 2),
            new List<Vector2I>
            {
                new(10, 2),
                new(8, 2),
                new(6, 2)
            },
            VarStats.Team.Hostile
            );
        CreateRenderer(hostile, Colors.DeepSkyBlue);
    }

    private Var CreateVar(Vector2I startPosition, List<Vector2I> path, VarStats.Team team = VarStats.Team.Friendly)
    {
        VarRange attackRange = CreateRange(
            new Vector2I(0, 1),
            new Vector2I(0, 2),
            new Vector2I(1, 1),
            new Vector2I(-1, 1));
        VarRange detectRange = CreateRange(
            new Vector2I(0, 1),
            new Vector2I(0, 2),
            new Vector2I(0, 3),
            new Vector2I(1, 1),
            new Vector2I(-1, 1),
            new Vector2I(1, 2),
            new Vector2I(-1, 2));

        Var var = new()
        {
            Stats = new VarStats
            {
                MaxHealth = 100,
                AttackSpeedMult = 1.0f,
                AttackFrameInterval = 20,
                MoveSpeed = 120.0f,
                AttackRange = attackRange,
                DetectRange = detectRange,
                Position = Grid.GridToWorld(startPosition),
                VarTeam = team
            }
        };

        VarManager.AddVar(var);
        var.SetPath(path);
        return var;
    }

    private void CreateRenderer(Var var, Color color)
    {
        VarRenderer renderer = new()
        {
            BodyRadius = 20.0f,
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

    private static VarRange CreateRange(params Vector2I[] relativeCells)
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
