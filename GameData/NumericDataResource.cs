using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class NumericDataResource : Resource
{
    [Export] public Dictionary<string, float> Data { get; set; } = new();
}
