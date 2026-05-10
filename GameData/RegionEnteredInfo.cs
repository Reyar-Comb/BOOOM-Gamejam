using Godot;

public class RegionEnteredInfo
{
    public Var Var { get; set; }
    public MapData MapData { get; set; }
    public Vector2I FromCell { get; set; }
    public Vector2I ToCell { get; set; }
    public int FromRegion { get; set; }
    public int ToRegion { get; set; }
}
