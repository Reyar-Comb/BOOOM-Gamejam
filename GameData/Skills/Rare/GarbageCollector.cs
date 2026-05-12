using Godot;
using System;

public class GarbageCollector : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("DeathTokenRefund", GetValue("DeathTokenRefund") * stack);
    }
}
