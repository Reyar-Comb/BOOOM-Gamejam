public class DroneBoolRuntime : ISkillRuntime
{
    private readonly SkillResource _resource;

    public DroneBoolRuntime(SkillResource resource)
    {
        _resource = resource;
    }

    public void OnDetected(DetectInfo info)
    {
        if (info == null || info.Detector == null || info.Detector.Stats.Type != VarStats.VarType.Bool)
        {
            return;
        }

        BattleManager.Instance.TokenManager.AddToken(TokenGainPerNewEnemyDetected);
    }

    public void OnTokenOperation(TokenOperationInfo info)
    {
        if (info == null || info.Type != TokenOperationInfo.OperationType.CreateVar || info.VarType != VarStats.VarType.Bool)
        {
            return;
        }

        info.TokenCost += BoolCreationCostIncrease;
    }

    private int TokenGainPerNewEnemyDetected => _resource.GetValue("TokenGainPerNewEnemyDetected");
    private int BoolCreationCostIncrease => _resource.GetValue("BoolCreationCostIncrease");
}
