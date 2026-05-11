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
			GD.Print("You Died!");
			return;
		}
		int rest = _gameData.NumericData.Get("Token") - cost;
		_gameData.NumericData.Set("Token", rest);
		GD.Print($"Applied token cost: {cost}, remaining tokens: {rest}");
		BarController.ApplyTokenCost((float)rest / _gameData.NumericData.Get("MaxToken"));
	}

	public float GetTokenCostRefPercent(TokenOperationInfo info)
	{
		_gameData.SkillManager.OnTokenOperation(info);
		int rest = _gameData.NumericData.Get("Token") - info.TokenCost;
		if (rest < 0) rest = 0;
		return (float)rest / _gameData.NumericData.Get("MaxToken");
	}

	public void Initialize(GameData gameData)
	{
		_gameData = gameData;
	}

	public void ClearTokenCostRef()
	{
		BarController.ClearTokenCostRef();
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
