using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// Black fog contracts around the character, briefly forms a shield-shaped
/// shell, then dissolves.
/// </summary>
public partial class NJinShuJieJieShieldVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_JinShuJieJieShield.tscn";

    private const float Duration = 1.50f;
    private const float ResolveDelay = 0.45f;
    private const float FogCanvasWidthFactor = 1.45f;
    private const float FogCanvasHeightFactor = 1.18f;

    [Export]
    private ColorRect? _fogShield;

    [Export]
    private Sprite2D? _solidShield;

    private Vector2 _effectSize = new(520f, 560f);
    private float _solidShieldBaseScale = 1f;

    public static async Task PlayOpening(Creature? owner)
    {
        if (TestMode.IsOn || owner == null || NCombatRoom.Instance == null)
        {
            return;
        }

        NCreature? creatureNode = NCombatRoom.Instance.GetCreatureNode(owner);
        if (creatureNode == null)
        {
            return;
        }

        NPowerVfxLayout.Resolve(
            owner,
            creatureNode,
            out Vector2 visualSize,
            out Vector2 shieldCenter);

        Control creatureBounds = creatureNode.Visuals.Bounds;
        NJinShuJieJieShieldVfx vfx = VFXUtil.GenVFXNode<NJinShuJieJieShieldVfx>(ScenePath);
        vfx._effectSize = new Vector2(
            Mathf.Clamp(visualSize.X * FogCanvasWidthFactor, 420f, 880f),
            Mathf.Clamp(visualSize.Y * FogCanvasHeightFactor, 520f, 920f));
        creatureBounds.AddChildSafely(vfx);
        vfx.Position = shieldCenter;

        await Cmd.Wait(ResolveDelay);
    }

    public override void _Ready()
    {
        if (_fogShield == null)
        {
            this.QueueFreeSafely();
            return;
        }

        _fogShield.Position = _effectSize * -0.5f;
        _fogShield.Size = _effectSize;

        if (_solidShield?.Texture != null)
        {
            _solidShieldBaseScale = _effectSize.Y * 0.52f / _solidShield.Texture.GetSize().Y;
        }

        SetProgress(0f);
        TaskHelper.RunSafely(PlaySequence());
    }

    private async Task PlaySequence()
    {
        Tween shield = CreateTween();
        shield.TweenMethod(Callable.From<float>(SetProgress), 0f, 1f, Duration)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(shield, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void SetProgress(float progress)
    {
        if (_fogShield?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("progress", progress);
        }

        if (_solidShield != null)
        {
            float formation = Mathf.SmoothStep(0.38f, 0.60f, progress);
            float fadeOut = 1f - Mathf.SmoothStep(0.74f, 1f, progress);
            float alpha = formation * fadeOut;
            _solidShield.Modulate = new Color(0.055f, 0.038f, 0.075f, 0.62f * alpha);
            float settleScale = Mathf.Lerp(1.12f, 1f, formation);
            _solidShield.Scale = Vector2.One * _solidShieldBaseScale * settleScale;
        }
    }
}
