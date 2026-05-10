using Godot;
using System;

public class GreaterRepairing : Skill
{
    public override string Name => "greater-repairing";
    public override string Description => "Increases the attack of created vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Common;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("AttackMultiplier", 1.1f);
    }
}
