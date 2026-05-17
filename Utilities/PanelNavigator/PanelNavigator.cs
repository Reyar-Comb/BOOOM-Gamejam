using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 面板导航器。管理一组 PanelUnit，同一时刻只显示一个面板。
///
/// 用法：
/// 1. 在场景中将 PanelUnit 子节点拖到 PanelNavigator 下。
/// 2. 通过 RegisterButton(btn, "panelId") 将按钮绑定到目标面板。
/// 3. 调用 NavigateTo("panelId") 切换面板。
/// 4. 连接 PanelChanged 信号以响应切换事件。
/// 5. 切换前设置 CurrentVarType / CurrentVarName 作为跨面板共享信息。
/// </summary>
[GlobalClass]
public partial class PanelNavigator : Control
{
	// ========== 信号 ==========

	/// <summary>
	/// 面板切换完成时触发。
	/// fromId: 切换前的面板 ID（首次显示时为空字符串）
	/// toId:   切换后的面板 ID
	/// </summary>
	[Signal]
	public delegate void PanelChangedEventHandler(string fromId, string toId);

	/// <summary>
	/// 面板切换即将发生（OnNavigatedFrom / OnNavigatedTo 之前）时触发。
	/// </summary>
	[Signal]
	public delegate void PanelChangingEventHandler(string fromId, string toId);

	[Export]
	public VarRenderer VarRenderer { get; set; }

	// ========== 导出字段 ==========

	/// <summary>
	/// 可选的静态面板列表。如果为空，则自动扫描直接子节点中的 PanelUnit。
	/// </summary>
	[Export]
	public Godot.Collections.Array<PanelUnit> Panels { get; set; } = new();

	/// <summary>
	/// 是否在 _Ready 时自动扫描子节点中的 PanelUnit（默认开启）。
	/// </summary>
	[Export]
	public bool AutoDiscoverPanels { get; set; } = true;

	/// <summary>
	/// 是否将第一个面板设为默认显示（否则所有面板初始隐藏）。
	/// </summary>
	[Export]
	public bool ShowFirstPanelByDefault { get; set; } = true;

	/// <summary>
	/// 在编辑器中配置的按钮→面板绑定列表。
	/// 每一项指定一个按钮节点路径、目标面板 ID。
	/// 在 _Ready 时自动生效，无需写代码。
	/// </summary>
	[Export]
	public Godot.Collections.Array<ButtonBinding> ButtonBindings { get; set; } = new();

	// ========== Blackboard：跨面板共享状态 ==========

	/// <summary>
	/// 当前操作的变量类型（如 "int", "float", "status", "command" 等）。
	/// 在导航前由按钮 onPressed 设置，目标面板 OnNavigatedTo 中读取。
	/// GoBack / GoToRoot 会自动清空。
	/// </summary>
	public VarStats.VarType? CurrentVarType { get; set; } = null;

	/// <summary>
	/// 当前操作的变量名。
	/// </summary>
	public virtual string CurrentVarName { get; set; } = "";

	/// <summary>
	/// 上一次鼠标点击的Grid
	/// </summary>
	public virtual Vector2I LastClickedGrid { get; set; } = Vector2I.Zero;

	// ========== 私有字段 ==========

	private readonly Dictionary<string, PanelUnit> _panels = new();
	private readonly Stack<string> _history = new();
	private PanelUnit _currentPanel;
	private string _currentPanelId = "";
	private Vector2I _cachedHoveredCell = new(114514, 114514);
	private bool _isNavigatingBack;

	// ========== 生命周期 ==========

	public override void _Ready()
	{
		// 1. 从导出字段注册
		foreach (var panel in Panels)
		{
			if (panel != null && !string.IsNullOrEmpty(panel.PanelId))
				RegisterPanel(panel);
		}

		// 2. 自动扫描子节点
		if (AutoDiscoverPanels)
		{
			foreach (var child in GetChildren())
			{
				if (child is PanelUnit panel && !_panels.ContainsKey(panel.PanelId))
				{
					if (!string.IsNullOrEmpty(panel.PanelId))
						RegisterPanel(panel);
				}
			}
		}

		// 3. 初始显隐：默认显示第一个
		if (ShowFirstPanelByDefault && _panels.Count > 0)
		{
			var first = _panels.Values.First();
			first.Visible = true;
			_currentPanel = first;
			_currentPanelId = first.PanelId;
			first.OnNavigatedTo();
		}
		else
		{
			foreach (var kv in _panels)
				kv.Value.Visible = false;
		}

		// 4. 应用编辑器配置的按钮绑定
		foreach (var binding in ButtonBindings)
		{
			if (binding == null || binding.ButtonPath == null || string.IsNullOrEmpty(binding.TargetPanelId))
				continue;

			var btn = GetNodeOrNull<Node>(binding.ButtonPath);
			if (btn != null)
			{
				RegisterButton(btn, binding.TargetPanelId);
			}
			else
			{
				GD.PushWarning($"[PanelNavigator] 找不到按钮路径: {binding.ButtonPath}");
			}
		}

		VarRenderer.HoveredGridCellChanged += (cell, hasCell) =>
		{
			if (hasCell)
				_cachedHoveredCell = cell;
			else
				_cachedHoveredCell = new Vector2I(114514, 114514);
		};
		
	}

