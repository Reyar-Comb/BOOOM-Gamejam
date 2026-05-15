using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ChoicePanel : Control
{
    private const string LogPrefix = "[ChoicePanel] ";

    [Export] public float BackgroundFadeDuration { get; set; } = 0.35f;
    [Export] public float CardFlyDuration { get; set; } = 0.38f;
    [Export] public float CardStagger { get; set; } = 0.09f;
    [Export] public float CardFlyDistance { get; set; } = 900.0f;
    [Export] public float CardHoverScale { get; set; } = 1.045f;
    [Export] public float CardSelectedPopScale { get; set; } = 1.1f;
    [Export] public float CardDimAlpha { get; set; } = 0.34f;
    [Export] public float CardHoverDuration { get; set; } = 0.12f;
    [Export] public float CardSelectPopDuration { get; set; } = 0.11f;
    [Export] public float CardSeparation { get; set; } = 42.0f;
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
    private readonly List<Control> _activeChoiceCards = new();
    private readonly List<Upgrade> _currentChoices = new();
    private readonly Dictionary<Control, Tween> _cardTweens = new();
    private readonly Dictionary<Control, int> _choiceIndexesByCard = new();
    private Tween _backgroundTween;
    private TaskCompletionSource<Upgrade> _choiceCompletionSource;
    private bool _isOpen;
    private bool _isChoosing;
    private bool _choiceSelected;

    private static void Log(string message)
    {
        GD.Print(LogPrefix + message);
    }

    private async Task FrameDelay(int frame = 2)
    {
        for (int i = 0; i < frame; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    public override async void _Ready()
    {
        CaptureCards();
        ConnectCardInputs();
        SetBackgroundAlpha(0.0f);

        await FrameDelay();
        PrepareCardLayout(GetAnimatedCards());

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
        // Log($"Presenting {choices.Count} upgrade choices to the player.");
        _choiceCompletionSource = new TaskCompletionSource<Upgrade>();
        _isChoosing = true;
        _choiceSelected = false;

        SetChoices(choices);
        await RelayoutCardsAsync();
        EnableInputBlock();
        await ShowPanelAsync();
        // Log($"Showing upgrade choices to the player.");
        Upgrade selectedUpgrade = await _choiceCompletionSource.Task;
        // Log($"Player selected upgrade: {selectedUpgrade.Name}");
        await ClosePanelAsync();
        // Log($"Disabling input block after choice selection.");
        DisableInputBlock();

        _isChoosing = false;
        _choiceSelected = false;
        _choiceCompletionSource = null;
        return selectedUpgrade;
    }

    public async Task ShowPanelAsync()
    {
        await EnsureCardLayoutReadyAsync();
        StopActiveTween();

        Visible = true;
        SetBackgroundAlpha(0.0f);
        PrepareCardsForShow();
        ResetCardVisuals();

        _backgroundTween = CreateTween();
        _backgroundTween.SetParallel(true);
        _backgroundTween.TweenMethod(Callable.From((float value) =>
        {
            GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, value);
            DimLayer.Modulate = new Color(DimLayer.Modulate, value);
        }
        ), 0f, 1f, BackgroundFadeDuration);

        List<Control> animatedCards = GetAnimatedCards();
        for (int i = 0; i < animatedCards.Count; i++)
        {
            Control card = animatedCards[i];
            Vector2 target = GetCardShowPosition(animatedCards, i);
            float delay = BackgroundFadeDuration + i * CardStagger;

            _backgroundTween.TweenProperty(card, "global_position", target, CardFlyDuration)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }

        await ToSignal(_backgroundTween, Tween.SignalName.Finished);
        // Log("Finished show tween. Cards are at " + string.Join(", ", animatedCards.ConvertAll(c => c.GlobalPosition.ToString())));
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
        await EnsureCardLayoutReadyAsync();
        StopActiveTween();

        _backgroundTween = CreateTween();
        _backgroundTween.SetParallel(true);
        _backgroundTween.TweenMethod(Callable.From((float value) =>
        {
            GaussianBlurLayer.Modulate = new Color(GaussianBlurLayer.Modulate, value);
            DimLayer.Modulate = new Color(DimLayer.Modulate, value);
        }
        ), 1f, 0f, BackgroundFadeDuration);

        List<Control> animatedCards = GetAnimatedCards();
        for (int i = 0; i < animatedCards.Count; i++)
        {
            Control card = animatedCards[i];
            Vector2 target = GetCardClosePosition(animatedCards, i);
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

    private void PrepareCardLayout(IReadOnlyList<Control> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Control card = cards[i];
            Vector2 target = GetCardShowPosition(cards, i);
            card.TopLevel = true;
            card.GlobalPosition = target;
            card.PivotOffset = card.Size * 0.5f;
            card.ZIndex = 100;
        }
    }

    private async Task EnsureCardLayoutReadyAsync()
    {
        foreach (Control card in GetAnimatedCards())
        {
            if (card.Size == Vector2.Zero)
            {
                await FrameDelay();
                return;
            }
        }
    }

    private Vector2 GetCardShowPosition(IReadOnlyList<Control> cards, int cardIndex)
    {
        Rect2 panelRect = GetGlobalRect();
        float totalWidth = GetTotalCardsWidth(cards);
        float maxHeight = GetMaxCardHeight(cards);
        Vector2 cardSize = GetCardLayoutSize(cards[cardIndex]);
        float x = panelRect.Position.X + (panelRect.Size.X - totalWidth) * 0.5f;

        for (int i = 0; i < cardIndex; i++)
        {
            x += GetCardLayoutSize(cards[i]).X + CardSeparation;
        }

        float groupTop = panelRect.Position.Y + (panelRect.Size.Y - maxHeight) * 0.5f;
        float y = groupTop + (maxHeight - cardSize.Y) * 0.5f;
        return new Vector2(x, y);
    }

    private Vector2 GetCardClosePosition(IReadOnlyList<Control> cards, int cardIndex)
    {
        return GetCardShowPosition(cards, cardIndex) + Vector2.Down * CardFlyDistance;
    }

    private float GetTotalCardsWidth(IReadOnlyList<Control> cards)
    {
        if (cards.Count == 0)
        {
            return 0f;
        }

        float width = CardSeparation * (cards.Count - 1);
        foreach (Control card in cards)
        {
            width += GetCardLayoutSize(card).X;
        }

        return width;
    }

    private static float GetMaxCardHeight(IReadOnlyList<Control> cards)
    {
        float maxHeight = 0f;
        foreach (Control card in cards)
        {
            maxHeight = Math.Max(maxHeight, GetCardLayoutSize(card).Y);
        }

        return maxHeight;
    }

    private static Vector2 GetCardLayoutSize(Control card)
    {
        return card.Size == Vector2.Zero ? card.CustomMinimumSize : card.Size;
    }

    private void PrepareCardsForShow()
    {
        List<Control> animatedCards = GetAnimatedCards();
        for (int i = 0; i < animatedCards.Count; i++)
        {
            Control card = animatedCards[i];
            card.TopLevel = true;
            card.GlobalPosition = GetCardClosePosition(animatedCards, i);
            card.PivotOffset = card.Size * 0.5f;
            card.ZIndex = 100;
        }
    }

    private void SetChoices(IReadOnlyList<Upgrade> choices)
    {
        _currentChoices.Clear();
        _activeChoiceCards.Clear();
        _choiceIndexesByCard.Clear();
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
            _activeChoiceCards.Add(card);
            _choiceIndexesByCard[card] = i;
            StopCardTween(card);
            card.Scale = Vector2.One;
            card.Modulate = new Color(1f, 1f, 1f, 0f);
            card.ZIndex = 100;
            SetCardContent(card, upgrade);
        }
    }

    private async Task RelayoutCardsAsync()
    {
        await FrameDelay();
        PrepareCardLayout(GetAnimatedCards());
    }

    private void SetCardContent(Control card, Upgrade upgrade)
    {
        Label nameLabel = card.GetNodeOrNull<Label>("MarginContainer/Content/NameLabel");
        Label descriptionLabel = card.GetNodeOrNull<Label>("MarginContainer/Content/DescriptionLabel");
        TextureRect iconTexture = card.GetNodeOrNull<TextureRect>("MarginContainer/Content/IconSlot/IconCenter/IconTexture");
        Label iconPlaceholder = card.GetNodeOrNull<Label>("MarginContainer/Content/IconSlot/IconCenter/IconPlaceholder");

        if (nameLabel != null)
        {
            nameLabel.Text = upgrade.Name;
            // Log($"Setting card name to: {upgrade.Name}");
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
        card.Set("theme_override_styles/panel", _cardStyleBoxes[(int)upgrade.Rarity]);
    }

    private void SetHiddenState()
    {
        SetBackgroundAlpha(0.0f);

        for (int i = 0; i < _cards.Count; i++)
        {
            Control card = _cards[i];
            card.TopLevel = true;
            card.GlobalPosition = GetCardClosePosition(_cards, i);
            card.ZIndex = 0;
            ResetCardVisual(card);
        }
    }

    private List<Control> GetAnimatedCards()
    {
        return _activeChoiceCards.Count > 0 ? _activeChoiceCards : _cards;
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
        if (!_isChoosing || _choiceSelected || !card.Visible || !_isOpen)
        {
            return;
        }
        // Log("Mouse entered card: " + card.Name);
        TweenCardScale(card, Vector2.One * CardHoverScale, CardHoverDuration);
    }

    private void OnCardMouseExited(Control card)
    {
        if (!_isChoosing || _choiceSelected || !card.Visible || !_isOpen)
        {
            return;
        }
        // Log("Mouse exited card: " + card.Name);
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
        if (!_isChoosing || _choiceSelected || _choiceCompletionSource == null || _choiceCompletionSource.Task.IsCompleted || !_isOpen)
        {
            return;
        }

        if (!_choiceIndexesByCard.TryGetValue(card, out int choiceIndex))
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
        // Log("Choice card clicked: " + _currentChoices[choiceIndex].Name);
        _choiceSelected = true;
        await PlayCardSelectedAsync(card);
        _choiceCompletionSource.SetResult(_currentChoices[choiceIndex]);
    }
}
