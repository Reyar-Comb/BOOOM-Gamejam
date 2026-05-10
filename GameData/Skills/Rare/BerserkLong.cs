using Godot;
using System;

public class BerserkLong : Skill
{
    public override string Name => "berserk-long";
    public override string Description => "Long vars deal double damage and have half defense while exactly one enemy is in detect range.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new BerserkLongRuntime();
}
