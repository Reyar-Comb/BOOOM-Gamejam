using Godot;
using System;

public partial class SkillCardList : VBoxContainer
{
	[Export] public Godot.Collections.Array<TextureRect> SkillCardIcons { get; set; }
	[Export] public Godot.Collections.Array<TooltipTrigger> SkillCardTooltipTriggers { get; set; }

	public SkillManager SkillManager;
	public override void _Ready()
	{

	}


	public void RefreshCards()
	{
		int index = 0;
		foreach (var skill in SkillManager.OwnedSkills)
		{
			if (index >= SkillCardIcons.Count || index >= SkillCardTooltipTriggers.Count)
				break;
			SkillCardIcons[index].Texture = skill.Icon;
			SkillCardTooltipTriggers[index].TooltipId = skill.Name + "_skill";
			index++;
		}
	}
}
