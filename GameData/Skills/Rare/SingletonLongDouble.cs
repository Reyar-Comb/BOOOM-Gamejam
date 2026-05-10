using Godot;
using System;

public class SingletonLongDouble : Skill
{
    public override string Name => "singleton-long-double";
    public override string Description => "If you create a LongDouble var as the first var in a wave, it will receive a significant boost but defense set to 0. Doing this blocks future Float, Double, and LongDouble creation.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new SingletonLongDoubleRuntime();
}
