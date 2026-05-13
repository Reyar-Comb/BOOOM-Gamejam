using Godot;
using System;
using System.Collections.Generic;

public partial class AttackInfo : RefCounted
{
    public Var Source { get; init; }
    public Var Target { get; init; }
    public HashSet<Var> Attackers { get; set; }
    public IReadOnlyList<Var> Vars { get; set; }
    public MapData MapData { get; set; }
    public Vector2 GetFromDirection(Vector2 to)
    {
        return -(to - Source.Stats.Position).Normalized();
    }
    public int Damage { get; set; }
    public int Defense { get; set; }
}
