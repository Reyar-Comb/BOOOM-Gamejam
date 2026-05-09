using Godot;
using System;

/// <summary>
/// 导航面板单元。挂载在 PanelNavigator 下作为子节点，
/// 由 Navigator 统一管理显示/隐藏与切换生命周期。
/// 跨面板共享信息通过 Navigator.CurrentVarType / CurrentVarName 访问。
/// </summary>
[GlobalClass]
public partial class PanelUnit : Control
{
    /// <summary>
    /// 面板唯一标识符，Navigator 通过此 ID 切换面板。
    /// </summary>
    [Export]
    public string PanelId { get; set; }

    /// <summary>
    /// 所属的 PanelNavigator 引用，由 Navigator 在 _Ready 时自动赋值。
    /// </summary>
    public PanelNavigator Navigator { get; internal set; }

    /// <summary>
    /// 当导航进入本面板时调用。重写此方法以处理面板初始化/刷新逻辑。
    /// 通过 Navigator.CurrentVarType / CurrentVarName 读取跨面板共享信息。
    /// </summary>
    public virtual void OnNavigatedTo() { }

    /// <summary>
    /// 当从本面板导航离开时调用。重写此方法以处理清理逻辑。
    /// </summary>
    public virtual void OnNavigatedFrom() { }

    /// <summary>
    /// 从本面板导航到另一个面板（便捷方法，等价于 Navigator.NavigateTo）。
    /// 例如面板内部的按钮点击后调用：NavigateTo("ShopPanel");
    /// </summary>
    /// <param name="panelId">目标面板 ID</param>
    public void NavigateTo(string panelId)
    {
        Navigator?.NavigateTo(panelId);
    }
}