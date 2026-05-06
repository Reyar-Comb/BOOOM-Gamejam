using Godot;
using System;

public class AttackInfo
{
    public Var Source { get; init; }
    public Vector2 GetFromDirection(Vector2 to)
    {
        return -(to - Source.Stats.Position).Normalized();
    }
    public int Damage { get; init; }
}
