using Godot;
using System;

public class EffectiveRepairing : Skill
{
    public override string Name => "effective-repairing";
    public override string Description => "Increases the attack of created vars.";
    public override Texture2D Icon => null;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("AttackMultiplier", 1.2f);
    }
}
