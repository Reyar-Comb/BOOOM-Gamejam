using Godot;
using System;

public class EfficientCreation : Skill
{
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("CreateTokenCostReduction", GetValue("CreateTokenCostReduction") * stack);
    }
}
