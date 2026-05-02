using Godot;
using System;
using System.Collections.Generic;
using GDArray = Godot.Collections.Array<Godot.Vector2I>;

[Tool]
[GlobalClass]
public partial class VarAttackRange : Resource
{
    [Export]
    public GDArray RelativeCells { get; set; } = new GDArray();

    public IEnumerable<Vector2I> EnumerateTargetCells(Vector2I originCell, Vector2 facingDirection)
    {
        Vector2I forward = facingDirection.ToFacingDirection();
        Vector2I right = new(-forward.Y, forward.X);

        foreach (Vector2I relativeCell in RelativeCells)
        {
            Vector2I rotatedOffset = new(
                right.X * relativeCell.X + forward.X * relativeCell.Y,
                right.Y * relativeCell.X + forward.Y * relativeCell.Y);

            yield return originCell + rotatedOffset;
        }
    }
}
