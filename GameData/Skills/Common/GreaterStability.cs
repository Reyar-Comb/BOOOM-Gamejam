using Godot;
using System;

public class GreaterStability : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("HealthBonus", GetValue("HealthBonus") * stack);
    }
}
