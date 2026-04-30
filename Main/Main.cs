using Godot;
using System;
using System.Collections.Generic;
public partial class Main : Node2D
{
	[Export] public VarManager VarManager { get; set; }
	private Var _var;
	public override void _Ready()
	{
		Var var = new Var();
		var.Stats = new VarStats
		{
			MaxHealth = 100,
			AttackSpeedMult = 1.5f,
			MoveSpeed = 100f,
			Position = new Vector2(0, 0)
		};
		VarManager.AddVar(var);
		var.SetPath(new List<Vector2I> { new Vector2I(1, 0), new Vector2I(1, 1), new Vector2I(0, 1) });
		
		_var = var;
	}
	public override void _Process(double delta)
	{
		QueueRedraw();
	}
    public override void _Draw()
    {
        DrawCircle(_var.Stats.Position, 50, Colors.Red);
    }
}
