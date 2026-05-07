using Godot;
using System;

[GlobalClass]
public partial class StatResource : Resource
{
    [Export] public string StatName { get; set; } = string.Empty;
    [Export] public float DefaultValue { get; set; } = 0f;
}
