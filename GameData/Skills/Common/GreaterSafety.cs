using Godot;
using System;

public class GreaterSafety : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("DefenseBonus", GetValue("DefenseBonus") * stack);
    }
}
