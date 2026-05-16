using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class DeletePanel : Control
{
	[Export] public float BackgroundFadeDuration { get; set; } = 0.35f;
	[Export] public float CardFlyDuration { get; set; } = 0.38f;
	[Export] public float CardStagger { get; set; } = 0.09f;
	[Export] public float CardFlyDistance { get; set; } = 900.0f;
	[Export] public float CardHoverScale { get; set; } = 1.045f;
	[Export] public float CardSelectedPopScale { get; set; } = 1.1f;
	[Export] public float CardDimAlpha { get; set; } = 0.34f;
	[Export] public float CardHoverDuration { get; set; } = 0.12f;
	[Export] public float CardSelectPopDuration { get; set; } = 0.11f;
	[Export] public bool StartHidden { get; set; } = true;

	private ColorRect GaussianBlurLayer => field ??= GetNode<ColorRect>("GaussianBlurLayer");
	private ColorRect DimLayer => field ??= GetNode<ColorRect>("DimLayer");
	private HBoxContainer CardsContainer => field ??= GetNode<HBoxContainer>("SafeArea/CardCenter/Cards");
	private CenterContainer CardCenter => field ??= GetNode<CenterContainer>("SafeArea/CardCenter");
	private Label TitleLabel => field ??= GetNode<Label>("TitleLabel");
	private readonly List<StyleBoxFlat> _cardStyleBoxes = new()
	{
		ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxCommon.tres"),
		ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxUncommon.tres"),
		ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxRare.tres"),
		ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxSpecial.tres")
	};
	private readonly List<Control> _cards = new();
	private readonly List<Control> _activeSkillCards = new();
	private readonly Dictionary<Control, Vector2> _cardTargetPositions = new();
	private readonly List<Skill> _currentSkills = new();
	private readonly Dictionary<Control, Tween> _cardTweens = new();
	private readonly Dictionary<Control, int> _skillIndexesByCard = new();
	private Tween _backgroundTween;
	private TaskCompletionSource<Skill> _skillCompletionSource;
	private bool _layoutCaptured;
	private bool _isOpen;
	private bool _isSelecting;
	private bool _skillSelected;

	private async Task FrameDelay(int frame = 3)
	{
		for (int i = 0; i < frame; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}
	private void SetVisibility(bool visible)
	{
		Modulate = Modulate with { A = visible ? 1f : 0.01f };
	}
	public override async void _Ready()
	{
		CaptureCards();
		ConnectCardInputs();
		SetBackgroundAlpha(0.0f);

		await FrameDelay();
		ComputeCardTargetPositions();

		if (StartHidden)
		{
			SetHiddenState();
		}
	}

	public async void ShowPanel()
	{
		SetVisibility(true);
		EnableInputBlock();
		await ShowPanelAsync();
	}

	public async void ClosePanel()
	{
		await ClosePanelAsync();
		DisableInputBlock();
		SetVisibility(false);
	}

	/// <summary>
	/// Presents the player's currently owned skills and returns the one selected for deletion.
	/// Returns null if the skill list is empty.
	/// </summary>
	public async Task<Skill> ChooseSkillToDeleteAsync(IReadOnlyList<Skill> ownedSkills)
	{
		if (ownedSkills == null || ownedSkills.Count == 0)
		{
			return null;
		}
		GD.Print($"Presenting {ownedSkills.Count} owned skills for deletion choice.");
		_skillCompletionSource = new TaskCompletionSource<Skill>();
		_isSelecting = true;
		_skillSelected = false;

		SetSkills(ownedSkills);
		await RelayoutCardsAsync();
		EnableInputBlock();
		await ShowPanelAsync();

		Skill selectedSkill = await _skillCompletionSource.Task;
		await ClosePanelAsync();
		DisableInputBlock();

		_isSelecting = false;
		_skillSelected = false;
		_skillCompletionSource = null;
		return selectedSkill;
	}

	public async Task ShowPanelAsync()
	{
		await EnsureLayoutCapturedAsync();
		StopActiveTween();

		SetVisibility(true);
		SetBackgroundAlpha(0.0f);
		PrepareCardsForShow();
		ResetCardVisuals();

		_backgroundTween = CreateTween();
		_backgroundTween.SetParallel(true);
		_backgroundTween.TweenMethod(Callable.From((float value) =>
		{
			GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, value);
			DimLayer.Modulate = new Color(DimLayer.Modulate, value);
			TitleLabel.Modulate = new Color(TitleLabel.Modulate, value);
		}
		), 0f, 1f, BackgroundFadeDuration);

		List<Control> animatedCards = GetAnimatedCards();
		for (int i = 0; i < animatedCards.Count; i++)
		{
			Control card = animatedCards[i];
			Vector2 target = _cardTargetPositions[card];
			float delay = BackgroundFadeDuration + i * CardStagger;

			_backgroundTween.TweenProperty(card, "global_position", target, CardFlyDuration)
				.SetDelay(delay)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
		}

		await ToSignal(_backgroundTween, Tween.SignalName.Finished);
		_isOpen = true;
		_backgroundTween = null;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (_isOpen && @event is InputEventMouseButton)
		{
			AcceptEvent();
		}
	}

	public async Task ClosePanelAsync()
	{
		await EnsureLayoutCapturedAsync();
		StopActiveTween();

		_backgroundTween = CreateTween();
		_backgroundTween.SetParallel(true);
		_backgroundTween.TweenMethod(Callable.From((float value) =>
		{
			GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, value);
			DimLayer.Modulate = new Color(DimLayer.Modulate, value);
			TitleLabel.Modulate = new Color(TitleLabel.Modulate, value);
		}
		), 1f, 0f, BackgroundFadeDuration);

		List<Control> animatedCards = GetAnimatedCards();
		for (int i = 0; i < animatedCards.Count; i++)
		{
			Control card = animatedCards[i];
			Vector2 target = _cardTargetPositions[card] + Vector2.Down * CardFlyDistance;
			float delay = i * CardStagger;

			_backgroundTween.TweenProperty(card, "global_position", target, CardFlyDuration)
				.SetDelay(delay)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
		}

		await ToSignal(_backgroundTween, Tween.SignalName.Finished);
		SetHiddenState();
		_isOpen = false;
		_backgroundTween = null;
	}

	private void CaptureCards()
	{
		_cards.Clear();

		foreach (Node child in CardsContainer.GetChildren())
		{
			if (child is Control card)
			{
				_cards.Add(card);
			}
		}
	}

	private void ConnectCardInputs()
	{
		for (int i = 0; i < _cards.Count; i++)
		{
			Control card = _cards[i];
			card.MouseFilter = MouseFilterEnum.Stop;
			card.MouseEntered += () => OnCardMouseEntered(card);
			card.MouseExited += () => OnCardMouseExited(card);
			card.GuiInput += inputEvent => OnCardGuiInput(card, inputEvent);
		}
	}

	private void ComputeCardTargetPositions()
	{
		_cardTargetPositions.Clear();

		List<Control> cards = GetAnimatedCards();
		if (cards.Count == 0)
		{
			_layoutCaptured = true;
			return;
		}

		// Calculate total width of all animated cards including separation
		float separation = CardsContainer.GetThemeConstant("separation");
		float totalWidth = 0f;
		float maxHeight = 0f;
		foreach (Control card in cards)
		{
			totalWidth += card.Size.X;
			maxHeight = Math.Max(maxHeight, card.Size.Y);
		}
		totalWidth += separation * (cards.Count - 1);

		// Use CardCenter (CenterContainer) as the reference area for centering
		Vector2 areaPos = CardCenter.GlobalPosition;
		Vector2 areaSize = CardCenter.Size;

		// HBoxContainer alignment = CENTER (1)
		float startX = areaPos.X + (areaSize.X - totalWidth) / 2f;
		float currentX = startX;

		foreach (Control card in cards)
		{
			float cardY = areaPos.Y + (areaSize.Y - card.Size.Y) / 2f;
			Vector2 target = new Vector2(currentX, cardY);
			currentX += card.Size.X + separation;

			card.TopLevel = true;
			card.GlobalPosition = target;
			card.PivotOffset = card.Size * 0.5f;
			card.ZIndex = 100;
			_cardTargetPositions[card] = target;
		}

		_layoutCaptured = true;
	}

	private async Task EnsureLayoutCapturedAsync()
	{
		if (_layoutCaptured)
		{
			return;
		}

		await FrameDelay();
		ComputeCardTargetPositions();
	}

	private void PrepareCardsForShow()
	{
		foreach (Control card in GetAnimatedCards())
		{
			Vector2 target = _cardTargetPositions.TryGetValue(card, out Vector2 storedPosition)
				? storedPosition
				: card.GlobalPosition;
			card.TopLevel = true;
			card.GlobalPosition = target + Vector2.Down * CardFlyDistance;
			card.PivotOffset = card.Size * 0.5f;
			card.ZIndex = 100;
		}
	}

	private void SetSkills(IReadOnlyList<Skill> skills)
	{
		_currentSkills.Clear();
		_activeSkillCards.Clear();
		_skillIndexesByCard.Clear();
		_skillSelected = false;

		int visibleCount = Math.Min(skills.Count, _cards.Count);
		for (int i = 0; i < _cards.Count; i++)
		{
			bool hasSkill = i < visibleCount;
			Control card = _cards[i];
			// Keep TopLevel=true — positions are computed in ComputeCardTargetPositions
			card.Visible = hasSkill;

			if (!hasSkill)
			{
				continue;
			}

			Skill skill = skills[i];
			_currentSkills.Add(skill);
			_activeSkillCards.Add(card);
			_skillIndexesByCard[card] = i;
			StopCardTween(card);
			card.Scale = Vector2.One;
			card.Modulate = new Color(1f, 1f, 1f, 0f);
			card.ZIndex = 100;
			SetCardContent(card, skill);
		}
	}

	private async Task RelayoutCardsAsync()
	{
		_layoutCaptured = false;
		// Only need 1 frame for card-internal layout (text wrapping) to settle.
		// Container-level positioning is computed manually, not read from the tree.
		await FrameDelay(1);
		ComputeCardTargetPositions();
	}

	private void SetCardContent(Control card, Skill skill)
	{
		Label nameLabel = card.GetNodeOrNull<Label>("MarginContainer/Content/NameLabel");
		Label descriptionLabel = card.GetNodeOrNull<Label>("MarginContainer/Content/DescriptionLabel");
		TextureRect iconTexture = card.GetNodeOrNull<TextureRect>("MarginContainer/Content/IconSlot/IconCenter/IconTexture");
		Label iconPlaceholder = card.GetNodeOrNull<Label>("MarginContainer/Content/IconSlot/IconCenter/IconPlaceholder");

		if (nameLabel != null)
		{
			nameLabel.Text = skill.Name;
			GD.Print($"Setting card name to: {skill.Name}");
		}

		if (descriptionLabel != null)
		{
			descriptionLabel.Text = skill.Description;
		}

		if (iconTexture != null)
		{
			iconTexture.Texture = skill.Icon;
			iconTexture.Visible = skill.Icon != null;
		}

		if (iconPlaceholder != null)
		{
			iconPlaceholder.Visible = skill.Icon == null;
		}
		card.Set("theme_override_styles/panel", _cardStyleBoxes[(int)skill.Rarity]);
	}

	private void SetHiddenState()
	{
		SetBackgroundAlpha(0.0f);

		foreach (Control card in _cards)
		{
			Vector2 target = _cardTargetPositions.TryGetValue(card, out Vector2 storedPosition)
				? storedPosition
				: card.GlobalPosition;
			card.TopLevel = true;
			card.GlobalPosition = target + Vector2.Down * CardFlyDistance;
			card.ZIndex = 0;
			ResetCardVisual(card);
		}
	}

	private List<Control> GetAnimatedCards()
	{
		return _activeSkillCards.Count > 0 ? _activeSkillCards : _cards;
	}

	private void SetBackgroundAlpha(float alpha)
	{
		GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, alpha);
		DimLayer.Modulate = new Color(DimLayer.Modulate, alpha);
		TitleLabel.Modulate = new Color(TitleLabel.Modulate, alpha);
	}

	private void EnableInputBlock()
	{
		MouseFilter = MouseFilterEnum.Stop;
		GaussianBlurLayer.MouseFilter = MouseFilterEnum.Stop;
		DimLayer.MouseFilter = MouseFilterEnum.Stop;
	}

	private void DisableInputBlock()
	{
		GaussianBlurLayer.MouseFilter = MouseFilterEnum.Ignore;
		DimLayer.MouseFilter = MouseFilterEnum.Ignore;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	private void StopActiveTween()
	{
		if (_backgroundTween == null || !_backgroundTween.IsValid())
		{
			return;
		}

		_backgroundTween.Kill();
		_backgroundTween = null;
	}

	private void StopCardTween(Control card)
	{
		if (!_cardTweens.TryGetValue(card, out Tween tween))
		{
			return;
		}

		if (tween != null && tween.IsValid())
		{
			tween.Kill();
		}

		_cardTweens.Remove(card);
	}

	private void ResetCardVisuals()
	{
		foreach (Control card in _cards)
		{
			ResetCardVisual(card);
		}
	}

	private void ResetCardVisual(Control card)
	{
		StopCardTween(card);
		card.Scale = Vector2.One;
		card.Modulate = Colors.White;
	}

	private void TweenCardScale(Control card, Vector2 targetScale, float duration)
	{
		StopCardTween(card);

		Tween tween = CreateTween();
		tween.TweenProperty(card, "scale", targetScale, duration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_cardTweens[card] = tween;
		tween.Finished += () => _cardTweens.Remove(card);
	}

	private void OnCardMouseEntered(Control card)
	{
		if (!_isSelecting || _skillSelected || !card.Visible || !_isOpen)
		{
			return;
		}
		AudioManager.Instance.PlaySFX("hover");
		GD.Print("Mouse entered card: " + card.Name);
		TweenCardScale(card, Vector2.One * CardHoverScale, CardHoverDuration);
	}

	private void OnCardMouseExited(Control card)
	{
		if (!_isSelecting || _skillSelected || !card.Visible || !_isOpen)
		{
			return;
		}
		GD.Print("Mouse exited card: " + card.Name);
		TweenCardScale(card, Vector2.One, CardHoverDuration);
	}

	private async Task PlayCardSelectedAsync(Control selectedCard)
	{
		foreach (Control card in _cards)
		{
			StopCardTween(card);
			card.Scale = Vector2.One;

			if (card != selectedCard)
			{
				card.Modulate = new Color(1f, 1f, 1f, CardDimAlpha);
			}
		}

		Tween tween = CreateTween();
		_cardTweens[selectedCard] = tween;
		tween.TweenProperty(selectedCard, "scale", Vector2.One * CardSelectedPopScale, CardSelectPopDuration)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(selectedCard, "scale", Vector2.One, CardSelectPopDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.In);

		await ToSignal(tween, Tween.SignalName.Finished);
		_cardTweens.Remove(selectedCard);
	}

	private async void OnCardGuiInput(Control card, InputEvent inputEvent)
	{
		if (!_isSelecting || _skillSelected || _skillCompletionSource == null || _skillCompletionSource.Task.IsCompleted || !_isOpen)
		{
			return;
		}

		if (!_skillIndexesByCard.TryGetValue(card, out int skillIndex))
		{
			return;
		}

		if (skillIndex < 0 || skillIndex >= _currentSkills.Count)
		{
			return;
		}

		if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed || mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}
		GD.Print("Skill card clicked for deletion: " + _currentSkills[skillIndex].Name);
		AudioManager.Instance.PlaySFX("select_skill");
		_skillSelected = true;
		await PlayCardSelectedAsync(card);
		_skillCompletionSource.SetResult(_currentSkills[skillIndex]);
	}
}
