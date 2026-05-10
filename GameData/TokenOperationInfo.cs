public class TokenOperationInfo
{
    public enum OperationType
    {
        CreateVar,
        MoveVar,
        QueryVarLocation,
        QueryVarHealth,
        QueryVarStatus
    }

    public OperationType Type { get; set; }
    public int TokenCost { get; set; }
}
