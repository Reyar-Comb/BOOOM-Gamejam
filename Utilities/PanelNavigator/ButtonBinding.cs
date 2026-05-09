using Godot;

/// <summary>
/// 按钮→面板绑定配置。在 PanelNavigator 的 Inspector 中直接配置，
/// 无需写代码即可完成按钮与目标面板的连线。
///
/// 用法：
/// 1. 在 PanelNavigator 的 ButtonBindings 数组中新增一项。
/// 2. 设置 ButtonPath（指向场景中某个按钮节点的路径）。
/// 3. 设置 TargetPanelId（目标 PanelUnit 的 PanelId）。
/// </summary>
[GlobalClass]
public partial class ButtonBinding : Resource
{
    /// <summary>
    /// 目标按钮的节点路径（相对于 PanelNavigator）。
    /// 例如："MainMenuPanel/BtnSettings" 或 "ShopPanel/VBox/BtnBuy"。
    /// </summary>
    [Export]
    public NodePath ButtonPath { get; set; }

    /// <summary>
    /// 点击后导航到的目标面板 ID。
    /// </summary>
    [Export]
    public string TargetPanelId { get; set; }
}
