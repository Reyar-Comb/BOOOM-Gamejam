using Godot;
using System;
using System.Collections.Generic;

public class AttackInfo
{
    public Var Source { get; init; }
    public HashSet<Var> Attackers { get; set; }
    public Vector2 GetFromDirection(Vector2 to)
    {
        return -(to - Source.Stats.Position).Normalized();
    }
    public int Damage { get; set; }
}
