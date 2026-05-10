using Godot;
using System;

public class CrowdOfChar : Skill
{
    public override string Name => "crowd-of-char";
    public override string Description => "";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }
    public override ISkillRuntime GetSkillRuntime() => new CrowdOfCharRuntime();
}
