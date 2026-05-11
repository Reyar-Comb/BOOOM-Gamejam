using Godot;
using System;

public class EfficientMarketing : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("TokenRequestGainBonus", GetValue("TokenRequestGainBonus") * stack);
    }
}
