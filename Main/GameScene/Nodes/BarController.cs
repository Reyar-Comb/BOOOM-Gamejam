using Godot;
using System;

public partial class BarController : HBoxContainer
{
	[Export] public ColorRect PatienceBar;
	[Export] public ColorRect TokenBar;

	private bool _patienceHoverLock;


	public override void _Ready()
	{
		SetBarProgress(PatienceBar, 1.0f, 1.0f);
		SetBarProgress(TokenBar, 1.0f, 1.0f);
	}

	private void SetBarProgress(ColorRect bar, float progress, float target)
	{
		var mat = bar.Material as ShaderMaterial;
		mat.SetShaderParameter("progress", progress);
		mat.SetShaderParameter("target", target);
	}

	public void ShowTokenCostRef(float refPercent)
	{
		if (refPercent > 1) refPercent = 1;
		var mat = TokenBar.Material as ShaderMaterial;
		float currentProgress = (float)mat.GetShaderParameter("progress");
		SetBarProgress(TokenBar, currentProgress, refPercent);
	}

	public void ShowPatienceCostRef(float refPercent)
	{
		_patienceHoverLock = true;
		var mat = PatienceBar.Material as ShaderMaterial;
		float currentProgress = (float)mat.GetShaderParameter("progress");
		SetBarProgress(PatienceBar, currentProgress, refPercent);
	}

	public void ClearTokenCostRef()
	{
		var mat = TokenBar.Material as ShaderMaterial;
		float p = (float)mat.GetShaderParameter("progress");
		SetBarProgress(TokenBar, p, p);
	}

	public void ClearPatienceCostRef()
	{
		_patienceHoverLock = false;
		var mat = PatienceBar.Material as ShaderMaterial;
		float p = (float)mat.GetShaderParameter("progress");
		SetBarProgress(PatienceBar, p, p);
	}

	public void ApplyTokenCost(float percent)
	{
		SetBarProgress(TokenBar, percent, percent);
	}


	public void RefreshPatienceProgress(float percent)
	{
		var mat = PatienceBar.Material as ShaderMaterial;
		mat.SetShaderParameter("progress", percent);
		if (!_patienceHoverLock)
			mat.SetShaderParameter("target", percent);
	}

	public void ApplyPatienceCost(float percent)
	{
		SetBarProgress(PatienceBar, percent, percent);
	}

}
