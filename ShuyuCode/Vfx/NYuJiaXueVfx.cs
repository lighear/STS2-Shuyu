using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// Yu Jia Xue's shared short snowfall for play, freeze, and unfreeze triggers.
/// It keeps Jue Wang Yong Chang's particle speed while running for half as long.
/// </summary>
public partial class NYuJiaXueVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_YuJiaXue.tscn";

    private const float EmissionDuration = 0.775f;
    private const float FadeDuration = 0.225f;

    [Export]
    private GpuParticles2D? _snow;

    private CancellationTokenSource? _cts;

    public static NYuJiaXueVfx? Create()
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null)
        {
            return null;
        }

        NYuJiaXueVfx vfx = VFXUtil.GenVFXNode<NYuJiaXueVfx>(ScenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = vfx.GetViewportRect().Size * 0.5f;
        return vfx;
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
        if (_snow == null)
        {
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        _snow.Position = new Vector2(0f, -viewportSize.Y * 0.5f - 72f);

        if (_snow.ProcessMaterial is ParticleProcessMaterial sourceMaterial)
        {
            ParticleProcessMaterial material = (ParticleProcessMaterial)sourceMaterial.Duplicate();
            material.EmissionBoxExtents = new Vector3(viewportSize.X * 0.55f, 72f, 1f);
            material.InitialVelocityMin = viewportSize.Y * 0.35f;
            material.InitialVelocityMax = viewportSize.Y * 0.65f;
            material.Gravity = new Vector3(viewportSize.X * 0.015f, viewportSize.Y * 0.08f, 0f);
            _snow.ProcessMaterial = material;
        }

        _snow.Restart();
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        await Cmd.Wait(EmissionDuration, _cts.Token);

        if (_snow != null)
        {
            _snow.Emitting = false;
            Tween fade = CreateTween();
            fade.TweenProperty(_snow, "modulate:a", 0f, FadeDuration)
                .SetTrans(Tween.TransitionType.Linear);
        }

        await Cmd.Wait(FadeDuration, _cts.Token);
        this.QueueFreeSafely();
    }
}
