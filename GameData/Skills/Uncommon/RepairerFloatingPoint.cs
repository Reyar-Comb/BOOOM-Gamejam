using Godot;
using System;

public class RepairerFloatingPoint : Skill
{
    public override string Name => "repairer-floating-point";
    public override string Description => "Increases the attack of created vars.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Uncommon;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("FloatingPointAttackMultiplier", 1.2f);
    }
}
