using Godot;

public class AttackRangeQueryInfo
{
    public Var Source { get; set; }
    public Vector2I OriginCell { get; set; }
    public Vector2 FacingDirection { get; set; }
    public VarRange AttackRange { get; set; }
    public VarRange DetectRange { get; set; }
}