	public override void _Input(InputEvent @event)
	{
		if (BattleManager.Instance != null && BattleManager.Instance.State != BattleState.Running)
		{
			return;
		}

		if (@event is InputEventMouseButton mb
			&& mb.Pressed
			&& mb.ButtonIndex == MouseButton.Left
			&& _cachedHoveredCell.X != 114514)
		{
			if (VarRenderer.GetGlobalRect().HasPoint(VarRenderer.GetGlobalMousePosition()))
			{
				LastClickedGrid = _cachedHoveredCell;
				Debug.Print($"Clicked grid: {LastClickedGrid}");
			}
		}
	}

	// ========== 按钮辅助 ==========

	/// <summary>
	/// 尝试从任意节点中提取可点击的 BaseButton。
	/// 支持三种情况：
	/// 1. 节点本身就是 BaseButton → 直接返回。
	/// 2. 节点内部包含一个 Button 子节点（如 VarButton） → 返回内部 Button。
	/// 3. 都不满足 → 返回 null。
	/// </summary>
	private static BaseButton TryGetPressable(Node node)
	{
		if (node is BaseButton btn)
			return btn;

		// 深度优先搜索第一个 BaseButton 子节点
		foreach (var child in node.FindChildren("*", "BaseButton", true, false))
		{
			if (child is BaseButton childBtn)
				return childBtn;
		}
		return null;
	}

	// ========== 公开 API ==========

	/// <summary>
	/// 注册一个面板。重复 ID 会覆盖旧面板。
	/// </summary>
	public void RegisterPanel(PanelUnit panel)
	{
		if (panel == null || string.IsNullOrEmpty(panel.PanelId))
			return;

		panel.Navigator = this;
		_panels[panel.PanelId] = panel;
	}

	/// <summary>
	/// 注销一个面板。
	/// </summary>
	public void UnregisterPanel(string panelId)
	{
		if (_panels.TryGetValue(panelId, out var panel))
		{
			panel.Navigator = null;
			_panels.Remove(panelId);
		}
	}

	/// <summary>
	/// 获取指定 ID 的面板。
	/// </summary>
	public PanelUnit GetPanel(string panelId)
	{
		_panels.TryGetValue(panelId, out var panel);
		return panel;
	}

	/// <summary>
	/// 获取当前显示的面板 ID。
	/// </summary>
	public string CurrentPanelId => _currentPanelId;

	/// <summary>
	/// 切换到指定面板。
	/// </summary>
	/// <param name="panelId">目标面板 ID</param>
	public void NavigateTo(string panelId)
	{
		if (!_panels.TryGetValue(panelId, out var targetPanel))
		{
			GD.PushWarning($"[PanelNavigator] 找不到面板 ID: {panelId}");
			return;
		}

		// 如果目标是当前面板，仍然触发生命周期（允许同面板刷新）
		var fromId = _currentPanelId ?? "";

		// 子类可重写此钩子来集中处理导航前的业务逻辑
		OnBeforeNavigate(panelId);

		EmitSignal(SignalName.PanelChanging, fromId, panelId);

		// 记录导航历史（同面板刷新、回退或回到根时不记录）
		if (_currentPanel != null && fromId != panelId && !_isNavigatingBack)
			_history.Push(fromId);

		// 离开旧面板
		if (_currentPanel != null)
		{
			_currentPanel.OnNavigatedFrom();
			_currentPanel.Visible = false;
		}

		// 进入新面板
		targetPanel.Visible = true;
		targetPanel.OnNavigatedTo();

		_currentPanel = targetPanel;
		_currentPanelId = panelId;

		EmitSignal(SignalName.PanelChanged, fromId, panelId);
	}

