using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class StatList : Resource
{
    [Export] public StatResource[] AvailableStats { get; set; } = [];
}
