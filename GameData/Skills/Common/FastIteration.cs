using Godot;
using System;

public class FastIteration : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("MoveSpeedBonus", GetValue("MoveSpeedBonus") * stack);
    }
}
