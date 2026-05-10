using Godot;
using System;

public class BiasedRecognition : Skill
{
    public override string Name => "biased-recognition";
    public override string Description => "Greatly reduces token costs for every operation at the start of each wave, but repeated use of the same operation increases its cost.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new BiasedRecognitionRuntime();
}
