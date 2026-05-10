using System;
using System.Collections.Generic;

public class BiasedRecognitionRuntime : ISkillRuntime
{
    private const int InitialTokenCostReduction = 10;
    private const int ReductionLossPerOperation = 1;

    private readonly Dictionary<TokenOperationInfo.OperationType, int> _tokenCostReductions = new();

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
}
