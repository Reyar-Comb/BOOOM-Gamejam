using Godot;
using System;

public class EffectiveFeedback : Skill
{
    public override string Name => "effective-feedback";
    public override string Description => "Reduces token cost when querying or commanding vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Common;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("CommandTokenCostMultiplier", 0.9f);
    }
}
