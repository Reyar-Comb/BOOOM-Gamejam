using Godot;
using System;

public class GreaterStability : Skill
{
    public override string Name => "greater-stability";
    public override string Description => "Increases the health of created vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Common;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("HealthMultiplier", 1.1f);
    }
}
