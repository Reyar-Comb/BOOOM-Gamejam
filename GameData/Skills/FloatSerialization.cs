using Godot;
using System;

public class FloatSerialization : Skill
{
    public override string Name => "float-serialization";
    public override string Description => "Reduces token cost when creating float vars.";
    public override Texture2D Icon => null;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("FloatCreateTokenCostMultiplier", 0.8f);
    }
}
