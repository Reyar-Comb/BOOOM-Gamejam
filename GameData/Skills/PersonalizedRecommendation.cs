using Godot;
using System;

public class PersonalizedRecommendation : Skill
{
    public override string Name => "personalized-recommendation";
    public override string Description => "Reduces patience consumed when requesting tokens from users.";
    public override Texture2D Icon => null;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("TokenRequestPatienceCostMultiplier", 0.8f);
    }
}
