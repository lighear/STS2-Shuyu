using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A pale-blue circular hit flash with a radial liquid-like splash.
/// Kept separate from the owner-centered wave so both effects can be tuned independently.
/// </summary>
public partial class NYiLiuXingTaiImpactVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.PowerVfxPath}/vfx_YiLiuXingTaiPowerImpact.tscn";

    private const float Duration = 0.38f;
    private const int DropletCount = 18;

    private readonly List<SplashDroplet> _droplets = [];
    private float _impactRadius = 72f;
    private float _progress;

    private readonly record struct SplashDroplet(Vector2 Direction, float Reach, float Radius, float Delay);

    public static void Play(Creature? target)
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

        NYiLiuXingTaiImpactVfx vfx = VFXUtil.GenVFXNode<NYiLiuXingTaiImpactVfx>(ScenePath);
        Vector2 targetSize = creatureNode.Visuals.Bounds.Size;
        vfx._impactRadius = Mathf.Clamp(Mathf.Max(targetSize.X, targetSize.Y) * 0.3f, 58f, 96f);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;
    }

    public override void _Ready()
    {
        CreateDroplets();
        SetProgress(0f);
        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _Draw()
    {
        float eased = 1f - Mathf.Pow(1f - _progress, 3f);
        float coreFade = 1f - Mathf.SmoothStep(0.22f, 1f, _progress);
        float ringFade = 1f - Mathf.SmoothStep(0.32f, 1f, _progress);

        float discRadius = Mathf.Lerp(_impactRadius * 0.16f, _impactRadius, eased);
        DrawCircle(Vector2.Zero, discRadius * 1.32f, new Color(0.22f, 0.66f, 1f, 0.07f * coreFade));
        DrawCircle(Vector2.Zero, discRadius * 1.12f, new Color(0.35f, 0.78f, 1f, 0.12f * coreFade));
        DrawCircle(Vector2.Zero, discRadius, new Color(0.58f, 0.9f, 1f, 0.3f * coreFade));

        float ringRadius = Mathf.Lerp(_impactRadius * 0.28f, _impactRadius * 1.34f, eased);
        float ringWidth = Mathf.Lerp(6f, 1.5f, eased);
        DrawArc(Vector2.Zero, ringRadius, 0f, Mathf.Tau, 56,
            new Color(0.62f, 0.93f, 1f, 0.82f * ringFade), ringWidth, true);

        foreach (SplashDroplet droplet in _droplets)
        {
            float localProgress = Mathf.Clamp((_progress - droplet.Delay) / (1f - droplet.Delay), 0f, 1f);
            if (localProgress <= 0f)
            {
                continue;
            }

            float dropletEase = 1f - Mathf.Pow(1f - localProgress, 2.4f);
            float dropletFade = 1f - Mathf.SmoothStep(0.48f, 1f, localProgress);
            float distance = _impactRadius * Mathf.Lerp(0.18f, droplet.Reach, dropletEase);
            Vector2 tip = droplet.Direction * distance;
            float radius = droplet.Radius * Mathf.Lerp(1f, 0.55f, localProgress);
            float trailLength = _impactRadius * Mathf.Lerp(0.08f, 0.24f, localProgress);
            Color glow = new(0.24f, 0.71f, 1f, 0.15f * dropletFade);
            Color splash = new(0.57f, 0.9f, 1f, 0.72f * dropletFade);

            DrawLine(tip - droplet.Direction * trailLength, tip, glow, radius * 2.8f, true);
            DrawLine(tip - droplet.Direction * trailLength * 0.75f, tip, splash, radius * 1.15f, true);
            DrawCircle(tip, radius * 1.75f, glow);
            DrawCircle(tip, radius, splash);
        }
    }

    private async Task PlaySequence()
    {
        Tween impact = CreateTween();
        impact.TweenMethod(Callable.From<float>(SetProgress), 0f, 1f, Duration)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(impact, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void SetProgress(float progress)
    {
        _progress = progress;
        QueueRedraw();
    }

    private void CreateDroplets()
    {
        for (int i = 0; i < DropletCount; i++)
        {
            float noiseA = Fraction(Mathf.Sin((i + 1) * 12.9898f) * 43758.547f);
            float noiseB = Fraction(Mathf.Sin((i + 1) * 78.233f) * 23421.633f);
            float angle = Mathf.Tau * i / DropletCount + Mathf.Lerp(-0.13f, 0.13f, noiseA);
            float reach = Mathf.Lerp(0.86f, 1.72f, noiseB);
            float radius = Mathf.Lerp(2.8f, 6.2f, noiseA);
            float delay = Mathf.Lerp(0.02f, 0.13f, noiseB);
            _droplets.Add(new SplashDroplet(Vector2.FromAngle(angle), reach, radius, delay));
        }
    }

    private static float Fraction(float value)
    {
        return value - Mathf.Floor(value);
    }
}
