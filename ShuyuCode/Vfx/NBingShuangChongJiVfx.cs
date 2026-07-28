using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// Throws one of four common ice-crystal sprites in a straight line, with a
/// launch flash, luminous translucent trail, and crystalline impact burst.
/// </summary>
public partial class NBingShuangChongJiVfx : Node2D
{
    public static readonly string ScenePath =
        $"{VFXUtil.CardVfxPath}/vfx_BingShuangChongJi.tscn";

    private const float LaunchDelay = 0.08f;
    private const float TravelDuration = 0.20f;
    private const float ImpactTime = LaunchDelay + TravelDuration;
    private const float VisualDuration = 0.64f;
    private const float ProjectileDisplayWidth = 112f;
    private const float EffectScale = 1.5f;
    private const int ImpactShardCount = 11;
    private const int ImpactSpeckCount = 14;

    [Export]
    private Sprite2D? _projectile;

    [Export]
    private Array<Texture2D> _crystalTextures = [];

    [Export]
    private bool _useArcTrajectory;

    [Export]
    private Vector2 _arcControlPoint;

    [Export]
    private Vector2 _targetOffset = Vector2.Right * 600f;
    private float _time;
    private float _projectileBaseScale = 1f;
    private float _projectileVisualHeight = 64f;
    private float _sizeMultiplier = 1f;
    private float _impactSizeMultiplier = 1f;
    private bool _impactTriggered;
    private readonly List<ImpactShard> _impactShards = [];
    private readonly List<ImpactSpeck> _impactSpecks = [];

    public static Task PlayProjectile(Creature? owner, Creature? target)
    {
        if (TestMode.IsOn || owner == null || target == null || NCombatRoom.Instance == null)
        {
            return Task.CompletedTask;
        }

        NCreature? ownerNode = NCombatRoom.Instance.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance.GetCreatureNode(target);
        if (ownerNode == null || targetNode == null)
        {
            return Task.CompletedTask;
        }

        Vector2 sourcePosition = ownerNode.VfxSpawnPosition;
        Vector2 targetPosition = targetNode.VfxSpawnPosition;

        NBingShuangChongJiVfx vfx =
            VFXUtil.GenVFXNode<NBingShuangChongJiVfx>(ScenePath);
        vfx._targetOffset = targetPosition - sourcePosition;
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = sourcePosition;

        // The visual continues independently so the card can immediately
        // discard frozen cards and start their projectiles.
        return Task.CompletedTask;
    }

    public static void SpawnFrozenCardProjectiles(
        Creature? owner,
        IReadOnlyList<Creature> targets,
        int effectiveEnergyCost)
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

