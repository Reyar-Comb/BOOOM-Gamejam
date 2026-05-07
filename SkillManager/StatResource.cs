using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;

[GlobalClass]
public partial class StatResource : Resource
{
    public enum Operation
    {
        Add,
        Mult
    }
    [Export] public string StatName { get; set; } = string.Empty;
    [Export] public float Value { get; set; } = 0f;
    [Export] public Operation StatOperation { get; set; } = Operation.Add;
    // [Export] public Operation StackOperation { get; set; } = Operation.Add;
    public T Apply<T>(float baseValue)
        where T : INumber<T>
    {
        float result = baseValue;
        switch (StatOperation)
        {
            case Operation.Add:
                result += Value;
                break;
            case Operation.Mult:
                result *= Value;
                break;
        }
        return T.CreateChecked(result);
    }
}
