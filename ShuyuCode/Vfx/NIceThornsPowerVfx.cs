using Godot;

namespace Shuyu.Vfx;

/// <summary>
/// A quiet, persistent ring of ice thorns. Individual emitters are placed on an
/// ellipse so the particles start outside the creature portrait and travel away
/// from it instead of covering it.
/// </summary>
public partial class NIceThornsPowerVfx : Node2D
{
    public const int MaxVisualStacks = 30;

    // Independent one-particle emitters preserve the requested density without
    // ever stacking multiple thorns at exactly the same origin.
    private const int MinEmitterCount = 28;
    private const int MaxEmitterCount = 64;
    private const float MinOpacity = 0.15f;
    private const float MaxOpacity = 0.40f;
    private const float HorizontalRadiusFactor = 0.34f;
    private const float VerticalRadiusFactor = 0.47f;
    private const float SideArcHalfAngle = Mathf.Pi * 0.30f;
    private const float MaxCycleJitter = 0.45f;
    private const int SnowflakeIntervalPerSide = 5;

    private static readonly Color[] ThornTints =
    [
        new(1f, 1f, 1f),
        new(0.88f, 0.97f, 1f),
        new(0.70f, 0.89f, 1f),
        new(0.48f, 0.73f, 1f),
    ];

    private static readonly Color[] SnowflakeTints =
    [
        new(1f, 1f, 1f),
        new(0.84f, 0.96f, 1f),
        new(0.62f, 0.84f, 1f),
    ];

    [Export]
    private GpuParticles2D? _emitterTemplate;

    [Export]
    private GpuParticles2D? _snowflakeEmitterTemplate;

    private readonly List<GpuParticles2D> _emitters = [];
    private readonly List<bool> _isSnowflakeEmitter = [];
    private readonly List<double> _secondsUntilEmission = [];
    private readonly RandomNumberGenerator _rng = new();
    private bool _rngIsRandomized;
    private Vector2 _boundsSize;
    private int _stackAmount;
    private int _activeEmitterCount;
    private float _opacity;
    private float _radiusX;
    private float _radiusY;
    private float _sizeScale;

    public override void _Ready()
    {
        EnsureRngRandomized();
        EnsureEmitters();
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (_activeEmitterCount <= 0 || _radiusX <= 0f || _radiusY <= 0f)
        {
            return;
        }

        for (int i = 0; i < _activeEmitterCount; i++)
        {
            _secondsUntilEmission[i] -= delta;
            if (_secondsUntilEmission[i] > 0.0)
            {
                continue;
            }

            EmitParticle(i);
            _secondsUntilEmission[i] = _emitters[i].Lifetime + _rng.RandfRange(0f, MaxCycleJitter);
        }
    }

    public void Configure(Vector2 boundsSize, int stackAmount)
    {
        EnsureRngRandomized();
        _boundsSize = boundsSize;
        _stackAmount = stackAmount;
        EnsureEmitters();
        Refresh();
    }

    private void EnsureEmitters()
    {
        if (_emitters.Count > 0 || _emitterTemplate == null || _snowflakeEmitterTemplate == null)
        {
            return;
        }

        _emitterTemplate.Emitting = false;
        _emitterTemplate.Visible = false;
        _snowflakeEmitterTemplate.Emitting = false;
        _snowflakeEmitterTemplate.Visible = false;

        for (int i = 0; i < MaxEmitterCount; i++)
        {
            // Every fifth slot on each side is a snowflake. Because slots are
            // paired left/right, the decorative snowflakes stay balanced too.
            bool isSnowflake = i / 2 % SnowflakeIntervalPerSide == 2;
            GpuParticles2D template = isSnowflake ? _snowflakeEmitterTemplate : _emitterTemplate;
            GpuParticles2D emitter = (GpuParticles2D)template.Duplicate();
            emitter.Name = $"{(isSnowflake ? "Snowflake" : "Thorn")}Emitter{i + 1:00}";
            emitter.Emitting = false;
            emitter.Visible = false;
            emitter.ProcessMaterial = emitter.ProcessMaterial?.Duplicate() as Material;
            AddChild(emitter);
            _emitters.Add(emitter);
            _isSnowflakeEmitter.Add(isSnowflake);

            _secondsUntilEmission.Add(0.0);
        }
    }

    private void EnsureRngRandomized()
    {
        if (_rngIsRandomized)
        {
            return;
        }

        _rng.Randomize();
        _rngIsRandomized = true;
    }

