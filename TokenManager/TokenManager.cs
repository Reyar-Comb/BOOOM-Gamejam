using Godot;
using System;

public partial class TokenManager : Node
{
	public enum EndReason
	{
		Token,
		Patience,
		Victory
	}
	private GameData _gameData = null!;
	[Export] public BarController BarController { get; private set; } = null!;
	public void ApplyTokenCost(TokenOperationInfo info)
	{
		ApplyTokenOperationModifiers(info);
		int cost = info.TokenCost;
		if (_gameData.NumericData.Get("Token") <= cost)
		{
			BattleManager.Instance.EndBattle(EndReason.Token);
			return;
		}
		int rest = _gameData.NumericData.Get("Token") - cost;
		_gameData.NumericData.Set("Token", rest);
		GD.Print($"Applied token cost: {cost}, remaining tokens: {rest}");
		BarController.ApplyTokenCost((float)rest / _gameData.NumericData.Get("MaxToken"));
	}

	public void AddToken(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		int maxToken = _gameData.NumericData.Get("MaxToken");
		int token = Math.Min(_gameData.NumericData.Get("Token") + amount, maxToken);
		_gameData.NumericData.Set("Token", token);
		BarController.ApplyTokenCost((float)token / maxToken);
		GD.Print($"Added {amount} tokens. Current token: {token}");
	}

	public float GetTokenCostRefPercent(TokenOperationInfo info)
	{
		ApplyTokenOperationModifiers(info);
		int rest = _gameData.NumericData.Get("Token") - info.TokenCost;
		if (rest < 0) rest = 0;
		return (float)rest / _gameData.NumericData.Get("MaxToken");
		
		
	}

	public float GetPatienceCostRefPercent(int patienceCost)
	{
		int rest = _gameData.NumericData.Get("Patience") - patienceCost;
		if (rest < 0) rest = 0;
		return (float)rest / _gameData.NumericData.Get("MaxPatience");
	}

	private void ApplyTokenOperationModifiers(TokenOperationInfo info)
	{
		_gameData.SkillManager.OnTokenOperation(info);
		info.TokenCost = Math.Max(0, info.TokenCost - GetTokenCostReduction(info.Type));
	}

	private int GetTokenCostReduction(TokenOperationInfo.OperationType operationType)
	{
		return operationType switch
		{
			TokenOperationInfo.OperationType.CreateVar => _gameData.NumericData.Get("CreateTokenCostReduction"),
			TokenOperationInfo.OperationType.MoveVar => _gameData.NumericData.Get("CommandTokenCostReduction"),
			TokenOperationInfo.OperationType.QueryVarLocation => _gameData.NumericData.Get("CommandTokenCostReduction"),
			TokenOperationInfo.OperationType.QueryVarHealth => _gameData.NumericData.Get("CommandTokenCostReduction"),
			_ => 0
		};
	}

	public void Initialize(GameData gameData)
	{
		_gameData = gameData;
	}

	public void Reset()
	{
		ClearCostRef();
		RefreshTokenProgress();
		RefreshPatienceProgress();
	}

	public void ClearCostRef()
	{
		BarController.ClearTokenCostRef();
		BarController.ClearPatienceCostRef();
	}

	public void Tick(double delta)
	{
		UpdatePatience(delta);
	}

	private void UpdatePatience(double delta)
	{
		int patience = _gameData.NumericData.Get("Patience");
		if (patience <= 0)
		{
			BattleManager.Instance.EndBattle(EndReason.Patience);
		}
		patience -= (int)(delta * _gameData.NumericData.Get("PatienceDecayRate"));
		_gameData.NumericData.Set("Patience", patience);
		float percent = (float)patience / _gameData.NumericData.Get("MaxPatience");
		BarController.RefreshPatienceProgress(percent);
	}

	private void RefreshTokenProgress()
	{
		BarController.ApplyTokenCost((float)_gameData.NumericData.Get("Token") / _gameData.NumericData.Get("MaxToken"));
	}

	private void RefreshPatienceProgress()
	{
		BarController.RefreshPatienceProgress((float)_gameData.NumericData.Get("Patience") / _gameData.NumericData.Get("MaxPatience"));
	}

