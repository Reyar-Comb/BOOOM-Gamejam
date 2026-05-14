using System.Collections.Generic;

public class DroneBoolRuntime : ISkillRuntime
{
    private readonly SkillResource _resource;
    private HashSet<Var> _detectedVars = new HashSet<Var>();
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
        if (_detectedVars.Contains(info.DetectedVar))
        {
            return;
        }
        _detectedVars.Add(info.DetectedVar);
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
