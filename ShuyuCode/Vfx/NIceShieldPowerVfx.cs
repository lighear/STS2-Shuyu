using Godot;

namespace Shuyu.Vfx;

/// <summary>
/// Two translucent, side-on magic arcs that frame the creature without placing
/// a shield texture over the portrait itself.
/// </summary>
public partial class NIceShieldPowerVfx : Node2D
{
    public const int MaxVisualStacks = 30;

    private const float MinOpacity = 0.38f;
    private const float MaxOpacity = 1f;
    private const float MaxThicknessScale = 2f;
    private const float HorizontalOffsetFactor = 0.30f;
    private const float ShieldHeightFactor = 0.70f;
    private const float SourceShieldHeight = 360f;
    private const float SourceRasterScale = 4f;

    [Export]
    private Sprite2D? _leftShield;

    [Export]
    private Sprite2D? _rightShield;

    [Export]
    private GpuParticles2D? _leftSnowflakes;

    [Export]
    private GpuParticles2D? _rightSnowflakes;

    [Export]
    private GpuParticles2D? _leftDarkSnowflakes;

    [Export]
    private GpuParticles2D? _rightDarkSnowflakes;

    private Vector2 _leftBasePosition;
    private Vector2 _rightBasePosition;
    private float _shieldScale;
    private float _thicknessScale = 1f;
    private float _darkSnowVerticalOffset;
    private float _opacity;
    private float _elapsed;
    private bool _active;
    private bool _showDarkSnowflakes;
    private bool _particleMaterialsLocalized;

    public override void _Ready()
    {
        EnsureParticleMaterialsLocalized();
    }

    public override void _Process(double delta)
    {
        if (!_active || _leftShield == null || _rightShield == null)
        {
            return;
        }

        _elapsed += (float)delta;
        float breathWave = Mathf.Sin(_elapsed * 1.08f);
        float breathAlpha = 0.78f + (breathWave + 1f) * 0.11f;
        float breathScale = 1f + breathWave * 0.018f;
        float breathExpansion = breathWave * 3.2f;
        Vector2 shieldScale = new(
            _shieldScale * _thicknessScale * breathScale,
            _shieldScale * breathScale
        );
        _leftShield.Position = _leftBasePosition + new Vector2(-breathExpansion, 0f);
        _rightShield.Position = _rightBasePosition + new Vector2(breathExpansion, 0f);
        _leftShield.Scale = shieldScale;
        _rightShield.Scale = shieldScale;
        SetShieldAlpha(_leftShield, _opacity * breathAlpha);
        SetShieldAlpha(_rightShield, _opacity * breathAlpha);

        if (_leftSnowflakes != null)
        {
            _leftSnowflakes.Position = _leftBasePosition + new Vector2(-breathExpansion, 0f);
            SetSnowflakeAlpha(_leftSnowflakes, _opacity * (0.62f + (breathWave + 1f) * 0.05f));
        }

        if (_rightSnowflakes != null)
        {
            _rightSnowflakes.Position = _rightBasePosition + new Vector2(breathExpansion, 0f);
            SetSnowflakeAlpha(_rightSnowflakes, _opacity * (0.62f + (breathWave + 1f) * 0.05f));
        }

        if (_leftDarkSnowflakes != null)
        {
            _leftDarkSnowflakes.Position = _leftBasePosition
                + new Vector2(-breathExpansion, _darkSnowVerticalOffset);
            SetDarkSnowflakeAlpha(_leftDarkSnowflakes, _opacity * (0.58f + (breathWave + 1f) * 0.05f));
        }

        if (_rightDarkSnowflakes != null)
        {
            _rightDarkSnowflakes.Position = _rightBasePosition
                + new Vector2(breathExpansion, _darkSnowVerticalOffset);
            SetDarkSnowflakeAlpha(_rightDarkSnowflakes, _opacity * (0.58f + (breathWave + 1f) * 0.05f));
        }
    }

