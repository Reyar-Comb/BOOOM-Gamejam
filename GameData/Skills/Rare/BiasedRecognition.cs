using Godot;
using System;

public class BiasedRecognition : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        for (int i = 0; i < stack; i++)
        {
            data.SkillManager.AddRuntime(GetSkillRuntime());
        }
    }

    public override ISkillRuntime GetSkillRuntime() => new BiasedRecognitionRuntime(Resource);
}
