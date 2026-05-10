using Godot;
using System;

public class AccurateFloat : Skill
{
    public override string Name => "accurate-float";
    public override string Description => "Float vars deal +3 damage when attacking a target on the same x or y axis.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new AccurateFloatRuntime();
}
