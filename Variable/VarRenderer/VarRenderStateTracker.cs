using Godot;
using System;
using System.Collections.Generic;

internal sealed class VarRenderStateTracker
{
    private readonly VarRenderer _owner;
    private readonly Dictionary<Var, VarRenderState> _renderStatesByVar = new();
    private VarRendererConfig _config;

    public VarRenderStateTracker(VarRenderer owner, VarRendererConfig config)
    {
        _owner = owner;
        _config = config;
    }

    public void InjectConfig(VarRendererConfig config)
    {
        _config = config;
    }

    public void Update(Var renderedVar, double delta)
    {
        if (renderedVar?.Stats == null)
        {
            Reset(renderedVar);
            return;
        }

        VarRenderState renderState = Get(renderedVar);
        VarStats stats = renderedVar.Stats;
        Vector2 logicalPosition = stats.Position;

        if (!renderState.HasInterpolationState)
        {
            Initialize(renderState, logicalPosition);
            return;
        }

        renderState.TimeSinceLastPositionChange += delta;

        if (logicalPosition.DistanceSquaredTo(renderState.LastObservedPosition) > MathConstants.EpsilonSquared)
        {
            BeginInterpolation(renderState, stats, logicalPosition);
        }

        Advance(renderState, delta);
    }

    public VarRenderState Get(Var renderedVar)
    {
        if (!_renderStatesByVar.TryGetValue(renderedVar, out VarRenderState renderState))
        {
            renderState = new VarRenderState();
            _renderStatesByVar[renderedVar] = renderState;
        }

        return renderState;
    }

    public void Remove(Var renderedVar)
    {
        if (renderedVar != null)
        {
            _renderStatesByVar.Remove(renderedVar);
        }
    }

    public void Clear()
    {
        _renderStatesByVar.Clear();
    }

    private static void Initialize(VarRenderState renderState, Vector2 position)
    {
        renderState.DisplayPosition = position;
        renderState.LastObservedPosition = position;
        renderState.InterpolationStartPosition = position;
        renderState.InterpolationTargetPosition = position;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = 0.0;
        renderState.TimeSinceLastPositionChange = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;
        renderState.HasInterpolationState = true;
    }

    private void BeginInterpolation(VarRenderState renderState, VarStats stats, Vector2 logicalPosition)
    {
        Vector2 previousLogicalPosition = renderState.LastObservedPosition;
        double observedInterval = HasBeenSettledForTooLong(renderState) ? 0.0 : renderState.TimeSinceLastPositionChange;
        float displayDistance = renderState.DisplayPosition.DistanceTo(logicalPosition);

        renderState.LastObservedPosition = logicalPosition;
        renderState.TimeSinceLastPositionChange = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;

        if (!_config.InterpolateRenderPosition
            || displayDistance <= MathConstants.EpsilonSquared
            || ShouldSnap(displayDistance))
        {
            SnapToPosition(renderState, logicalPosition);
            return;
        }

        renderState.InterpolationStartPosition = renderState.DisplayPosition;
        renderState.InterpolationTargetPosition = logicalPosition;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = CalculateDuration(stats, previousLogicalPosition, logicalPosition, observedInterval);
    }

    private bool ShouldSnap(float distance)
    {
        return _config.SnapDistance > 0.0f && distance > _config.SnapDistance;
    }

    private bool HasBeenSettledForTooLong(VarRenderState renderState)
    {
        return _config.IdleInterpolationResetDelay >= 0.0f
            && renderState.TimeSinceInterpolationFinished > _config.IdleInterpolationResetDelay;
    }

    private double CalculateDuration(VarStats stats, Vector2 previousLogicalPosition, Vector2 logicalPosition, double observedInterval)
    {
        float logicalStepDistance = previousLogicalPosition.DistanceTo(logicalPosition);
        float duration = _config.FallbackInterpolationDuration;

        if (TryGetBattleManagerInterpolationDuration(out double battleManagerDuration))
        {
            duration = (float)battleManagerDuration;
        }
        else if (observedInterval > 0.0)
        {
            duration = (float)observedInterval;
        }
        else if (stats.MoveSpeed > 0.001f && logicalStepDistance > 0.001f)
        {
            duration = logicalStepDistance / stats.MoveSpeed;
        }

        float minimumDuration = Mathf.Max(0.0f, _config.MinimumInterpolationDuration);
        float maximumDuration = Mathf.Max(minimumDuration, _config.MaximumInterpolationDuration);
        return Mathf.Clamp(duration, minimumDuration, maximumDuration);
    }

    private bool TryGetBattleManagerInterpolationDuration(out double duration)
    {
        duration = 0.0;
        if (!_config.UseBattleManagerInterpolationDuration || _owner.BattleManager == null || _owner.BattleManager.TickScale <= 0.0f)
        {
            return false;
        }

        duration = _owner.BattleManager.TickInterval / _owner.BattleManager.TickScale;
        return duration > 0.0;
    }

    private static void Advance(VarRenderState renderState, double delta)
    {
        if (renderState.InterpolationDuration <= 0.0)
        {
            renderState.DisplayPosition = renderState.InterpolationTargetPosition;
            renderState.TimeSinceInterpolationFinished += delta;
            return;
        }

        if (renderState.InterpolationElapsed >= renderState.InterpolationDuration)
        {
            renderState.DisplayPosition = renderState.InterpolationTargetPosition;
            renderState.TimeSinceInterpolationFinished += delta;
            return;
        }

        renderState.InterpolationElapsed = Math.Min(renderState.InterpolationElapsed + delta, renderState.InterpolationDuration);
        float interpolationWeight = (float)(renderState.InterpolationElapsed / renderState.InterpolationDuration);
        renderState.DisplayPosition = renderState.InterpolationStartPosition.Lerp(renderState.InterpolationTargetPosition, interpolationWeight);
        renderState.TimeSinceInterpolationFinished = 0.0;
    }

    private static void SnapToPosition(VarRenderState renderState, Vector2 position)
    {
        renderState.DisplayPosition = position;
        renderState.InterpolationStartPosition = position;
        renderState.InterpolationTargetPosition = position;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;
    }

    private void Reset(Var renderedVar)
    {
        if (renderedVar == null || !_renderStatesByVar.TryGetValue(renderedVar, out VarRenderState renderState))
        {
            return;
        }

        renderState.DisplayPosition = Vector2.Zero;
        renderState.LastObservedPosition = Vector2.Zero;
        renderState.InterpolationStartPosition = Vector2.Zero;
        renderState.InterpolationTargetPosition = Vector2.Zero;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = 0.0;
        renderState.TimeSinceLastPositionChange = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;
        renderState.HasInterpolationState = false;
    }
}
