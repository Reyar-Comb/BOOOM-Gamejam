using Godot;
using System;

[GlobalClass]
public partial class TooltipTrigger : Node
{
	[Export] public string TooltipId { get; set; } = "";
	[Export] public bool HideOnClick { get; set; } = true;

	public override void _Ready()
	{
		var parent = GetParent();
		if (parent is Control control)
		{
			control.MouseEntered += () => {
				// GD.Print($"Showing tooltip: {TooltipId}");
				TooltipManager.Instance.ShowTooltip(TooltipId, HideOnClick);
			};
			control.MouseExited  += () => {
				// GD.Print($"Hiding tooltip: {TooltipId}");
				TooltipManager.Instance.HideTooltip();
			};
		}
	}
}
