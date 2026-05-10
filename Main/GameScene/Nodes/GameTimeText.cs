using Godot;
using System;

public partial class GameTimeText : RichTextLabel
{
	public override void _Process(double delta)
	{
		long t = BattleManager.Instance.GameTime;
		Text = $"{t / 1000 / 60 / 60 % 24 :00}:{(t / 1000 / 60) % 60:00}:{(t / 1000) % 60:00}:{t % 1000 / 10 :00}";
	}
}
