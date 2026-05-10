using Godot;
using System;

public class SaferInteger : Skill
{
    public override string Name => "safer-integer";
    public override string Description => "Increases the defense of created vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Uncommon;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("IntegerDefenseMultiplier", 1.2f);
    }
}
