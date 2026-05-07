using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
    [Export] public VarManager VarManager { get; set; }
    private VarRenderer _varRenderer = null!;

    public override void _Ready()
    {
        Var var = new Var();
        var.Stats = new VarStats
        {
            MaxHealth = 100,
            AttackSpeedMult = 1.5f,
            AttackRange = new VarRange
            {
                RelativeCells = new Godot.Collections.Array<Vector2I>
                {
                    new Vector2I(0, 1),
                    new Vector2I(0, 2)
                }
            },
            MoveSpeed = 100f,
            Position = Grid.GridToWorld(0, 0)
        };
        VarManager.AddVar(var);
        var.SetPath(new List<Vector2I> { new Vector2I(1, 0), new Vector2I(1, 1), new Vector2I(0, 1) });

        CreateRenderer(var);
    }

    private void CreateRenderer(Var var)
    {
        _varRenderer ??= new VarRenderer
        {
            BodyRadius = 50.0f,
            BodyColor = Colors.Red,
            DirectionColor = Colors.White,
            DirectionLength = 60.0f,
            RenderVarBody = true,
            RenderDirection = true
        };

        if (_varRenderer.GetParent() == null)
        {
            AddChild(_varRenderer);
        }

        _varRenderer.AddVar(var, Colors.Red, Colors.Red, Colors.Red, Colors.White);
    }
}
