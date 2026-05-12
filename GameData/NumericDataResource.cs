using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class NumericDataResource : Resource
{
	[Export] public Dictionary<string, int> Data { get; set; } = new();
}