	public void ExchangeToken()
	{
		int token = _gameData.NumericData.Get("Token");
		int tokenEx = _gameData.NumericData.Get("TokenExchangeAmount") + _gameData.NumericData.Get("TokenRequestGainBonus");
		int patience = _gameData.NumericData.Get("Patience");
		int patienceEx = Math.Max(0, _gameData.NumericData.Get("PatienceExchangeAmount") - _gameData.NumericData.Get("TokenRequestPatienceCostReduction"));
		if (patience - patienceEx <= 0)
		{
			BattleManager.Instance.EndBattle(EndReason.Patience);
			return;
		}
		_gameData.NumericData.Set("Token", Math.Min(token + tokenEx, _gameData.NumericData.Get("MaxToken")));
		_gameData.NumericData.Set("Patience", patience - patienceEx);
		BarController.ApplyTokenCost((float)_gameData.NumericData.Get("Token") / _gameData.NumericData.Get("MaxToken"));
		BarController.ApplyPatienceCost((float)_gameData.NumericData.Get("Patience") / _gameData.NumericData.Get("MaxPatience"));
		GD.Print($"Exchanged {patienceEx} patience for {tokenEx} tokens. Current token: {_gameData.NumericData.Get("Token")}, current patience: {_gameData.NumericData.Get("Patience")}");
	}

	public void OnHoverExchangeToken()
	{
		int tokenEx = _gameData.NumericData.Get("TokenExchangeAmount") + _gameData.NumericData.Get("TokenRequestGainBonus");
		int patienceEx = Math.Max(0, _gameData.NumericData.Get("PatienceExchangeAmount") - _gameData.NumericData.Get("TokenRequestPatienceCostReduction"));
		int token = Math.Min(_gameData.NumericData.Get("Token") + tokenEx, _gameData.NumericData.Get("MaxToken"));
		BarController.ShowTokenCostRef((float)token / _gameData.NumericData.Get("MaxToken"));
		BarController.ShowPatienceCostRef(GetPatienceCostRefPercent(patienceEx));
	}

	public void OnHoverRegisterVar(Var var)
	{
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.CreateVar,
			VarType = var.Stats.Type,
			TokenCost = var.Stats.TokenCost,
			ShowOnly = true
		};
		BarController.ShowTokenCostRef(GetTokenCostRefPercent(info));
	}

	public void RegisterVar(Var var)
	{
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.CreateVar,
			VarType = var.Stats.Type,
			TokenCost = var.Stats.TokenCost,
			ShowOnly = false
		};
		ApplyTokenCost(info);
	}

	public void OnHoverMoveVar(Var var)
	{
		int cost = _gameData.NumericData.Get("MoveCost");
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.MoveVar,
			TokenCost = cost,
			ShowOnly = true
		};
		BarController.ShowTokenCostRef(GetTokenCostRefPercent(info));
	}

	public void MoveVar(Var var)
	{
		int cost = _gameData.NumericData.Get("MoveCost");
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.MoveVar,
			TokenCost = cost,
			ShowOnly = false
		};
		ApplyTokenCost(info);
	}

	public void OnHoverQueryVarLocation(Var var)
	{
		int cost = _gameData.NumericData.Get("QueryLocationCost");
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.QueryVarLocation,
			TokenCost = cost,
			ShowOnly = true
		};
		BarController.ShowTokenCostRef(GetTokenCostRefPercent(info));
	}

	public void QueryVarLocation(Var var)
	{
		int cost = _gameData.NumericData.Get("QueryLocationCost");
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.QueryVarLocation,
			TokenCost = cost,
			ShowOnly = false
		};
		ApplyTokenCost(info);
	}

	public void OnHoverQueryVarHealth(Var var)
	{
		int cost = _gameData.NumericData.Get("QueryHealthCost");
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.QueryVarHealth,
			TokenCost = cost,
			ShowOnly = true
		};
		BarController.ShowTokenCostRef(GetTokenCostRefPercent(info));
	}

	public void QueryVarHealth(Var var)
	{
		int cost = _gameData.NumericData.Get("QueryHealthCost");
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.QueryVarHealth,
			TokenCost = cost,
			ShowOnly = false
		};
		ApplyTokenCost(info);
	}
}
