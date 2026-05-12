using Godot;
using System;

public class EffectiveFeedback : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("CommandTokenCostReduction", GetValue("CommandTokenCostReduction") * stack);
    }
}
