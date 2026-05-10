using Godot;
using System;

public class GreaterSafety : Skill
{
    public override string Name => "greater-safety";
    public override string Description => "Increases the defense of created vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Common;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("DefenseMultiplier", 1.1f);
    }
}
