using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Shuyu.Vfx;

/// <summary>
/// Indicates that ShiTanPower can still grant block during the current turn.
/// The scene root is the wobble pivot marked by the user inside the bubble.
/// </summary>
public partial class NShiTanPowerVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.PowerVfxPath}/vfx_ShiTanPower.tscn";

    private const float IdleInterval = 2f;
    private const float TransitionDuration = 0.22f;
    private const float DialogueSizeMultiplier = 2f / 3f;
    private static readonly Vector2 BoundsAnchorRatio = new(1.03f, -0.06f);

    [Export]
    private Node2D? _visual;

    [Export]
    private Sprite2D? _dialogue;

    private CancellationTokenSource? _idleCancellation;
    private Tween? _motionTween;
    private NCreature? _creatureNode;
    private Node2D? _idleFollowAnchor;
    private Vector2 _basePosition;
    private Vector2 _baseScale = Vector2.One;
    private float _followBaselineLocalY;
    private bool _hasFollowBaseline;
    private bool _available;
    private bool _consuming;
    private bool _appearancePending;

    public override void _Ready()
    {
        Visible = false;
        ResetVisual();
    }

    public override void _Process(double delta)
    {
        if (_creatureNode != null &&
            (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding))
        {
            SetProcess(false);
            this.QueueFreeSafely();
            return;
        }

        if (_consuming)
        {
            return;
        }

        UpdateIdleVisibility();
    }

    public override void _ExitTree()
    {
        StopIdleMotion();
    }

    public void Configure(
        Vector2 boundsSize,
        NCreature creatureNode,
        Node2D? idleFollowAnchor)
    {
        _basePosition = boundsSize * BoundsAnchorRatio;
        _creatureNode = creatureNode;

        if (_idleFollowAnchor != idleFollowAnchor)
        {
            _idleFollowAnchor = idleFollowAnchor;
            _hasFollowBaseline = false;
        }

        CaptureFollowBaseline();
        UpdateFollowPosition();

        if (_dialogue?.Texture == null)
        {
            return;
        }

        float displayHeight = Mathf.Clamp(boundsSize.Y * 0.34f, 120f, 190f);
        float scale =
            displayHeight / _dialogue.Texture.GetHeight() * DialogueSizeMultiplier;
        _baseScale = Vector2.One * scale;

        if (!_available && !_consuming)
        {
            ResetVisual();
        }
    }

    public void SetAvailable(bool available, bool animateAppearance = false)
    {
        if (!available)
        {
            _available = false;
            _appearancePending = false;
            if (!_consuming)
            {
                HideImmediately();
            }
            return;
        }

        if (_available && !_consuming)
        {
            return;
        }

        StopIdleMotion();
        _available = true;
        _consuming = false;
        _appearancePending = animateAppearance;
        ResetVisual();
        StartIdleMotion();
        UpdateIdleVisibility();
    }

    public async Task ConsumeAsync()
    {
        if (!_available || _visual == null || _consuming)
        {
            return;
        }

        _available = false;
        _appearancePending = false;
        _consuming = true;
        StopIdleMotion();
        Visible = true;
        ResetVisual();

        _motionTween = CreateTween().SetParallel();
        _motionTween.TweenProperty(
                _visual,
                "scale",
                _baseScale * 1.48f,
                TransitionDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _motionTween.TweenProperty(
                _visual,
                "modulate:a",
                0f,
                TransitionDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        Tween consumeTween = _motionTween;
        await ToSignal(consumeTween, Tween.SignalName.Finished);

        _motionTween = null;
        _consuming = false;
        Visible = false;
        ResetVisual();
    }

    private void UpdateIdleVisibility()
    {
        if (!_available)
        {
            Visible = false;
            return;
        }

        bool isIdle = _creatureNode?.SpineAnimation.GetCurrentAnimationName() == "idle_loop";
        if (!isIdle)
        {
            if (Visible)
            {
                _motionTween?.Kill();
                _motionTween = null;
                Rotation = 0f;
                Visible = false;
                ResetVisual();
            }
            return;
        }

        UpdateFollowPosition();
        if (!Visible)
        {
            Visible = true;
            ResetVisual();
        }

        if (_appearancePending)
        {
            StartAppearance();
        }
    }

    private void StartAppearance()
    {
        if (_visual == null)
        {
            _appearancePending = false;
            return;
        }

        _appearancePending = false;
        _motionTween?.Kill();
        Rotation = 0f;
        _visual.Scale = _baseScale * 1.48f;
        _visual.Modulate = new Color(1f, 1f, 1f, 0f);

        _motionTween = CreateTween().SetParallel();
        _motionTween.TweenProperty(
                _visual,
                "scale",
                _baseScale,
                TransitionDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _motionTween.TweenProperty(
                _visual,
                "modulate:a",
                1f,
                TransitionDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    private void StartIdleMotion()
    {
        _idleCancellation = new CancellationTokenSource();
        TaskHelper.RunSafely(IdleLoop(_idleCancellation.Token));
    }

    private async Task IdleLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Cmd.Wait(IdleInterval, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !_available)
                {
                    return;
                }
                if (Visible)
                {
                    StartWobble();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The indicator was consumed, hidden, removed, or combat ended.
        }
    }

    private void StartWobble()
    {
        if (_visual == null || _consuming)
        {
            return;
        }

        _motionTween?.Kill();
        Rotation = 0f;
        _motionTween = CreateTween();
        _motionTween.TweenProperty(this, "rotation_degrees", -5f, 0.07f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _motionTween.TweenProperty(this, "rotation_degrees", 5f, 0.11f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _motionTween.TweenProperty(this, "rotation_degrees", -3f, 0.09f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _motionTween.TweenProperty(this, "rotation_degrees", 0f, 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    private void CaptureFollowBaseline()
    {
        if (_hasFollowBaseline || _idleFollowAnchor == null || GetParent() is not CanvasItem parent)
        {
            return;
        }

        Vector2 anchorLocal =
            parent.GetGlobalTransform().AffineInverse() * _idleFollowAnchor.GlobalPosition;
        _followBaselineLocalY = anchorLocal.Y;
        _hasFollowBaseline = true;
    }

    private void UpdateFollowPosition()
    {
        float followOffsetY = 0f;
        if (_hasFollowBaseline &&
            _idleFollowAnchor != null &&
            GetParent() is CanvasItem parent)
        {
            Vector2 anchorLocal =
                parent.GetGlobalTransform().AffineInverse() * _idleFollowAnchor.GlobalPosition;
            followOffsetY = anchorLocal.Y - _followBaselineLocalY;
        }

        Position = _basePosition + Vector2.Down * followOffsetY;
    }

    private void StopIdleMotion()
    {
        _idleCancellation?.Cancel();
        _idleCancellation = null;
        _motionTween?.Kill();
        _motionTween = null;
        Rotation = 0f;
    }

    private void HideImmediately()
    {
        StopIdleMotion();
        Visible = false;
        ResetVisual();
    }

    private void ResetVisual()
    {
        Rotation = 0f;
        if (_visual == null)
        {
            return;
        }
        _visual.Scale = _baseScale;
        _visual.Modulate = Colors.White;
    }
}