	/// <summary>
	/// 导航前钩子。在 OnNavigatedFrom / OnNavigatedTo 之前调用。
	/// 在 PanelNavigator 子类中重写此方法，集中处理所有面板切换的业务逻辑。
	/// </summary>
	/// <param name="targetPanelId">目标面板 ID</param>
	protected virtual void OnBeforeNavigate(string targetPanelId) { }

	/// <summary>
	/// 返回上一层级面板。如果已在最顶层则无操作。
	/// 自动清空 CurrentVarType / CurrentVarName。
	/// </summary>
	public void GoBack()
	{
		if (_history.Count == 0) 
		{
			ClearBlackboard();
			return;
		}
		_isNavigatingBack = true;
		var prevId = _history.Pop();
		NavigateTo(prevId);
		_isNavigatingBack = false;
	}

	/// <summary>
	/// 一键清空历史栈，回到根面板（第一个注册的面板）。
	/// 自动清空 CurrentVarType / CurrentVarName。
	/// </summary>
	public void GoToRoot()
	{
		if (_panels.Count == 0) return;
		ClearBlackboard();
		_history.Clear();
		_isNavigatingBack = true;
		var rootId = _panels.Keys.First();
		NavigateTo(rootId);
		_isNavigatingBack = false;
	}

	private void ClearBlackboard()
	{
		CurrentVarType = null;
		CurrentVarName = "";
	}

	/// <summary>
	/// 在指定面板中按路径查找节点。便捷方法，等价于 GetPanel(id)?.GetNode&lt;T&gt;(path)。
	/// 示例：FindInPanel&lt;VarButton&gt;("VarUnit", "VBoxContainer/ScrollContainer/VBoxContainer/VarButton1");
	/// </summary>
	public T FindInPanel<T>(string panelId, string path) where T : Node
	{
		var panel = GetPanel(panelId);
		return panel?.GetNodeOrNull<T>(path);
	}

	/// <summary>
	/// 为节点注册导航行为：点击即导航到指定面板。
	/// 自动识别 BaseButton 或包含内部 Button 子节点的自定义控件（如 VarButton）。
	/// 若 targetPanelId 为空字符串，则仅执行 onPressed 回调，不触发导航。
	/// </summary>
	/// <param name="buttonNode">按钮节点（BaseButton 或包含 Button 子节点的任意 Node）</param>
	/// <param name="targetPanelId">目标面板 ID。传空字符串跳过导航。</param>
	/// <param name="onPressed">可选：点击回调，在导航之前执行（通常用于设置 CurrentVarType/CurrentVarName）</param>
	public void RegisterButton(Node buttonNode, string targetPanelId,
		Action onPressed = null,
		Action onHoverEnter = null,
		Action onHoverLeave = null)
	{
		var pressable = TryGetPressable(buttonNode);
		if (pressable == null)
		{
			GD.PushWarning($"[PanelNavigator] 节点 '{buttonNode?.Name}' 不包含可点击的 Button");
			return;
		}

		pressable.Pressed += () =>
		{
			onPressed?.Invoke();
			if (!string.IsNullOrEmpty(targetPanelId))
				NavigateTo(targetPanelId);
		};

		if (buttonNode is VarButton varBtn)
		{
			varBtn.OnHoverEnter = onHoverEnter;
			varBtn.OnHoverLeave = onHoverLeave;
		}
	}

	/// <summary>
	/// 扫描 target 节点下所有子节点，按名称匹配规则自动绑定。
	/// 按钮名称格式：NavBtn_&lt;PanelId&gt;  或  NavBtn_&lt;PanelId&gt;__&lt;任意后缀&gt;
	/// 例如：NavBtn_Settings  或  NavBtn_Shop_Main
	/// 支持普通 BaseButton 以及 VarButton 等内部包含 Button 的自定义控件。
	/// </summary>
	/// <param name="target">要扫描的根节点（通常为某个 PanelUnit）</param>
	public void AutoRegisterButtonsIn(Node target)
	{
		if (target == null) return;
		foreach (var child in target.FindChildren("*", "", true, false))
		{
			var name = child.Name.ToString();
			if (!name.StartsWith("NavBtn_")) continue;

			var parts = name.Substring(7).Split("__");
			var panelId = parts[0];
			if (_panels.ContainsKey(panelId))
			{
				RegisterButton(child, panelId);
			}
		}
	}

	/// <summary>
	/// 扫描所有已注册面板中的所有 NavBtn_ 按钮并自动绑定。
	/// </summary>
	public void AutoRegisterAllButtons()
	{
		foreach (var kv in _panels)
			AutoRegisterButtonsIn(kv.Value);
	}
}

