using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A softly glowing continuous-spectrum rainbow placed behind the enemy side.
/// Its lifetime is controlled by Hong Ni so it remains until every power finishes resolving.
/// </summary>
public partial class NHongNiVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_HongNi.tscn";

    private const int SpectrumBands = 64;
    private const int ArcSegments = 128;
    private const float BandThickness = 74f;
    private const float VisibleAlpha = 0.215f;
    private const float SpectrumSaturation = 0.52f;
    private const float EdgeFadeRatio = 0.15f;
    private const float FadeInDuration = 0.24f;
    private const float FadeOutDuration = 0.30f;

    private float _arcRadius = 360f;
    private float _time;
    private bool _isFinishing;
    private Tween? _fadeInTween;

    public static NHongNiVfx? Create(ICombatState combatState)
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null)
        {
            return null;
        }

        Vector2? sideCenter = VfxCmd.GetSideCenter(CombatSide.Enemy, combatState);
        Vector2? sideFloor = VfxCmd.GetSideCenterFloor(CombatSide.Enemy, combatState);
        if (!sideCenter.HasValue || !sideFloor.HasValue)
        {
            return null;
        }

        Vector2 viewportSize = NCombatRoom.Instance.GetViewportRect().Size;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;

        foreach (Creature enemy in combatState.GetCreaturesOnSide(CombatSide.Enemy).Where(enemy => enemy.IsHittable))
        {
            NCreature? creatureNode = NCombatRoom.Instance.GetCreatureNode(enemy);
            if (creatureNode == null)
            {
                continue;
            }

            float halfWidth = creatureNode.Visuals.Bounds.Size.X * 0.5f;
            minX = Mathf.Min(minX, creatureNode.VfxSpawnPosition.X - halfWidth);
            maxX = Mathf.Max(maxX, creatureNode.VfxSpawnPosition.X + halfWidth);
        }

        float occupiedWidth = float.IsFinite(minX) && float.IsFinite(maxX)
            ? maxX - minX
            : viewportSize.X * 0.30f;

        NHongNiVfx vfx = VFXUtil.GenVFXNode<NHongNiVfx>(ScenePath);
        vfx._arcRadius = Mathf.Clamp(
            Mathf.Max(occupiedWidth * 0.58f + 100f, viewportSize.X * 0.16f),
            250f,
            viewportSize.X * 0.28f);

        NCombatRoom.Instance.BackCombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = new Vector2(sideCenter.Value.X, sideFloor.Value.Y + 8f);
        return vfx;
    }

    public override void _Ready()
    {
        Modulate = new Color(1f, 1f, 1f, 0f);
        _fadeInTween = CreateTween();
        _fadeInTween.TweenProperty(this, "modulate:a", VisibleAlpha, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = 0.96f + Mathf.Sin(_time * 2.4f) * 0.04f;
        float innerRadius = _arcRadius - BandThickness * 0.5f;
        float outerRadius = _arcRadius + BandThickness * 0.5f;

        DrawEdgeGlow(innerRadius, hue: 0.76f, inward: true, pulse);
        DrawEdgeGlow(outerRadius, hue: 0.0f, inward: false, pulse);

        for (int i = 0; i < SpectrumBands; i++)
        {
            float ratio = i / (float)(SpectrumBands - 1);
            float radius = Mathf.Lerp(innerRadius, outerRadius, ratio);
            float hue = Mathf.Lerp(0.76f, 0f, ratio);
            float edgeDistance = Mathf.Min(ratio, 1f - ratio);
            float edgeFade = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp(edgeDistance / EdgeFadeRatio, 0f, 1f));
            Color color = Color.FromHsv(hue, SpectrumSaturation, 0.96f,
                0.72f * pulse * edgeFade);
            DrawArc(Vector2.Zero, radius, Mathf.Pi, Mathf.Tau, ArcSegments, color, 2.15f, true);
        }

        DrawArc(Vector2.Zero, _arcRadius, Mathf.Pi, Mathf.Tau, ArcSegments,
            new Color(1f, 1f, 1f, 0.075f * pulse), BandThickness * 0.72f, true);
    }

    public async Task FinishAsync()
    {
        if (_isFinishing || !GodotObject.IsInstanceValid(this))
        {
            return;
        }

        _isFinishing = true;
        _fadeInTween?.Kill();

        Tween fadeOut = CreateTween();
        fadeOut.TweenProperty(this, "modulate:a", 0f, FadeOutDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(fadeOut, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void DrawEdgeGlow(float edgeRadius, float hue, bool inward, float pulse)
    {
        const int glowLayers = 10;
        for (int i = glowLayers; i >= 1; i--)
        {
            float ratio = i / (float)glowLayers;
            float offset = ratio * 24f * (inward ? -1f : 1f);
            float alpha = 0.012f + (1f - ratio) * 0.022f;
            Color glowColor = Color.FromHsv(hue, 0.46f, 0.96f, alpha * pulse);
            DrawArc(Vector2.Zero, edgeRadius + offset, Mathf.Pi, Mathf.Tau,
                ArcSegments, glowColor, 7f, true);
        }
    }
}
