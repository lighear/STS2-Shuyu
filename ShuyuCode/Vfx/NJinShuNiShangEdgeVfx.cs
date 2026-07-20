using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A cold black-violet vignette that darkens the edge of the battlefield.
/// This is intentionally independent from the contracting fog ring.
/// </summary>
public partial class NJinShuNiShangEdgeVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_JinShuNiShangEdge.tscn";

    private const float FadeInDuration = 0.15f;
    private const float HoldDuration = 0.90f;
    private const float FadeOutDuration = 0.45f;

    [Export]
    private ColorRect? _edgeDarkness;

    public static void Play()
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null)
        {
            return;
        }

        NJinShuNiShangEdgeVfx vfx = VFXUtil.GenVFXNode<NJinShuNiShangEdgeVfx>(ScenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = vfx.GetViewportRect().Size * 0.5f;
    }

    public override void _Ready()
    {
        ConfigureForViewport();
        TaskHelper.RunSafely(PlaySequence());
    }

    private void ConfigureForViewport()
    {
        if (_edgeDarkness == null)
        {
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        _edgeDarkness.Position = viewportSize * -0.5f;
        _edgeDarkness.Size = viewportSize;
        SetStrength(0f);
    }

    private async Task PlaySequence()
    {
        Tween vignette = CreateTween();
        vignette.TweenMethod(Callable.From<float>(SetStrength), 0f, 1f, FadeInDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        vignette.TweenInterval(HoldDuration);
        vignette.TweenMethod(Callable.From<float>(SetStrength), 1f, 0f, FadeOutDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(vignette, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void SetStrength(float strength)
    {
        if (_edgeDarkness?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("strength", strength);
        }
    }
}
