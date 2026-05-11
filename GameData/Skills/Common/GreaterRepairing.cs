using Godot;
using System;

public class GreaterRepairing : Skill
{
    public override string Name => "greater-repairing";
    public override string Description => "Increases attack of created vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Common;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("AttackBonus", GetValue("AttackBonus") * stack);
    }
}
