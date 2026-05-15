using Godot;
using System;

public partial class AlertBorder : ColorRect
{
	public GameData GameData { get; set; }

	public int CurrentToken => GameData.NumericData.Get("Token");
	public int CurrentPatience => GameData.NumericData.Get("Patience");
	[Export] public int TokenThreshold = 30;
	[Export] public int PatienceThreshold = 30000;

	public override void _Process(double delta)
	{
		if (GameData == null)
		{
			Visible = false;
			return;
		}
		if (CurrentToken < TokenThreshold || CurrentPatience < PatienceThreshold)
		{
			Visible = true;
		}
		else
		{
			Visible = false;
		}
	}
}