    public void Configure(Vector2 boundsSize, int stackAmount, bool showDarkSnowflakes)
    {
        if (_leftShield == null || _rightShield == null || boundsSize.X <= 0f || boundsSize.Y <= 0f)
        {
            return;
        }

        EnsureParticleMaterialsLocalized();

        float cappedStacks = Mathf.Clamp(stackAmount, 0, MaxVisualStacks);
        _active = stackAmount > 0;
        _showDarkSnowflakes = _active && showDarkSnowflakes;
        float strength = Mathf.Clamp(
            (cappedStacks - 1f) / (MaxVisualStacks - 1f),
            0f,
            1f
        );
        _opacity = _active ? Mathf.Lerp(MinOpacity, MaxOpacity, strength) : 0f;
        _thicknessScale = Mathf.Lerp(1f, MaxThicknessScale, strength);

        _shieldScale = Mathf.Clamp(
            boundsSize.Y * ShieldHeightFactor / (SourceShieldHeight * SourceRasterScale),
            0.58f / SourceRasterScale,
            1.18f / SourceRasterScale
        );
        float horizontalOffset = boundsSize.X * HorizontalOffsetFactor + 2f;
        float verticalOffset = boundsSize.Y * 0.01f;
        _darkSnowVerticalOffset = -boundsSize.Y * 0.10f;

        _leftBasePosition = new Vector2(-horizontalOffset, verticalOffset);
        _rightBasePosition = new Vector2(horizontalOffset, verticalOffset);
        _leftShield.Position = _leftBasePosition;
        _rightShield.Position = _rightBasePosition;
        Vector2 initialScale = new(_shieldScale * _thicknessScale, _shieldScale);
        _leftShield.Scale = initialScale;
        _rightShield.Scale = initialScale;
        _leftShield.Visible = _active;
        _rightShield.Visible = _active;
        SetShieldAlpha(_leftShield, _opacity);
        SetShieldAlpha(_rightShield, _opacity);

        ConfigureSnowflakes(_leftSnowflakes, _leftBasePosition, boundsSize);
        ConfigureSnowflakes(_rightSnowflakes, _rightBasePosition, boundsSize);
        ConfigureDarkSnowflakes(_leftDarkSnowflakes, _leftBasePosition, boundsSize);
        ConfigureDarkSnowflakes(_rightDarkSnowflakes, _rightBasePosition, boundsSize);
    }

    private static void SetShieldAlpha(Sprite2D shield, float alpha)
    {
        shield.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(alpha, 0f, 1f));
    }

    private void EnsureParticleMaterialsLocalized()
    {
        if (_particleMaterialsLocalized)
        {
            return;
        }

        if (_leftSnowflakes?.ProcessMaterial != null)
        {
            _leftSnowflakes.ProcessMaterial = (Material)_leftSnowflakes.ProcessMaterial.Duplicate();
        }

        if (_rightSnowflakes?.ProcessMaterial != null)
        {
            _rightSnowflakes.ProcessMaterial = (Material)_rightSnowflakes.ProcessMaterial.Duplicate();
        }

        if (_leftDarkSnowflakes?.ProcessMaterial != null)
        {
            _leftDarkSnowflakes.ProcessMaterial = (Material)_leftDarkSnowflakes.ProcessMaterial.Duplicate();
        }

        if (_rightDarkSnowflakes?.ProcessMaterial != null)
        {
            _rightDarkSnowflakes.ProcessMaterial = (Material)_rightDarkSnowflakes.ProcessMaterial.Duplicate();
        }

        _particleMaterialsLocalized = true;
    }

    private void ConfigureSnowflakes(GpuParticles2D? emitter, Vector2 position, Vector2 boundsSize)
    {
        if (emitter == null)
        {
            return;
        }

        emitter.Position = position;
        emitter.Visible = _active;
        emitter.Emitting = _active;
        SetSnowflakeAlpha(emitter, _opacity * 0.68f);

        if (emitter.ProcessMaterial is ParticleProcessMaterial material)
        {
            material.EmissionBoxExtents = new Vector3(
                Mathf.Max(12f, boundsSize.X * 0.045f),
                boundsSize.Y * 0.31f,
                1f
            );
        }
    }

    private static void SetSnowflakeAlpha(GpuParticles2D emitter, float alpha)
    {
        emitter.Modulate = new Color(0.94f, 0.97f, 1f, Mathf.Clamp(alpha, 0f, 1f));
    }

    private void ConfigureDarkSnowflakes(GpuParticles2D? emitter, Vector2 position, Vector2 boundsSize)
    {
        if (emitter == null)
        {
            return;
        }

        // Dark flakes begin in a narrow band above the shield and fall for a
        // fixed short lifetime. Velocity and gravity scale with creature size,
        // placing their final fade close to the shield's lower edge.
        emitter.Position = position + new Vector2(0f, _darkSnowVerticalOffset);
        emitter.Visible = _showDarkSnowflakes;
        emitter.Emitting = _showDarkSnowflakes;
        emitter.Lifetime = 2.15f;
        emitter.Randomness = 0.10f;
        SetDarkSnowflakeAlpha(emitter, _opacity * 0.64f);

        if (emitter.ProcessMaterial is ParticleProcessMaterial material)
        {
            material.EmissionBoxExtents = new Vector3(
                Mathf.Max(12f, boundsSize.X * 0.045f),
                Mathf.Max(10f, boundsSize.Y * 0.055f),
                1f
            );
            material.InitialVelocityMin = boundsSize.Y * 0.04f;
            material.InitialVelocityMax = boundsSize.Y * 0.065f;
            material.Gravity = new Vector3(0f, boundsSize.Y * 0.16f, 0f);
        }
    }

    private static void SetDarkSnowflakeAlpha(GpuParticles2D emitter, float alpha)
    {
        emitter.Modulate = new Color(0.025f, 0.018f, 0.045f, Mathf.Clamp(alpha, 0f, 1f));
    }
}
