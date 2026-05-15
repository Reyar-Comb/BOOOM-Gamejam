using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ChoicePanel : Control
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
    private readonly List<StyleBoxFlat> _cardStyleBoxes = new()
    {
        ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxCommon.tres"),
        ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxUncommon.tres"),
        ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxRare.tres"),
        ResourceLoader.Load<StyleBoxFlat>("res://Main/ChoicePanel/CardStyleBoxes/CardStyleBoxSpecial.tres")
    };
    private readonly List<Control> _cards = new();
    private readonly Dictionary<Control, Vector2> _cardTargetPositions = new();
    private readonly List<Upgrade> _currentChoices = new();
    private readonly Dictionary<Control, Tween> _cardTweens = new();
    private Tween _activeTween;
    private TaskCompletionSource<Upgrade> _choiceCompletionSource;
    private bool _layoutCaptured;
    private bool _isOpen;
    private bool _isChoosing;
    private bool _choiceSelected;

    public override async void _Ready()
    {
        CaptureCards();
        ConnectCardInputs();
        SetBackgroundAlpha(0.0f);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        CaptureCardLayout();

        if (StartHidden)
        {
            SetHiddenState();
        }
    }

    public async void ShowPanel()
    {
        EnableInputBlock();
        await ShowPanelAsync();
    }

    public async void ClosePanel()
    {
        await ClosePanelAsync();
        DisableInputBlock();
    }

    public async Task<Upgrade> ChooseUpgradeAsync(IReadOnlyList<Upgrade> choices)
    {
        if (choices == null || choices.Count == 0)
        {
            return null;
        }
        GD.Print($"Presenting {choices.Count} upgrade choices to the player.");
        _choiceCompletionSource = new TaskCompletionSource<Upgrade>();
        _isChoosing = true;
        _choiceSelected = false;

        SetChoices(choices);
        await RelayoutCardsAsync();
        EnableInputBlock();
        await ShowPanelAsync();

        Upgrade selectedUpgrade = await _choiceCompletionSource.Task;
        await ClosePanelAsync();
        DisableInputBlock();

        _isChoosing = false;
        _choiceSelected = false;
        _choiceCompletionSource = null;
        return selectedUpgrade;
    }

    public async Task ShowPanelAsync()
    {
        await EnsureLayoutCapturedAsync();
        StopActiveTween();

        _isOpen = true;
        Visible = true;
        SetBackgroundAlpha(0.0f);
        PrepareCardsForShow();
        ResetCardVisuals();

        _activeTween = CreateTween();
        _activeTween.SetParallel(true);
        _activeTween.TweenMethod(Callable.From((float value) => 
        {
            GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, value);
            DimLayer.Modulate = new Color(DimLayer.Modulate, value);
        }
        ), 0f, 1f, BackgroundFadeDuration);

        int visibleIndex = 0;
        foreach (Control card in _cards)
        {
            if (!card.Visible)
            {
                continue;
            }

            Vector2 target = _cardTargetPositions[card];
            float delay = BackgroundFadeDuration + visibleIndex * CardStagger;

            _activeTween.TweenProperty(card, "global_position", target, CardFlyDuration)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            visibleIndex++;
        }

        await ToSignal(_activeTween, Tween.SignalName.Finished);
        _activeTween = null;
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

        _activeTween = CreateTween();
        _activeTween.SetParallel(true);
        _activeTween.TweenMethod(Callable.From((float value) => 
        {
            GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, value);
            DimLayer.Modulate = new Color(DimLayer.Modulate, value);
        }
        ), 1f, 0f, BackgroundFadeDuration);

        int visibleIndex = 0;
        foreach (Control card in _cards)
        {
            if (!card.Visible)
            {
                continue;
            }

            Vector2 target = _cardTargetPositions[card] + Vector2.Down * CardFlyDistance;
            float delay = visibleIndex * CardStagger;

            _activeTween.TweenProperty(card, "global_position", target, CardFlyDuration)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            visibleIndex++;
        }

        await ToSignal(_activeTween, Tween.SignalName.Finished);
        SetHiddenState();
        _isOpen = false;
        _activeTween = null;
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
            int choiceIndex = i;
            Control card = _cards[i];
            Control inputTarget = GetCardInputTarget(card);
            inputTarget.MouseFilter = MouseFilterEnum.Stop;
            inputTarget.MouseEntered += () => OnCardMouseEntered(card);
            inputTarget.MouseExited += () => OnCardMouseExited(card);
            inputTarget.GuiInput += inputEvent => OnCardGuiInput(choiceIndex, inputEvent);
        }
    }

    private void CaptureCardLayout()
    {
        _cardTargetPositions.Clear();

        foreach (Control card in _cards)
        {
            if (!card.Visible)
            {
                continue;
            }

            Vector2 target = card.GlobalPosition;
            card.TopLevel = true;
            card.GlobalPosition = target;
            card.PivotOffset = card.Size * 0.5f;
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

        CaptureCards();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        CaptureCardLayout();
    }

    private void PrepareCardsForShow()
    {
        foreach (Control card in _cards)
        {
            if (!card.Visible)
            {
                continue;
            }

            Vector2 target = _cardTargetPositions[card];
            card.TopLevel = true;
            card.GlobalPosition = target + Vector2.Down * CardFlyDistance;
            card.PivotOffset = card.Size * 0.5f;
        }
    }

    private void SetChoices(IReadOnlyList<Upgrade> choices)
    {
        _currentChoices.Clear();
        _choiceSelected = false;

        int visibleCount = Math.Min(choices.Count, _cards.Count);
        for (int i = 0; i < _cards.Count; i++)
        {
            bool hasChoice = i < visibleCount;
            Control card = _cards[i];
            card.TopLevel = false;
            card.Visible = hasChoice;

            if (!hasChoice)
            {
                continue;
            }

            Upgrade upgrade = choices[i];
            _currentChoices.Add(upgrade);
            StopCardTween(card);
            card.Scale = Vector2.One;
            card.Modulate = new Color(1f, 1f, 1f, 0f);
            SetCardContent(card, upgrade);
        }
    }

    private async Task RelayoutCardsAsync()
    {
        _layoutCaptured = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        CaptureCardLayout();
    }

    private void SetCardContent(Control card, Upgrade upgrade)
    {
        Control contentRoot = GetCardContentRoot(card);
        PanelContainer panel = GetCardPanel(card);
        Label nameLabel = contentRoot.GetNodeOrNull<Label>("MarginContainer/Content/NameLabel");
        Label descriptionLabel = contentRoot.GetNodeOrNull<Label>("MarginContainer/Content/DescriptionLabel");
        TextureRect iconTexture = contentRoot.GetNodeOrNull<TextureRect>("MarginContainer/Content/IconSlot/IconCenter/IconTexture");
        Label iconPlaceholder = contentRoot.GetNodeOrNull<Label>("MarginContainer/Content/IconSlot/IconCenter/IconPlaceholder");

        if (nameLabel != null)
        {
            nameLabel.Text = upgrade.Name;
            GD.Print($"Setting card name to: {upgrade.Name}");
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.Text = upgrade.Description;
        }

        if (iconTexture != null)
        {
            iconTexture.Texture = upgrade.Icon;
            iconTexture.Visible = upgrade.Icon != null;
        }

        if (iconPlaceholder != null)
        {
            iconPlaceholder.Visible = upgrade.Icon == null;
        }
        panel.Set("theme_override_styles/panel", _cardStyleBoxes[(int)upgrade.Rarity]);
    }

    private Control GetCardContentRoot(Control card)
    {
        return GetCardPanel(card) ?? card;
    }

    private PanelContainer GetCardPanel(Control card)
    {
        if (card is PanelContainer panel)
        {
            return panel;
        }

        return card.GetNodeOrNull<PanelContainer>("UnitChoiceCard");
    }

    private Control GetCardInputTarget(Control card)
    {
        return GetCardPanel(card) ?? card;
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
            ResetCardVisual(card);
        }
    }

    private void SetBackgroundAlpha(float alpha)
    {
        GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, alpha);
        DimLayer.Modulate = new Color(DimLayer.Modulate, alpha);
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
        if (_activeTween == null || !_activeTween.IsValid())
        {
            return;
        }

        _activeTween.Kill();
        _activeTween = null;
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
        if (!_isChoosing || _choiceSelected || !card.Visible)
        {
            return;
        }

        TweenCardScale(card, Vector2.One * CardHoverScale, CardHoverDuration);
    }

    private void OnCardMouseExited(Control card)
    {
        if (!_isChoosing || _choiceSelected || !card.Visible)
        {
            return;
        }

        TweenCardScale(card, Vector2.One, CardHoverDuration);
    }

    private async Task PlayCardSelectedAsync(Control selectedCard)
    {
        _choiceSelected = true;

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

    private async void OnCardGuiInput(int choiceIndex, InputEvent inputEvent)
    {
        if (!_isChoosing || _choiceSelected || _choiceCompletionSource == null || _choiceCompletionSource.Task.IsCompleted)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= _currentChoices.Count)
        {
            return;
        }

        if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed || mouseButton.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        await PlayCardSelectedAsync(_cards[choiceIndex]);
        _choiceCompletionSource.SetResult(_currentChoices[choiceIndex]);
    }
}
