using Godot;
using System;

public abstract class Skill
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract void Apply(GameData data);
}
