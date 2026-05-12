using Godot;
using System;

public class SaferInteger : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("IntegerDefenseBonus", GetValue("IntegerDefenseBonus") * stack);
    }
}
