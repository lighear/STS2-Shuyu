using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A short magic-circle summon centered independently on every enemy struck by Yin Zhi Shu.
/// </summary>
public partial class NYinZhiShuVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_YinZhiShu.tscn";

    [Export]
    private Sprite2D? _outerGlow;

    [Export]
    private Sprite2D? _core;

    public static NYinZhiShuVfx? Create(Creature? target)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (creatureNode == null)
        {
            return null;
        }

        NYinZhiShuVfx vfx = VFXUtil.GenVFXNode<NYinZhiShuVfx>(ScenePath);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;
        return vfx;
    }

    public override void _Ready()
    {
        SetOpacity(0f);
        Scale = Vector2.One * 0.45f;
        TaskHelper.RunSafely(PlaySequence());
    }

    private async Task PlaySequence()
    {
        Tween spin = CreateTween();
        spin.TweenProperty(this, "rotation", Rotation + Mathf.DegToRad(120f), 0.38f)
            .SetTrans(Tween.TransitionType.Linear);

        Tween size = CreateTween();
        size.TweenProperty(this, "scale", Vector2.One, 0.16f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        size.TweenInterval(0.10f);
        size.TweenProperty(this, "scale", Vector2.One * 0.4f, 0.12f)
            .SetTrans(Tween.TransitionType.Linear);

        Tween visibility = CreateTween();
        visibility.TweenMethod(Callable.From<float>(SetOpacity), 0f, 1f, 0.16f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        visibility.TweenInterval(0.10f);
        visibility.TweenMethod(Callable.From<float>(SetOpacity), 1f, 0f, 0.12f)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(visibility, Tween.SignalName.Finished);

        this.QueueFreeSafely();
    }

    private void SetOpacity(float opacity)
    {
        if (_outerGlow?.Material is ShaderMaterial glowMaterial)
        {
            glowMaterial.SetShaderParameter("tint", new Color(0.88f, 0.95f, 1f, 0.24f * opacity));
        }

        if (_core?.Material is ShaderMaterial coreMaterial)
        {
            coreMaterial.SetShaderParameter("tint", new Color(1f, 1f, 1f, 0.62f * opacity));
        }
    }
}
