using Godot;
using System;

public class EffectiveCreation : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("CreateTokenCostReduction", GetValue("CreateTokenCostReduction") * stack);
    }
}
