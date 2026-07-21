using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A short, simultaneous ice-cannon volley: muzzle flash, high-speed tracers,
/// and a separate icy impact burst on every living target.
/// </summary>
public partial class NBingChaDaPaoVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_BingChaDaPao.tscn";

    private const float ImpactResolveDelay = 0.13f;
    private const float VisualDuration = 0.38f;
    private const float AudioTailDuration = 0.24f;
    private const float TravelEnd = 0.34f;
    private const int PelletsPerTarget = 3;
    private const int ImpactShardCount = 7;

    [Export]
    private Godot.Collections.Array<AudioStreamPlayer> _gunshots = [];

    private readonly List<Vector2> _targetOffsets = [];
    private float _progress;

    public static async Task PlayVolley(Creature? owner, IEnumerable<Creature> targets)
    {
        if (TestMode.IsOn || owner == null || NCombatRoom.Instance == null)
        {
            return;
        }

        NCreature? ownerNode = NCombatRoom.Instance.GetCreatureNode(owner);
        if (ownerNode == null)
        {
            return;
        }

        List<Vector2> targetPositions = [];
        foreach (Creature target in targets)
        {
            NCreature? targetNode = NCombatRoom.Instance.GetCreatureNode(target);
            if (target.IsAlive && targetNode != null)
            {
                targetPositions.Add(targetNode.VfxSpawnPosition);
            }
        }

        if (targetPositions.Count == 0)
        {
            return;
        }

        Vector2 ownerSize = ownerNode.Visuals.Bounds.Size;
        Vector2 muzzlePosition = ownerNode.VfxSpawnPosition + new Vector2(
            Mathf.Clamp(ownerSize.X * 0.22f, 58f, 112f),
            -Mathf.Clamp(ownerSize.Y * 0.035f, 8f, 24f));

        NBingChaDaPaoVfx vfx = VFXUtil.GenVFXNode<NBingChaDaPaoVfx>(ScenePath);
        foreach (Vector2 targetPosition in targetPositions)
        {
            vfx._targetOffsets.Add(targetPosition - muzzlePosition);
        }

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = muzzlePosition;

        // Damage lands as the tracer heads reach the enemies, while the ice
        // splinters and the gunshot tail continue without holding up combat.
        await Cmd.Wait(ImpactResolveDelay);
    }

    public override void _Ready()
    {
        SetProgress(0f);
        TaskHelper.RunSafely(PlayGunshotBurst());
        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _Draw()
    {
        DrawMuzzleFlash();

        for (int targetIndex = 0; targetIndex < _targetOffsets.Count; targetIndex++)
        {
            for (int pelletIndex = 0; pelletIndex < PelletsPerTarget; pelletIndex++)
            {
                Vector2 pelletTarget = GetPelletTarget(_targetOffsets[targetIndex], targetIndex, pelletIndex);
                DrawTracer(pelletTarget);
                DrawImpact(pelletTarget, targetIndex, pelletIndex);
            }
        }
    }

    private void DrawMuzzleFlash()
    {
        float flashProgress = Mathf.Clamp(_progress / 0.30f, 0f, 1f);
        float flashFade = 1f - Mathf.SmoothStep(0.12f, 1f, flashProgress);
        if (flashFade <= 0f)
        {
            return;
        }

        Vector2 fireDirection = Vector2.Right;
        if (_targetOffsets.Count > 0)
        {
            Vector2 averageTarget = Vector2.Zero;
            foreach (Vector2 targetOffset in _targetOffsets)
            {
                averageTarget += targetOffset.Normalized();
            }

            if (averageTarget.LengthSquared() > 0.001f)
            {
                fireDirection = averageTarget.Normalized();
            }
        }

        float expansion = 1f - Mathf.Pow(1f - flashProgress, 3f);
        DrawCircle(Vector2.Zero, Mathf.Lerp(18f, 52f, expansion),
            new Color(0.20f, 0.67f, 1f, 0.16f * flashFade));
        DrawCircle(Vector2.Zero, Mathf.Lerp(8f, 25f, expansion),
            new Color(0.69f, 0.94f, 1f, 0.78f * flashFade));
        DrawCircle(Vector2.Zero, Mathf.Lerp(4f, 12f, expansion),
            new Color(1f, 1f, 1f, 0.96f * flashFade));

        for (int i = 0; i < 7; i++)
        {
            float spread = Mathf.Lerp(-0.58f, 0.58f, i / 6f);
            Vector2 rayDirection = fireDirection.Rotated(spread);
            float rayLength = Mathf.Lerp(72f, 42f, Mathf.Abs(spread) / 0.58f) * expansion;
            DrawLine(rayDirection * 5f, rayDirection * rayLength,
                new Color(0.58f, 0.90f, 1f, 0.72f * flashFade),
                Mathf.Lerp(5.5f, 2.4f, Mathf.Abs(spread) / 0.58f), true);
        }
    }

    private void DrawTracer(Vector2 targetOffset)
    {
        float travelProgress = Mathf.Clamp(_progress / TravelEnd, 0f, 1f);
        if (travelProgress <= 0f || travelProgress >= 1f)
        {
            return;
        }

        float headProgress = 1f - Mathf.Pow(1f - travelProgress, 2.2f);
        float tailProgress = Mathf.Max(0f, headProgress - 0.20f);
        Vector2 head = targetOffset * headProgress;
        Vector2 tail = targetOffset * tailProgress;
        float fade = 1f - Mathf.SmoothStep(0.76f, 1f, travelProgress);

        DrawLine(tail, head, new Color(0.18f, 0.64f, 1f, 0.17f * fade), 12f, true);
        DrawLine(tail, head, new Color(0.48f, 0.87f, 1f, 0.66f * fade), 5f, true);
        DrawLine(tail, head, new Color(0.94f, 0.99f, 1f, 0.92f * fade), 2.1f, true);
        DrawCircle(head, 6f, new Color(0.72f, 0.95f, 1f, 0.68f * fade));
    }

    private void DrawImpact(Vector2 targetOffset, int targetIndex, int pelletIndex)
    {
        float impactProgress = Mathf.Clamp((_progress - TravelEnd) / (1f - TravelEnd), 0f, 1f);
        if (impactProgress <= 0f)
        {
            return;
        }

        float eased = 1f - Mathf.Pow(1f - impactProgress, 2.7f);
        float fade = 1f - Mathf.SmoothStep(0.38f, 1f, impactProgress);
        float flashFade = 1f - Mathf.SmoothStep(0.0f, 0.52f, impactProgress);
        float ringRadius = Mathf.Lerp(9f, 58f, eased);

        DrawCircle(targetOffset, Mathf.Lerp(16f, 38f, eased),
            new Color(0.43f, 0.84f, 1f, 0.18f * flashFade));
        DrawCircle(targetOffset, Mathf.Lerp(7f, 18f, eased),
            new Color(0.88f, 0.98f, 1f, 0.68f * flashFade));
        DrawArc(targetOffset, ringRadius, 0f, Mathf.Tau, 40,
            new Color(0.56f, 0.90f, 1f, 0.64f * fade), Mathf.Lerp(3.6f, 1f, eased), true);

        for (int i = 0; i < ImpactShardCount; i++)
        {
            float noise = Fraction(Mathf.Sin(
                (targetIndex + 1) * 31.73f + (pelletIndex + 1) * 19.19f + (i + 1) * 12.9898f) * 43758.547f);
            float angle = Mathf.Tau * i / ImpactShardCount + Mathf.Lerp(-0.17f, 0.17f, noise);
            Vector2 direction = Vector2.FromAngle(angle);
            float reach = Mathf.Lerp(36f, 78f, noise) * eased;
            Vector2 tip = targetOffset + direction * reach;
            float shardLength = Mathf.Lerp(9f, 21f, 1f - noise);

            DrawLine(tip - direction * shardLength, tip,
                new Color(0.31f, 0.76f, 1f, 0.15f * fade), 6f, true);
            DrawLine(tip - direction * shardLength * 0.82f, tip,
                new Color(0.76f, 0.95f, 1f, 0.76f * fade), Mathf.Lerp(1.1f, 2.5f, noise), true);
        }
    }

    private async Task PlayGunshotBurst()
    {
        if (_gunshots.Count == 0)
        {
            return;
        }

        _gunshots[0].Play();
        if (_gunshots.Count > 1)
        {
            await Cmd.Wait(0.028f);
            _gunshots[1].Play();
        }

        if (_gunshots.Count > 2)
        {
            await Cmd.Wait(0.030f);
            _gunshots[2].Play();
        }
    }

    private async Task PlaySequence()
    {
        Tween visual = CreateTween();
        visual.TweenMethod(Callable.From<float>(SetProgress), 0f, 1f, VisualDuration)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(visual, Tween.SignalName.Finished);

        Visible = false;
        await Cmd.Wait(AudioTailDuration);
        this.QueueFreeSafely();
    }

    private void SetProgress(float progress)
    {
        _progress = progress;
        QueueRedraw();
    }

    private static float Fraction(float value)
    {
        return value - Mathf.Floor(value);
    }

    private static Vector2 GetPelletTarget(Vector2 targetOffset, int targetIndex, int pelletIndex)
    {
        if (pelletIndex == 0 || targetOffset.LengthSquared() < 0.001f)
        {
            return targetOffset;
        }

        Vector2 direction = targetOffset.Normalized();
        Vector2 perpendicular = new(-direction.Y, direction.X);
        float side = pelletIndex == 1 ? -1f : 1f;
        float lateralNoise = Fraction(Mathf.Sin((targetIndex + 1) * 17.17f + pelletIndex * 41.37f) * 15431.743f);
        float depthNoise = Fraction(Mathf.Sin((targetIndex + 1) * 53.11f + pelletIndex * 9.73f) * 28741.311f);
        float lateralOffset = side * Mathf.Lerp(18f, 28f, lateralNoise);
        float depthOffset = Mathf.Lerp(-6f, 7f, depthNoise);
        return targetOffset + perpendicular * lateralOffset + direction * depthOffset;
    }
}
