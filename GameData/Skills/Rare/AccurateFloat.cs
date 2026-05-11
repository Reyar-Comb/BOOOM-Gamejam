using Godot;
using System;

public class AccurateFloat : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new AccurateFloatRuntime(Resource);
}
