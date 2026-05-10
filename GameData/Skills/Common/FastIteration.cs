using Godot;
using System;

public class FastIteration : Skill
{
    public override string Name => "fast-iteration";
    public override string Description => "Increases the move speed of created vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Common;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("MoveSpeedMultiplier", 1.1f);
    }
}
