using Godot;
using System;

public class StabilityImprovement : Skill
{
    public override string Name => "stability-improvement";
    public override string Description => "Increases the health of created vars.";
    public override Texture2D Icon => null;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("HealthMultiplier", 1.2f);
    }
}
