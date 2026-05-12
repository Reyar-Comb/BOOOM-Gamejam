using Godot;
using System;

public partial class TooltipWindow : PanelContainer
{
	private RichTextLabel _label;
	private const float MaxWidth = 200f;

	public override void _Ready()
	{
		_label = GetNode<RichTextLabel>("MarginContainer/RichTextLabel");
		this.Hide();
	}

	public void SetContent(string content)
	{
		// 文本没变就跳过，避免每帧无意义地重建布局
		if (_label.Text == content)
			return;

		_label.Text = content;
		Size = Vector2.Zero;

		// // 先恢复自适应，让 RichTextLabel 自然撑开
		// _label.FitContent = true;
		// _label.AutowrapMode = TextServer.AutowrapMode.Off;
		// _label.CustomMinimumSize = Vector2.Zero;

		// // 延迟一帧，等 BBCode 解析 + 布局完成后再测量
		// CallDeferred(nameof(ClampWidth));
	}

	// private void ClampWidth()
	// {
	// 	float naturalW = _label.Size.X;

	// 	if (naturalW > MaxWidth)
	// 	{
	// 		// 锁定宽度、开启自动换行
	// 		_label.FitContent = false;
	// 		_label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
	// 		_label.CustomMinimumSize = new Vector2(MaxWidth, 0);

	// 		// 再延迟一帧，等换行完成后读高度
	// 		CallDeferred(nameof(FixHeight));
	// 	}
	// 	// 短文本：保持 fit_content=true，不做任何事
	// }

	// private void FixHeight()
	// {
	// 	// 换行后的实际高度
	// 	float h = _label.Size.Y;
	// 	if (h > 0)
	// 		_label.CustomMinimumSize = new Vector2(MaxWidth, h);
	// }
}
