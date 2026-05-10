using Godot;
using System;

public class EffectiveCreation : Skill
{
    public override string Name => "effective-creation";
    public override string Description => "Reduces token cost when creating vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Uncommon;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("CreateTokenCostMultiplier", 0.8f);
    }
}
