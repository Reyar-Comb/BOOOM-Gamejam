using Godot;
using System;

public class GrowingInt : Skill
{
    public override string Name => "growing-int";
    public override string Description => "Each created Int randomly increases health, attack, or defense for future Int vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new GrowingIntRuntime();
}
