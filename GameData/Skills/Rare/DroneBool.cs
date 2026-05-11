using Godot;
using System;

public class DroneBool : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }
    public override ISkillRuntime GetSkillRuntime() => new DroneBoolRuntime();
}
