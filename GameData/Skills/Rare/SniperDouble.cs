using Godot;
using System;

public class SniperDouble : Skill
{
    public override string Name => "sniper-double";
    public override string Description => "Double vars can attack enemies in detect range when they share the same x or y axis.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new SniperDoubleRuntime();
}
