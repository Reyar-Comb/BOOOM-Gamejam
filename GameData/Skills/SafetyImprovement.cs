using Godot;
using System;

public class SafetyImprovement : Skill
{
    public override string Name => "safety-improvement";
    public override string Description => "Increases the defense of created vars.";
    public override Texture2D Icon => null;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("DefenseMultiplier", 1.2f);
    }
}
