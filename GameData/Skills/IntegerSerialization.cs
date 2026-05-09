using Godot;
using System;

public class IntegerSerialization : Skill
{
    public override string Name => "integer-serialization";
    public override string Description => "Reduces token cost when creating integer vars.";
    public override Texture2D Icon => null;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("IntegerCreateTokenCostMultiplier", 0.8f);
    }
}
