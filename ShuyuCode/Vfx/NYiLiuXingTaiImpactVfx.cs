using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A pale-blue liquid impact made from uneven splashes and droplets.
/// Kept separate from the owner-centered wave so both effects can be tuned independently.
/// </summary>
public partial class NYiLiuXingTaiImpactVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.PowerVfxPath}/vfx_YiLiuXingTaiPowerImpact.tscn";

    private const float Duration = 0.42f;
    private const int LobeCount = 11;
    private const int DropletCount = 16;

    private readonly List<SplashLobe> _lobes = [];
    private readonly List<SplashDroplet> _droplets = [];
    private float _impactRadius = 72f;
    private float _progress;
    private Vector2 _flowDirection = Vector2.Right;

    private readonly record struct SplashLobe(
        Vector2 Direction,
        float Reach,
        float Width,
        float Bend,
        float Delay,
        float Opacity);

    private readonly record struct SplashDroplet(
        Vector2 Direction,
        float Reach,
        float Radius,
        float Bend,
        float Delay);

    public static void Play(Creature? source, Creature? target)
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
        NCreature? sourceNode = NCombatRoom.Instance.GetCreatureNode(source);
        if (sourceNode != null)
        {
            Vector2 travel = creatureNode.VfxSpawnPosition - sourceNode.VfxSpawnPosition;
            if (travel.LengthSquared() > 0.001f)
            {
                vfx._flowDirection = travel.Normalized();
            }
        }
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;
    }

    public override void _Ready()
    {
        CreateSplashShapes();
        SetProgress(0f);
        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _Draw()
    {
        float eased = 1f - Mathf.Pow(1f - _progress, 3f);
        float coreFade = 1f - Mathf.SmoothStep(0.18f, 0.82f, _progress);
        float bloomRadius = Mathf.Lerp(_impactRadius * 0.12f, _impactRadius * 0.72f, eased);
        DrawIrregularBloom(
            bloomRadius * 1.28f,
            new Color(0.18f, 0.63f, 1f, 0.09f * coreFade),
            1.7f);
        DrawIrregularBloom(
            bloomRadius,
            new Color(0.48f, 0.86f, 1f, 0.28f * coreFade),
            7.3f);

        foreach (SplashLobe lobe in _lobes)
        {
            float localProgress = Mathf.Clamp(
                (_progress - lobe.Delay) / (1f - lobe.Delay),
                0f,
                1f);
            if (localProgress <= 0f)
            {
                continue;
            }

            float motion = 1f - Mathf.Pow(1f - localProgress, 2.6f);
            float fade = 1f - Mathf.SmoothStep(0.42f, 1f, localProgress);
            Vector2 side = lobe.Direction.Orthogonal();
            Vector2 curvedDirection =
                (lobe.Direction + side * lobe.Bend * motion).Normalized();
            Vector2 root = lobe.Direction * _impactRadius * 0.05f;
            Vector2 tip =
                curvedDirection * _impactRadius * lobe.Reach * motion;
            float halfWidth =
                _impactRadius * lobe.Width *
                Mathf.Lerp(1f, 0.34f, localProgress);
            Vector2[] splashPoints =
            [
                root - lobe.Direction * _impactRadius * 0.08f,
                root + side * halfWidth,
                tip,
                root - side * halfWidth * 0.72f
            ];
            DrawColoredPolygon(
                splashPoints,
                new Color(0.40f, 0.82f, 1f, lobe.Opacity * fade));

            Vector2 innerTip = root.Lerp(tip, 0.86f);
            DrawNeedle(
                root,
                innerTip,
                Mathf.Max(1f, halfWidth * 0.22f),
                new Color(0.75f, 0.96f, 1f, 0.34f * fade));
        }

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
            Vector2 curvedDirection =
                (droplet.Direction +
                 droplet.Direction.Orthogonal() * droplet.Bend * localProgress)
                .Normalized();
            Vector2 tip = curvedDirection * distance;
            float radius = droplet.Radius * Mathf.Lerp(1f, 0.55f, localProgress);
            float trailLength = _impactRadius * Mathf.Lerp(0.08f, 0.24f, localProgress);
            Color glow = new(0.24f, 0.71f, 1f, 0.15f * dropletFade);
            Color splash = new(0.57f, 0.9f, 1f, 0.72f * dropletFade);

            DrawNeedle(
                tip - curvedDirection * trailLength,
                tip,
                radius * 2.8f,
                glow);
            DrawNeedle(
                tip - curvedDirection * trailLength * 0.75f,
                tip,
                radius * 1.15f,
                splash);
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

    private void CreateSplashShapes()
    {
        float flowAngle = _flowDirection.Angle();

        for (int i = 0; i < LobeCount; i++)
        {
            float noiseA = Hash(i, 2.13f);
            float noiseB = Hash(i, 9.47f);
            float noiseC = Hash(i, 17.81f);
            bool forwardLobe = i < 7;
            float angle = forwardLobe
                ? flowAngle + Mathf.Lerp(-1.18f, 1.18f, noiseA)
                : Mathf.Tau * noiseA;
            _lobes.Add(new SplashLobe(
                Vector2.FromAngle(angle),
                Mathf.Lerp(forwardLobe ? 0.86f : 0.62f, forwardLobe ? 1.64f : 1.16f, noiseB),
                Mathf.Lerp(0.08f, 0.19f, noiseC),
                Mathf.Lerp(-0.34f, 0.34f, noiseB),
                Mathf.Lerp(0f, 0.11f, noiseC),
                Mathf.Lerp(0.12f, 0.26f, noiseA)));
        }

        for (int i = 0; i < DropletCount; i++)
        {
            float noiseA = Hash(i, 31.37f);
            float noiseB = Hash(i, 47.11f);
            float noiseC = Hash(i, 63.79f);
            float angle = Mathf.Tau * noiseA;
            float reach = Mathf.Lerp(0.86f, 1.72f, noiseB);
            float radius = Mathf.Lerp(2.8f, 6.2f, noiseA);
            float delay = Mathf.Lerp(0.02f, 0.13f, noiseC);
            _droplets.Add(new SplashDroplet(
                Vector2.FromAngle(angle),
                reach,
                radius,
                Mathf.Lerp(-0.28f, 0.28f, noiseB),
                delay));
        }
    }

    private void DrawIrregularBloom(float radius, Color color, float salt)
    {
        const int pointCount = 13;
        Vector2[] points = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float angle =
                Mathf.Tau * i / pointCount +
                Mathf.Lerp(-0.12f, 0.12f, Hash(i, salt + 2.9f));
            float pointRadius =
                radius * Mathf.Lerp(0.64f, 1.08f, Hash(i, salt));
            points[i] = Vector2.FromAngle(angle) * pointRadius;
        }

        DrawColoredPolygon(points, color);
    }

    private void DrawNeedle(
        Vector2 start,
        Vector2 end,
        float width,
        Color color)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= 0.001f || width <= 0f)
        {
            return;
        }

        Vector2 direction = delta / length;
        Vector2 normal = direction.Orthogonal();
        float halfWidth = width * 0.5f;
        float taperLength = Mathf.Min(
            length * 0.26f,
            Mathf.Max(halfWidth * 1.7f, 1f));
        Vector2 tailShoulder = start + direction * taperLength;
        Vector2 tipShoulder = end - direction * taperLength;
        Vector2[] points =
        [
            start,
            tailShoulder + normal * halfWidth * 0.72f,
            tipShoulder + normal * halfWidth,
            end,
            tipShoulder - normal * halfWidth,
            tailShoulder - normal * halfWidth * 0.72f
        ];
        DrawColoredPolygon(points, color);
    }

    private static float Hash(int index, float salt)
    {
        float value = Mathf.Sin((index + 1) * 12.9898f + salt * 78.233f) * 43758.547f;
        return value - Mathf.Floor(value);
    }
}
