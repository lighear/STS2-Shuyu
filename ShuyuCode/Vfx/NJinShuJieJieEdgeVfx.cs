using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A short black fog vignette for Jin Shu Jie Jie. Kept independent from the
/// character-centered shield so both parts can be tuned separately.
/// </summary>
public partial class NJinShuJieJieEdgeVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_JinShuJieJieEdge.tscn";

    private const float FadeInDuration = 0.16f;
    private const float HoldDuration = 0.92f;
    private const float FadeOutDuration = 0.42f;

    [Export]
    private ColorRect? _edgeDarkness;

    public static void Play()
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null)
        {
            return;
        }

        NJinShuJieJieEdgeVfx vfx = VFXUtil.GenVFXNode<NJinShuJieJieEdgeVfx>(ScenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = vfx.GetViewportRect().Size * 0.5f;
    }

    public override void _Ready()
    {
        if (_edgeDarkness == null)
        {
            this.QueueFreeSafely();
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        _edgeDarkness.Position = viewportSize * -0.5f;
        _edgeDarkness.Size = viewportSize;
        SetStrength(0f);
        TaskHelper.RunSafely(PlaySequence());
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
            material.SetShaderParameter("strength", strength * 0.88f);
        }
    }
}
