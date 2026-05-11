using Godot;
using System;

public class BiasedRecognition : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new BiasedRecognitionRuntime(Resource);
}
