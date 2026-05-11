using Godot;
using System;

public class PersonalizedRecommendation : Skill
{
    public override string Name => "personalized-recommendation";
    public override string Description => "Reduces patience consumed when requesting tokens from users by a fixed amount.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Uncommon;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("TokenRequestPatienceCostReduction", GetValue("TokenRequestPatienceCostReduction") * stack);
    }
}
