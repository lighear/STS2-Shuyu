using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A brief radial ice crack that opens over the struck creature.
/// </summary>
public partial class NSuiLieWuShengVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_SuiLieWuSheng.tscn";

    private const float CrackOpenDuration = 0.08f;
    private const float CrackHoldDuration = 0.10f;
    private const float CrackFadeDuration = 0.15f;
    private const float SourceTextureSize = 2000f;

    [Export]
    private Sprite2D? _glowCracks;

    [Export]
    private Sprite2D? _coreCracks;

    private float _targetScale = 0.17f;

    public static async Task Play(Creature? target)
    {
        if (TestMode.IsOn || target == null || NCombatRoom.Instance == null)
        {
            return;
        }

        NCreature? creatureNode = NCombatRoom.Instance.GetCreatureNode(target);
        if (creatureNode == null)
        {
            return;
        }

        NSuiLieWuShengVfx vfx = VFXUtil.GenVFXNode<NSuiLieWuShengVfx>(ScenePath);
        Vector2 targetSize = creatureNode.Visuals.Bounds.Size;
        float crackDiameter = Mathf.Clamp(Mathf.Max(targetSize.X, targetSize.Y) * 1.35f, 260f, 460f);
        vfx._targetScale = crackDiameter / SourceTextureSize * 0.5f;
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;

        // The strike lands as the crack reaches its full size.
        await Cmd.Wait(CrackOpenDuration);
    }

    public override void _Ready()
    {
        SetOpacity(0f);
        Scale = Vector2.One * (_targetScale * 0.68f);
        Rotation = Mathf.DegToRad(-4f);
        TaskHelper.RunSafely(PlaySequence());
    }

    private async Task PlaySequence()
    {
        Tween expansion = CreateTween().SetParallel();
        expansion.TweenProperty(this, "scale", Vector2.One * (_targetScale * 1.04f), CrackOpenDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        expansion.TweenProperty(this, "rotation", Mathf.DegToRad(1f), CrackOpenDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        expansion.TweenMethod(Callable.From<float>(SetOpacity), 0f, 1f, CrackOpenDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(expansion, Tween.SignalName.Finished);

        await Cmd.Wait(CrackHoldDuration);

        Tween fade = CreateTween().SetParallel();
        fade.TweenProperty(this, "scale", Vector2.One * (_targetScale * 1.10f), CrackFadeDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        fade.TweenMethod(Callable.From<float>(SetOpacity), 1f, 0f, CrackFadeDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        await ToSignal(fade, Tween.SignalName.Finished);

        this.QueueFreeSafely();
    }

    private void SetOpacity(float opacity)
    {
        if (_glowCracks?.Material is ShaderMaterial glowMaterial)
        {
            glowMaterial.SetShaderParameter("opacity", opacity);
        }

        if (_coreCracks?.Material is ShaderMaterial coreMaterial)
        {
            coreMaterial.SetShaderParameter("opacity", opacity);
        }
    }
}
