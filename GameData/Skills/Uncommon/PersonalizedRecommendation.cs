using Godot;
using System;

public class PersonalizedRecommendation : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("TokenRequestPatienceCostReduction", GetValue("TokenRequestPatienceCostReduction") * stack);
    }
}