        Vector2 sourcePosition = ownerNode.VfxSpawnPosition;
        foreach (Creature target in targets)
        {
            NCreature? targetNode = NCombatRoom.Instance.GetCreatureNode(target);
            if (!target.IsAlive || targetNode == null)
            {
                continue;
            }

            Vector2 targetOffset = targetNode.VfxSpawnPosition - sourcePosition;
            float distance = targetOffset.Length();
            float upperArc = -Mathf.Clamp(distance * 0.28f, 180f, 320f);
            float lowerArc = Mathf.Clamp(distance * 0.20f, 130f, 240f);
            float controlProgress = Rng.Chaotic.NextFloat(0.42f, 0.58f);

            NBingShuangChongJiVfx vfx =
                VFXUtil.GenVFXNode<NBingShuangChongJiVfx>(ScenePath);
            vfx._targetOffset = targetOffset;
            vfx._useArcTrajectory = true;
            vfx._sizeMultiplier = GetFrozenCardSizeMultiplier(effectiveEnergyCost);
            vfx._impactSizeMultiplier =
                GetFrozenCardImpactSizeMultiplier(effectiveEnergyCost);
            vfx._arcControlPoint =
                targetOffset * controlProgress +
                Vector2.Down * Rng.Chaotic.NextFloat(upperArc, lowerArc);
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
            vfx.GlobalPosition = sourcePosition;
        }
    }

    private static float GetFrozenCardSizeMultiplier(int effectiveEnergyCost)
    {
        float costProgress =
            Mathf.Clamp(Math.Max(effectiveEnergyCost, 0) / 4f, 0f, 1f);
        return Mathf.Lerp(
            2f / 3f,
            1f,
            costProgress);
    }

    private static float GetFrozenCardImpactSizeMultiplier(
        int effectiveEnergyCost)
    {
        return Mathf.Clamp(
            2f / 3f + Math.Max(effectiveEnergyCost, 0) / 3f,
            2f / 3f,
            2f);
    }

    public override void _Ready()
    {
        ConfigureRandomCrystal();
        ConfigureImpactParticles();
        SetTime(0f);
        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _Draw()
    {
        DrawDeparture();
        DrawTrail();
        DrawImpact();
    }

    private void ConfigureRandomCrystal()
    {
        if (_projectile == null || _crystalTextures.Count == 0)
        {
            return;
        }

        Texture2D texture =
            _crystalTextures[Rng.Chaotic.NextInt(_crystalTextures.Count)];
        _projectile.Texture = texture;
        _projectile.FlipH = true;
        _projectile.Rotation = GetPathTangent(0f).Angle();
        _projectileBaseScale =
            ProjectileDisplayWidth * EffectScale * _sizeMultiplier /
            texture.GetWidth();
        _projectileVisualHeight =
            texture.GetHeight() * _projectileBaseScale;
        _projectile.Scale = Vector2.One * _projectileBaseScale;
    }

    private void ConfigureImpactParticles()
    {
        Vector2 incoming = GetPathTangent(1f).Normalized();
        float incomingAngle = incoming.Angle();

        _impactShards.Clear();
        for (int i = 0; i < ImpactShardCount; i++)
        {
            bool backScatter = i >= 8;
            float angle = backScatter
                ? incomingAngle + Mathf.Pi +
                  Rng.Chaotic.NextFloat(-0.82f, 0.82f)
                : incomingAngle +
                  Rng.Chaotic.NextFloat(-1.48f, 1.48f);
            _impactShards.Add(new ImpactShard(
                Vector2.FromAngle(angle),
                Rng.Chaotic.NextFloat(54f, backScatter ? 105f : 175f),
                Rng.Chaotic.NextFloat(8f, 20f),
                Rng.Chaotic.NextFloat(2.5f, 7f),
                Rng.Chaotic.NextFloat(0f, 0.075f),
                Rng.Chaotic.NextFloat(-3.8f, 3.8f),
                Rng.Chaotic.NextFloat(20f, 68f),
                Rng.Chaotic.NextFloat(0.68f, 1f)));
        }

        _impactSpecks.Clear();
        for (int i = 0; i < ImpactSpeckCount; i++)
        {
            float angle =
                incomingAngle + Rng.Chaotic.NextFloat(-1.75f, 1.75f);
            _impactSpecks.Add(new ImpactSpeck(
                Vector2.FromAngle(angle),
                Rng.Chaotic.NextFloat(90f, 235f),
                Rng.Chaotic.NextFloat(5f, 16f),
                Rng.Chaotic.NextFloat(0.6f, 1.8f),
                Rng.Chaotic.NextFloat(0f, 0.055f),
                Rng.Chaotic.NextFloat(0.72f, 1f)));
        }
    }

    private async Task PlaySequence()
    {
        Tween visual = CreateTween();
        visual.TweenMethod(
                Callable.From<float>(SetTime),
                0f,
                VisualDuration,
                VisualDuration)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(visual, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void SetTime(float time)
    {
        _time = time;

        float travelProgress = GetTravelProgress();
        float easedTravel = EaseOutCubic(travelProgress);
        Vector2 projectilePosition = GetPathPosition(easedTravel);

        if (_projectile != null)
        {
            bool isTravelling = time >= LaunchDelay && time < ImpactTime;
            _projectile.Visible = isTravelling;
            _projectile.Position = projectilePosition;
            _projectile.Rotation = GetPathTangent(easedTravel).Angle();
            if (isTravelling)
            {
                float appear = Mathf.Clamp((time - LaunchDelay) / 0.045f, 0f, 1f);
                float arrivalFade =
                    1f - Mathf.SmoothStep(0.82f, 1f, travelProgress);
                _projectile.Modulate =
                    new Color(0.86f, 0.97f, 1f, appear * arrivalFade);
                float pulse = Mathf.Lerp(0.92f, 1.05f, travelProgress);
                _projectile.Scale =
                    Vector2.One * _projectileBaseScale * pulse;
            }
        }

        if (!_impactTriggered && time >= ImpactTime)
        {
            _impactTriggered = true;
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        }

        QueueRedraw();
    }

    private void DrawDeparture()
    {
        float progress = Mathf.Clamp(_time / 0.15f, 0f, 1f);
        if (progress >= 1f)
        {
            return;
        }

        float eased = EaseOutCubic(progress);
        float fade = 1f - Mathf.SmoothStep(0.34f, 1f, progress);
        Vector2 direction = GetPathTangent(0f).Normalized();
        Vector2 normal = direction.Orthogonal();

        DrawCircle(Vector2.Zero, Mathf.Lerp(Scaled(8f), Scaled(34f), eased),
            new Color(0.34f, 0.78f, 1f, 0.18f * fade));
        DrawCircle(Vector2.Zero, Mathf.Lerp(Scaled(4f), Scaled(15f), eased),
            new Color(0.79f, 0.96f, 1f, 0.78f * fade));
        DrawCircle(Vector2.Zero, Mathf.Lerp(Scaled(2f), Scaled(7f), eased),
            new Color(1f, 1f, 1f, 0.96f * fade));

        for (int i = -2; i <= 2; i++)
        {
            Vector2 rayDirection =
                (direction + normal * i * 0.23f).Normalized();
            float length = Mathf.Lerp(
                Scaled(18f),
                Scaled(62f - Mathf.Abs(i) * 8f),
                eased);
            DrawLine(
                rayDirection * Scaled(4f),
                rayDirection * length,
                new Color(0.66f, 0.92f, 1f, 0.64f * fade),
                Mathf.Lerp(Scaled(3.4f), Scaled(1.4f), Mathf.Abs(i) / 2f),
                true);
        }
    }

    private void DrawTrail()
    {
        float travelProgress = GetTravelProgress();
        if (travelProgress <= 0f || travelProgress >= 1f)
        {
            return;
        }

        float easedTravel = EaseOutCubic(travelProgress);
        Vector2 head = GetPathPosition(easedTravel);
        float pathLength = _targetOffset.Length();
        float growth = Mathf.SmoothStep(0f, 0.24f, travelProgress);
        float fade = 1f - Mathf.SmoothStep(0.76f, 1f, travelProgress);
        float lengthScale =
            EffectScale * Mathf.Pow(_sizeMultiplier, 1.35f);
        float tailLength =
            Mathf.Clamp(pathLength * 0.32f, 110f, 240f) *
            growth *
            lengthScale;
        float sizeProgress =
            Mathf.InverseLerp(2f / 3f, 2f, _sizeMultiplier);
        float maxTailSpan = Mathf.Lerp(0.34f, 0.94f, sizeProgress);
        float tailSpan = pathLength > 0.001f
            ? Mathf.Clamp(tailLength / pathLength, 0f, maxTailSpan)
            : 0f;
        float tailProgress = Mathf.Max(0f, easedTravel - tailSpan);
        float headWidth = Mathf.Max(
            _projectileVisualHeight * 0.92f,
            Scaled(32f));

        DrawTrailLayer(
            tailProgress,
            easedTravel,
            new Color(0.12f, 0.54f, 1.12f, 0.035f * fade),
            headWidth * 1.06f);
        DrawTrailLayer(
            Mathf.Lerp(tailProgress, easedTravel, 0.018f),
            easedTravel,
            new Color(0.20f, 0.72f, 1.18f, 0.065f * fade),
            headWidth * 0.88f);
        DrawTrailLayer(
            Mathf.Lerp(tailProgress, easedTravel, 0.035f),
            easedTravel,
            new Color(0.38f, 0.88f, 1.22f, 0.11f * fade),
            headWidth * 0.76f);

        const int BrightLayerCount = 10;
        for (int layer = 0; layer < BrightLayerCount; layer++)
        {
            float layerProgress = layer / (BrightLayerCount - 1f);
            float widthProgress = Mathf.Pow(layerProgress, 0.86f);
            float insetProgress = Mathf.Pow(layerProgress, 0.9f);
            float opacityProgress = Mathf.Pow(layerProgress, 1.2f);
            float widthFactor = Mathf.Lerp(0.70f, 0.055f, widthProgress);
            float startInset = Mathf.Lerp(0.05f, 0.50f, insetProgress);
            Color color = new(
                Mathf.Lerp(0.68f, 1.24f, layerProgress),
                Mathf.Lerp(1.02f, 1.30f, layerProgress),
                Mathf.Lerp(1.22f, 1.42f, layerProgress),
                Mathf.Lerp(0.26f, 0.46f, opacityProgress) * fade);

            DrawTrailLayer(
                Mathf.Lerp(tailProgress, easedTravel, startInset),
                easedTravel,
                color,
                headWidth * widthFactor);
        }
        DrawTrailLayer(
            Mathf.Lerp(tailProgress, easedTravel, 0.54f),
            easedTravel,
            new Color(1.32f, 1.38f, 1.48f, 0.72f * fade),
            headWidth * 0.028f);
        DrawCircle(
            head,
            headWidth * 0.09f,
            new Color(1.12f, 1.28f, 1.42f, 0.68f * fade));
    }

    private void DrawImpact()
    {
        float impactElapsed = _time - ImpactTime;
        if (impactElapsed <= 0f)
        {
            return;
        }

        float impactDuration = VisualDuration - ImpactTime;
        float progress = Mathf.Clamp(impactElapsed / impactDuration, 0f, 1f);
        Vector2 incoming = GetPathTangent(1f).Normalized();

        float flashProgress = Mathf.Clamp(impactElapsed / 0.15f, 0f, 1f);
        float flashExpansion = EaseOutCubic(flashProgress);
        float flashFade =
            1f - Mathf.SmoothStep(0.18f, 1f, flashProgress);

        Vector2 impactNormal = incoming.Orthogonal();
        Vector2[] bloomPoints =
        [
            _targetOffset +
            incoming * ImpactScaled(36f) * flashExpansion,
            _targetOffset +
            impactNormal * ImpactScaled(18f) * flashExpansion,
            _targetOffset -
            incoming * ImpactScaled(14f) * flashExpansion,
            _targetOffset -
            impactNormal * ImpactScaled(13f) * flashExpansion
        ];
        DrawColoredPolygon(
            bloomPoints,
            new Color(0.30f, 0.76f, 1f, 0.17f * flashFade));
        DrawCircle(
            _targetOffset + incoming * ImpactScaled(4f),
            Mathf.Lerp(ImpactScaled(4f), ImpactScaled(10f), flashExpansion),
            new Color(0.91f, 0.99f, 1f, 0.68f * flashFade));

        int flareCount = Math.Min(6, _impactSpecks.Count);
        for (int i = 0; i < flareCount; i++)
        {
            ImpactSpeck flare = _impactSpecks[i];
            Vector2 direction = flare.Direction;
            Vector2 normal = direction.Orthogonal();
            float length =
                ImpactScaled(14f + flare.Length * 1.2f) * flashExpansion;
            float halfWidth =
                ImpactScaled(1.8f + flare.Width * 1.1f) *
                (1f - flashProgress * 0.55f);
            Vector2 root =
                _targetOffset - direction * ImpactScaled(4f);
            Vector2[] flarePoints =
            [
                root + direction * length,
                root + normal * halfWidth,
                root - direction * ImpactScaled(5f),
                root - normal * halfWidth
            ];
            DrawColoredPolygon(
                flarePoints,
                new Color(
                    0.56f,
                    0.88f,
                    1f,
                    (0.10f + i * 0.012f) * flashFade));
        }

        foreach (ImpactShard shard in _impactShards)
        {
            float localElapsed = impactElapsed - shard.Delay;
            if (localElapsed <= 0f)
            {
                continue;
            }

            float localProgress = Mathf.Clamp(
                localElapsed / Math.Max(impactDuration - shard.Delay, 0.01f),
                0f,
                1f);
            float motion = EaseOutCubic(localProgress);
            float fade =
                1f - Mathf.SmoothStep(0.48f, 1f, localProgress);
            Vector2 center =
                _targetOffset +
                shard.Direction * ImpactScaled(shard.Speed) * localElapsed +
                Vector2.Down * ImpactScaled(shard.Drop) *
                localElapsed * localElapsed;
            float rotation =
                shard.Direction.Angle() + shard.Spin * localElapsed;
            Vector2 facing = Vector2.FromAngle(rotation);
            Vector2 normal = facing.Orthogonal();
            float length =
                ImpactScaled(shard.Length) *
                Mathf.Lerp(0.45f, 1f, Mathf.Min(motion * 2.4f, 1f));
            float halfWidth =
                ImpactScaled(shard.Width) * 0.5f *
                Mathf.Lerp(0.62f, 1f, Mathf.Min(motion * 2.8f, 1f));
            Vector2 tip = center + facing * length * 0.62f;
            Vector2 baseCenter = center - facing * length * 0.38f;
            Vector2[] shardPoints =
            [
                tip,
                baseCenter + normal * halfWidth,
                baseCenter - normal * halfWidth
            ];
            DrawColoredPolygon(
                shardPoints,
                new Color(
                    Mathf.Lerp(0.38f, 0.74f, shard.Brightness),
                    Mathf.Lerp(0.75f, 0.94f, shard.Brightness),
                    1f,
                    0.68f * fade));
            DrawLine(
                baseCenter,
                tip,
                new Color(0.88f, 0.98f, 1f, 0.68f * fade),
                Mathf.Max(ImpactScaled(0.45f), 0.8f),
                true);
        }

        foreach (ImpactSpeck speck in _impactSpecks)
        {
            float localElapsed = impactElapsed - speck.Delay;
            if (localElapsed <= 0f)
            {
                continue;
            }

            float localProgress = Mathf.Clamp(
                localElapsed / Math.Max(impactDuration - speck.Delay, 0.01f),
                0f,
                1f);
            float fade =
                1f - Mathf.SmoothStep(0.38f, 1f, localProgress);
            Vector2 tip =
                _targetOffset +
                speck.Direction * ImpactScaled(speck.Speed) * localElapsed +
                Vector2.Down * ImpactScaled(18f) *
                localElapsed * localElapsed;
            float length =
                ImpactScaled(speck.Length) *
                Mathf.Lerp(1f, 0.35f, localProgress);
            DrawLine(
                tip - speck.Direction * length,
                tip,
                new Color(
                    0.75f,
                    0.95f,
                    1f,
                    0.62f * speck.Brightness * fade),
                Mathf.Max(ImpactScaled(speck.Width), 0.8f),
                true);
        }
    }

    private float GetTravelProgress()
    {
        return Mathf.Clamp((_time - LaunchDelay) / TravelDuration, 0f, 1f);
    }

    private Vector2 GetDirection()
    {
        return _targetOffset.LengthSquared() > 0.001f
            ? _targetOffset.Normalized()
            : Vector2.Right;
    }

    private void DrawTrailLayer(
        float startProgress,
        float endProgress,
        Color color,
        float width)
    {
        const int SampleCount = 24;
        Vector2[] points = new Vector2[SampleCount * 2];
        for (int i = 0; i < SampleCount; i++)
        {
            float sampleProgress = i / (SampleCount - 1f);
            float progress = Mathf.Lerp(
                startProgress,
                endProgress,
                sampleProgress);
            Vector2 center = GetPathPosition(progress);
            Vector2 tangent = GetPathTangent(progress).Normalized();
            Vector2 normal = tangent.Orthogonal();
            float taper = Mathf.SmoothStep(0f, 1f, sampleProgress);
            Vector2 halfWidth = normal * width * 0.5f * taper;

            points[i] = center + halfWidth;
            points[points.Length - 1 - i] = center - halfWidth;
        }

        DrawColoredPolygon(points, color);
    }

    private Vector2 GetPathPosition(float progress)
    {
        if (!_useArcTrajectory)
        {
            return _targetOffset * progress;
        }

        float remaining = 1f - progress;
        return
            2f * remaining * progress * _arcControlPoint +
            progress * progress * _targetOffset;
    }

    private Vector2 GetPathTangent(float progress)
    {
        if (!_useArcTrajectory)
        {
            return GetDirection();
        }

        Vector2 tangent =
            2f * (1f - progress) * _arcControlPoint +
            2f * progress * (_targetOffset - _arcControlPoint);
        return tangent.LengthSquared() > 0.001f
            ? tangent
            : GetDirection();
    }

    private static float EaseOutCubic(float progress)
    {
        return 1f - Mathf.Pow(1f - progress, 3f);
    }

    private float Scaled(float value)
    {
        return value * EffectScale * _sizeMultiplier;
    }

    private float ImpactScaled(float value)
    {
        float impactMultiplier = _impactSizeMultiplier <= 1f
            ? Mathf.Lerp(
                0.75f,
                1f,
                Mathf.InverseLerp(
                    2f / 3f,
                    1f,
                    _impactSizeMultiplier))
            : Mathf.Lerp(
                1f,
                1.55f,
                Mathf.InverseLerp(
                    1f,
                    2f,
                    _impactSizeMultiplier));
        return value * EffectScale * impactMultiplier;
    }

    private readonly struct ImpactShard(
        Vector2 direction,
        float speed,
        float length,
        float width,
        float delay,
        float spin,
        float drop,
        float brightness)
    {
        public Vector2 Direction { get; } = direction;
        public float Speed { get; } = speed;
        public float Length { get; } = length;
        public float Width { get; } = width;
        public float Delay { get; } = delay;
        public float Spin { get; } = spin;
        public float Drop { get; } = drop;
        public float Brightness { get; } = brightness;
    }

    private readonly struct ImpactSpeck(
        Vector2 direction,
        float speed,
        float length,
        float width,
        float delay,
        float brightness)
    {
        public Vector2 Direction { get; } = direction;
        public float Speed { get; } = speed;
        public float Length { get; } = length;
        public float Width { get; } = width;
        public float Delay { get; } = delay;
        public float Brightness { get; } = brightness;
    }
}
