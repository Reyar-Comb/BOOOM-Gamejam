using System;
using System.Collections.Generic;

public class BiasedRecognitionRuntime : ISkillRuntime
{
    private readonly SkillResource _resource;

    private readonly Dictionary<TokenOperationInfo.OperationType, int> _tokenCostReductions = new();

    public BiasedRecognitionRuntime(SkillResource resource)
    {
        _resource = resource;
    }

    public void OnWaveStarted()
    {
        _tokenCostReductions.Clear();
        foreach (TokenOperationInfo.OperationType operationType in Enum.GetValues<TokenOperationInfo.OperationType>())
        {
            _tokenCostReductions[operationType] = InitialTokenCostReduction;
        }
    }

    public void OnTokenOperation(TokenOperationInfo info)
    {
        if (info == null)
        {
            return;
        }

        int reduction = GetReduction(info.Type);
        info.TokenCost = Math.Max(0, info.TokenCost - reduction);
        
        if (info.ShowOnly)
        {
            return;
        }
        _tokenCostReductions[info.Type] = reduction - ReductionLossPerOperation;
    }

    private int GetReduction(TokenOperationInfo.OperationType operationType)
    {
        if (!_tokenCostReductions.TryGetValue(operationType, out int reduction))
        {
            reduction = InitialTokenCostReduction;
            _tokenCostReductions[operationType] = reduction;
        }

        return reduction;
    }

    private int InitialTokenCostReduction => (int)_resource.GetValue("InitialTokenCostReduction");
    private int ReductionLossPerOperation => (int)_resource.GetValue("ReductionLossPerOperation");
}
