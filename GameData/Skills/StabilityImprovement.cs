using Godot;
using System;

public class StabilityImprovement : Skill
{
    public override string Name => "stability-improvement";
    public override string Description => "Improves the stability of the Var.";

    public override void Apply(GameData data)
    {
        data.NumericData.Set("HealthMultiplier", 1.2f);
    }
}
