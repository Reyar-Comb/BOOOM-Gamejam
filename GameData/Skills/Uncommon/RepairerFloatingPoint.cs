using Godot;
using System;

public class RepairerFloatingPoint : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("FloatingPointAttackBonus", GetValue("FloatingPointAttackBonus") * stack);
    }
}
