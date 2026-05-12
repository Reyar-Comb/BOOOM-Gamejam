using Godot;
using System;

public class GreaterRepairing : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("AttackBonus", GetValue("AttackBonus") * stack);
    }
}
