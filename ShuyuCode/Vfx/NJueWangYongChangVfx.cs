using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A short, viewport-sized snowfall that plays without delaying card resolution.
/// </summary>
public partial class NJueWangYongChangVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_JueWangYongChang.tscn";

    [Export]
    private GpuParticles2D? _snow;

    private CancellationTokenSource? _cts;

    public static NJueWangYongChangVfx? Create()
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null)
        {
            return null;
        }

        NJueWangYongChangVfx vfx = VFXUtil.GenVFXNode<NJueWangYongChangVfx>(ScenePath);
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
        await Cmd.Wait(1.55f, _cts.Token);

        if (_snow != null)
        {
            _snow.Emitting = false;
            Tween fade = CreateTween();
            fade.TweenProperty(_snow, "modulate:a", 0f, 0.45f)
                .SetTrans(Tween.TransitionType.Linear);
        }

        await Cmd.Wait(0.45f, _cts.Token);
        this.QueueFreeSafely();
    }
}
