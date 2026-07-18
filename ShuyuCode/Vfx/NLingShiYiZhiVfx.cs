using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A pale full-screen freeze flash followed by lingering snow around the viewport edges.
/// </summary>
public partial class NLingShiYiZhiVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_LingShiYiZhi.tscn";

    private const float OpeningDelay = 0.15f;
    private const float WhiteFadeInDuration = 0.18f;
    private const float WhiteHoldDuration = 0.52f;
    private const float WhiteFadeOutDuration = 0.38f;
    private const float SnowHoldAfterWhite = 0.32f;
    private const float SnowFadeDuration = 0.60f;

    [Export]
    private ColorRect? _whiteout;

    [Export]
    private Node2D? _snowLayer;

    [Export]
    private GpuParticles2D? _topSnow;

    [Export]
    private GpuParticles2D? _bottomSnow;

    [Export]
    private GpuParticles2D? _leftSnow;

    [Export]
    private GpuParticles2D? _rightSnow;

    private CancellationTokenSource? _cts;

    public static async Task PlayOpening()
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null)
        {
            return;
        }

        NLingShiYiZhiVfx vfx = VFXUtil.GenVFXNode<NLingShiYiZhiVfx>(ScenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = vfx.GetViewportRect().Size * 0.5f;

        // Let the all-enemy hit land once the pale flash is nearly established.
        await Cmd.Wait(OpeningDelay);
    }

    public override void _Ready()
    {
        ConfigureForViewport();
        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
    }

    private void ConfigureForViewport()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 halfViewport = viewportSize * 0.5f;
        const float edgeInset = 34f;
        const float edgeBand = 58f;

        if (_whiteout != null)
        {
            _whiteout.Position = -halfViewport;
            _whiteout.Size = viewportSize;
            SetWhiteoutStrength(0f);
        }

        ConfigureEmitter(
            _topSnow,
            new Vector2(0f, -halfViewport.Y + edgeInset),
            new Vector3(viewportSize.X * 0.54f, edgeBand, 1f),
            Vector3.Down,
            Mathf.RoundToInt(Mathf.Clamp(viewportSize.X / 24f, 50f, 90f)));
        ConfigureEmitter(
            _bottomSnow,
            new Vector2(0f, halfViewport.Y - edgeInset),
            new Vector3(viewportSize.X * 0.54f, edgeBand, 1f),
            Vector3.Up,
            Mathf.RoundToInt(Mathf.Clamp(viewportSize.X / 24f, 50f, 90f)));
        ConfigureEmitter(
            _leftSnow,
            new Vector2(-halfViewport.X + edgeInset, 0f),
            new Vector3(edgeBand, viewportSize.Y * 0.54f, 1f),
            Vector3.Right,
            Mathf.RoundToInt(Mathf.Clamp(viewportSize.Y / 24f, 32f, 58f)));
        ConfigureEmitter(
            _rightSnow,
            new Vector2(halfViewport.X - edgeInset, 0f),
            new Vector3(edgeBand, viewportSize.Y * 0.54f, 1f),
            Vector3.Left,
            Mathf.RoundToInt(Mathf.Clamp(viewportSize.Y / 24f, 32f, 58f)));

        if (_snowLayer != null)
        {
            _snowLayer.Modulate = new Color(1f, 1f, 1f, 0f);
        }
    }

    private static void ConfigureEmitter(
        GpuParticles2D? emitter,
        Vector2 position,
        Vector3 extents,
        Vector3 direction,
        int amount)
    {
        if (emitter == null)
        {
            return;
        }

        emitter.Position = position;
        emitter.Amount = amount;

        if (emitter.ProcessMaterial is ParticleProcessMaterial sourceMaterial)
        {
            ParticleProcessMaterial material = (ParticleProcessMaterial)sourceMaterial.Duplicate();
            material.EmissionBoxExtents = extents;
            material.Direction = direction;
            emitter.ProcessMaterial = material;
        }

        emitter.Restart();
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();

        if (_snowLayer != null)
        {
            CreateTween()
                .TweenProperty(_snowLayer, "modulate:a", 1f, WhiteFadeInDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }

        if (_whiteout != null)
        {
            Tween white = CreateTween();
            white.TweenMethod(Callable.From<float>(SetWhiteoutStrength), 0f, 1f, WhiteFadeInDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            white.TweenInterval(WhiteHoldDuration);
            white.TweenMethod(Callable.From<float>(SetWhiteoutStrength), 1f, 0f, WhiteFadeOutDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            await ToSignal(white, Tween.SignalName.Finished);
        }
        else
        {
            await Cmd.Wait(WhiteFadeInDuration + WhiteHoldDuration + WhiteFadeOutDuration, _cts.Token);
        }

        await Cmd.Wait(SnowHoldAfterWhite, _cts.Token);

        foreach (GpuParticles2D? emitter in new[] { _topSnow, _bottomSnow, _leftSnow, _rightSnow })
        {
            if (emitter != null)
            {
                emitter.Emitting = false;
            }
        }

        if (_snowLayer != null)
        {
            Tween snowFade = CreateTween();
            snowFade.TweenProperty(_snowLayer, "modulate:a", 0f, SnowFadeDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.In);
            await ToSignal(snowFade, Tween.SignalName.Finished);
        }
        else
        {
            await Cmd.Wait(SnowFadeDuration, _cts.Token);
        }

        this.QueueFreeSafely();
    }

    private void SetWhiteoutStrength(float strength)
    {
        if (_whiteout?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("strength", strength);
        }
    }
}
