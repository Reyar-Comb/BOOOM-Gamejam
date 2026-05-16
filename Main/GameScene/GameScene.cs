using Godot;

public partial class GameScene : Node2D
{
    private PauseMenu PauseMenu => field ??= GetNode<PauseMenu>("PauseMenuLayer/PauseMenu");

    public override void _Ready()
    {
        CallDeferred(MethodName.ConnectBattleManager);
    }

    public override void _ExitTree()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.StateChanged -= OnBattleStateChanged;
        }
    }

    public void TogglePauseMenu()
    {
        if (BattleManager.Instance is { CanOpenPauseMenu: false })
        {
            PauseMenu.Close();
            return;
        }

        PauseMenu.Toggle();
    }

    private void ConnectBattleManager()
    {
        if (BattleManager.Instance == null)
        {
            GD.PushWarning("GameScene could not connect pause menu state handling because BattleManager.Instance is null.");
            return;
        }

        BattleManager.Instance.StateChanged += OnBattleStateChanged;
        OnBattleStateChanged(BattleManager.Instance.State);
    }

    private void OnBattleStateChanged(BattleState state)
    {
        if (state == BattleState.BeforeWaveEnd || state == BattleState.Choice)
        {
            PauseMenu.Close();
        }
    }
}
