using Godot;
using System;

public class CrowdOfChar : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }
    public override ISkillRuntime GetSkillRuntime() => new CrowdOfCharRuntime(Resource);
}
