using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class ColorDataResource : Resource
{
    [Export] public Dictionary<string, Color> Data { get; set; } = new();
}