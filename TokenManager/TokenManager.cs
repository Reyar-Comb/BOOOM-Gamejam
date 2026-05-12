using Godot;
using System;

public partial class TokenManager : Node
{
	private GameData _gameData = null!;
	[Export] public BarController BarController { get; private set; } = null!;

	public void ApplyTokenCost(TokenOperationInfo info)
	{
		_gameData.SkillManager.OnTokenOperation(info);
		int cost = info.TokenCost;
		if (_gameData.NumericData.Get("Token") <= cost)
		{
			BattleManager.Instance.OnDie();
			return;
		}
		int rest = _gameData.NumericData.Get("Token") - cost;
		_gameData.NumericData.Set("Token", rest);
		GD.Print($"Applied token cost: {cost}, remaining tokens: {rest}");
		BarController.ApplyTokenCost((float)rest / _gameData.NumericData.Get("MaxToken"));
	}

	public float GetTokenCostRefPercent(TokenOperationInfo info)
	{
		if (info.TokenCost > 0)
		{
			_gameData.SkillManager.OnTokenOperation(info);
		}
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

	public void Initialize(GameData gameData)
	{
		_gameData = gameData;
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
			BattleManager.Instance.OnDie();
		}
		patience -= (int)(delta * _gameData.NumericData.Get("PatienceDecayRate"));
		_gameData.NumericData.Set("Patience", patience);
		float percent = (float)patience / _gameData.NumericData.Get("MaxPatience");
		BarController.RefreshPatienceProgress(percent);
	}

	public void ExchangeToken()
	{
		int token = _gameData.NumericData.Get("Token");
		int tokenEx = _gameData.NumericData.Get("TokenExchangeAmount");
		int patience = _gameData.NumericData.Get("Patience");
		int patienceEx = _gameData.NumericData.Get("PatienceExchangeAmount");
		if (patience - patienceEx <= 0)
		{
			BattleManager.Instance.OnDie();
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
		int tokenEx = _gameData.NumericData.Get("TokenExchangeAmount");
		int patienceEx = _gameData.NumericData.Get("PatienceExchangeAmount");
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.CreateVar,
			TokenCost = -tokenEx,
			ShowOnly = true
		};
		BarController.ShowTokenCostRef(GetTokenCostRefPercent(info));
		BarController.ShowPatienceCostRef(GetPatienceCostRefPercent(patienceEx));
	}

	public void OnHoverRegisterVar(Var var)
	{
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.CreateVar,
			TokenCost = var.Stats.TokenCost,
			ShowOnly = true
		};
		_gameData.SkillManager.OnTokenOperation(info);
		BarController.ShowTokenCostRef(GetTokenCostRefPercent(info));
	}

	public void RegisterVar(Var var)
	{
		TokenOperationInfo info = new()
		{
			Type = TokenOperationInfo.OperationType.CreateVar,
			TokenCost = var.Stats.TokenCost,
			ShowOnly = false
		};
		_gameData.SkillManager.OnTokenOperation(info);
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
		_gameData.SkillManager.OnTokenOperation(info);
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
		_gameData.SkillManager.OnTokenOperation(info);
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
		_gameData.SkillManager.OnTokenOperation(info);
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
		_gameData.SkillManager.OnTokenOperation(info);
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
		_gameData.SkillManager.OnTokenOperation(info);
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
		_gameData.SkillManager.OnTokenOperation(info);
		ApplyTokenCost(info);
	}
}
