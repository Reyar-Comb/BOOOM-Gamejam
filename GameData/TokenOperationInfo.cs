public class TokenOperationInfo
{
    public enum OperationType
    {
        CreateVar,
        MoveVar,
        QueryVarLocation,
        QueryVarHealth
    }

    public OperationType Type { get; set; }
    public VarStats.VarType? VarType { get; set; }
    public int TokenCost { get; set; }
    public bool ShowOnly { get; set; }
}