    private void Refresh()
    {
        if (_emitters.Count == 0 || _boundsSize.X <= 0f || _boundsSize.Y <= 0f)
        {
            return;
        }

        float cappedStacks = Mathf.Clamp(_stackAmount, 0, MaxVisualStacks);
        float strength = cappedStacks / MaxVisualStacks;
        int previousActiveEmitterCount = _activeEmitterCount;
        int nextActiveEmitterCount = _stackAmount <= 0
            ? 0
            : (int)MathF.Round(Mathf.Lerp(MinEmitterCount, MaxEmitterCount, strength));
        _activeEmitterCount = nextActiveEmitterCount;

        if (previousActiveEmitterCount <= 0 && _activeEmitterCount > 0)
        {
            RandomizeInitialEmissionOrder(_activeEmitterCount);
        }
        else if (_activeEmitterCount > previousActiveEmitterCount)
        {
            // Emitters unlocked by a stack increase should not start as a
            // simultaneous burst or resume the old index-based scan pattern.
            for (int i = previousActiveEmitterCount; i < _activeEmitterCount; i++)
            {
                _secondsUntilEmission[i] = _rng.RandfRange(0f, (float)_emitters[i].Lifetime);
            }
        }

        _opacity = Mathf.Lerp(MinOpacity, MaxOpacity, strength);
        _radiusX = _boundsSize.X * HorizontalRadiusFactor + 3f;
        _radiusY = _boundsSize.Y * VerticalRadiusFactor + 3f;
        _sizeScale = Mathf.Clamp(Mathf.Min(_boundsSize.X, _boundsSize.Y) / 310f, 0.72f, 1.15f);

        for (int i = 0; i < _emitters.Count; i++)
        {
            GpuParticles2D emitter = _emitters[i];
            bool active = i < _activeEmitterCount;
            emitter.Visible = active;
            if (!active)
            {
                emitter.Emitting = false;
            }
        }
    }

    private void RandomizeInitialEmissionOrder(int emitterCount)
    {
        int[] shuffledSlots = new int[emitterCount];
        for (int i = 0; i < emitterCount; i++)
        {
            shuffledSlots[i] = i;
        }

        // Fisher-Yates gives every spatial emitter an equal chance to occupy
        // every time slot. Slots stay evenly spaced, so the opening is random
        // without turning into a distracting burst.
        for (int i = emitterCount - 1; i > 0; i--)
        {
            int swapIndex = _rng.RandiRange(0, i);
            (shuffledSlots[i], shuffledSlots[swapIndex]) = (shuffledSlots[swapIndex], shuffledSlots[i]);
        }

        double fillDuration = _emitterTemplate?.Lifetime ?? 3.2;
        for (int i = 0; i < emitterCount; i++)
        {
            _secondsUntilEmission[i] = fillDuration * shuffledSlots[i] / emitterCount;
        }
    }

    private void EmitParticle(int emitterIndex)
    {
        GpuParticles2D emitter = _emitters[emitterIndex];
        bool isSnowflake = _isSnowflakeEmitter[emitterIndex];

        // Only use the left and right arcs. The excluded gaps around +/-90
        // degrees guarantee that no thorn originates above the head or below
        // the feet.
        // Alternate sides to keep the composition balanced. Within each side,
        // give every active emitter its own arc segment and randomize inside it;
        // positions still change every cycle, but two live thorns cannot share
        // the same origin or collapse into a distracting clump.
        bool rightSide = emitterIndex % 2 == 0;
        int rankOnSide = emitterIndex / 2;
        int emittersOnSide = rightSide
            ? (_activeEmitterCount + 1) / 2
            : _activeEmitterCount / 2;
        float segmentWidth = SideArcHalfAngle * 2f / emittersOnSide;
        float segmentStart = -SideArcHalfAngle + segmentWidth * rankOnSide;
        float inset = segmentWidth * 0.12f;
        float angleOffset = _rng.RandfRange(segmentStart + inset, segmentStart + segmentWidth - inset);
        float sideCenter = rightSide ? 0f : Mathf.Pi;
        float angle = sideCenter + angleOffset;
        Vector2 position = new(Mathf.Cos(angle) * _radiusX, Mathf.Sin(angle) * _radiusY);

        // The ellipse normal gives the true outward direction at this random
        // point, rather than merely pointing away from the ellipse center.
        Vector2 outward = new(Mathf.Cos(angle) / _radiusX, Mathf.Sin(angle) / _radiusY);
        outward = outward.Normalized();

        emitter.Position = position;
        emitter.Rotation = outward.Angle() + Mathf.Pi * 0.5f;

        // A single-particle emitter can otherwise replay an almost identical
        // random sequence on every restart. Explicitly vary each emission so
        // the persistent buff feels organic without becoming visually noisy.
        float sizeVariation = isSnowflake
            ? _rng.RandfRange(0.42f, 1.18f)
            : _rng.RandfRange(0.45f, 1.32f);
        float speed = isSnowflake
            ? _rng.RandfRange(3.5f, 11f)
            : _rng.RandfRange(7f, 23f);
        emitter.Scale = Vector2.One * (_sizeScale * sizeVariation);

        Color[] palette = isSnowflake ? SnowflakeTints : ThornTints;
        Color tint = palette[_rng.RandiRange(0, palette.Length - 1)];
        float alphaVariation = _rng.RandfRange(0.78f, 1.12f);
        tint.A = Mathf.Clamp(_opacity * alphaVariation * (isSnowflake ? 0.82f : 1f), 0f, 1f);
        emitter.Modulate = tint;

        if (emitter.ProcessMaterial is ParticleProcessMaterial material)
        {
            material.InitialVelocityMin = speed * 0.92f;
            material.InitialVelocityMax = speed * 1.08f;
        }

        emitter.Restart();
    }
}
