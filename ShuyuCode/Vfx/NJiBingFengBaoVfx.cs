using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A short, viewport-filling blizzard made from layered wind streaks,
/// drifting snow, curved gust bands, and a cold fog wash.
/// </summary>
public partial class NJiBingFengBaoVfx : Node2D
{
    public static readonly string ScenePath =
        $"{VFXUtil.CardVfxPath}/vfx_JiBingFengBao.tscn";

    private const float FadeInDuration = 0.18f;
    private const float FadeOutStart = 1.08f;
    private const float VisualDuration = 1.50f;
    private const int BackStreakCount = 160;
    private const int MidStreakCount = 135;
    private const int FrontStreakCount = 52;
    private const int SnowSpeckCount = 110;
    private const int GustBandCount = 7;
    private const int GustSamples = 42;

    private readonly List<StormStreak> _streaks = [];
    private readonly List<SnowSpeck> _specks = [];
    private Vector2 _viewportSize = new(1920f, 1080f);
    private float _time;

    public static void Play()
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null)
        {
            return;
        }

        NJiBingFengBaoVfx vfx =
            VFXUtil.GenVFXNode<NJiBingFengBaoVfx>(ScenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = vfx.GetViewportRect().Size * 0.5f;
    }

    public override void _Ready()
    {
        _viewportSize = GetViewportRect().Size;
        BuildStreakLayer(
            BackStreakCount,
            520f,
            940f,
            8f,
            42f,
            0.45f,
            1.35f,
            0.18f,
            0.34f,
            0);
        BuildStreakLayer(
            MidStreakCount,
            800f,
            1400f,
            24f,
            86f,
            0.8f,
            2.3f,
            0.22f,
            0.42f,
            1);
        BuildStreakLayer(
            FrontStreakCount,
            1200f,
            2100f,
            70f,
            175f,
            1.8f,
            4.5f,
            0.25f,
            0.50f,
            2);
        BuildSpecks();
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        if (_time >= VisualDuration)
        {
            SetProcess(false);
            this.QueueFreeSafely();
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        float strength = GetStrength();
        if (strength <= 0f)
        {
            return;
        }

        Vector2 topLeft = _viewportSize * -0.5f;
        DrawRect(
            new Rect2(topLeft, _viewportSize),
            new Color(0.39f, 0.53f, 0.70f, 0.24f * strength));
        DrawGustBands(strength);
        DrawSpecks(strength);
        DrawStreaks(strength);
    }

    private void BuildStreakLayer(
        int count,
        float minSpeed,
        float maxSpeed,
        float minLength,
        float maxLength,
        float minWidth,
        float maxWidth,
        float minAlpha,
        float maxAlpha,
        int depth)
    {
        for (int i = 0; i < count; i++)
        {
            _streaks.Add(new StormStreak(
                new Vector2(
                    Rng.Chaotic.NextFloat(0f, 1f),
                    Rng.Chaotic.NextFloat(0f, 1f)),
                Rng.Chaotic.NextFloat(minSpeed, maxSpeed),
                Rng.Chaotic.NextFloat(minLength, maxLength),
                Rng.Chaotic.NextFloat(minWidth, maxWidth),
                Rng.Chaotic.NextFloat(minAlpha, maxAlpha),
                Rng.Chaotic.NextFloat(-0.18f, 0.20f),
                Rng.Chaotic.NextFloat(8f, 48f),
                Rng.Chaotic.NextFloat(0f, Mathf.Tau),
                Rng.Chaotic.NextFloat(1.8f, 4.6f),
                depth));
        }
    }

    private void BuildSpecks()
    {
        for (int i = 0; i < SnowSpeckCount; i++)
        {
            _specks.Add(new SnowSpeck(
                new Vector2(
                    Rng.Chaotic.NextFloat(0f, 1f),
                    Rng.Chaotic.NextFloat(0f, 1f)),
                Rng.Chaotic.NextFloat(330f, 900f),
                Rng.Chaotic.NextFloat(0.55f, 1.8f),
                Rng.Chaotic.NextFloat(0.30f, 0.70f),
                Rng.Chaotic.NextFloat(0f, Mathf.Tau),
                Rng.Chaotic.NextFloat(8f, 30f)));
        }
    }

    private void DrawStreaks(float strength)
    {
        float margin = 300f;
        float travelWidth = _viewportSize.X + margin * 2f;
        Vector2 topLeft = _viewportSize * -0.5f;

        foreach (StormStreak streak in _streaks)
        {
            float wrappedX = Wrap(
                streak.Start.X * travelWidth + _time * streak.Speed,
                travelWidth);
            float x = topLeft.X - margin + wrappedX;
            float y =
                topLeft.Y + streak.Start.Y * _viewportSize.Y +
                Mathf.Sin(_time * streak.Frequency + streak.Phase) *
                streak.Sway;
            float flutter =
                Mathf.Sin(_time * (streak.Frequency * 0.73f) +
                    streak.Phase * 1.7f) * 0.035f;
            float flowField = Mathf.Sin(
                streak.Start.Y * Mathf.Tau * 1.45f +
                _time * 2.15f +
                streak.Phase * 0.22f) * 0.10f;
            Vector2 direction =
                new Vector2(
                    1f,
                    streak.Slope + flutter + flowField).Normalized();
            Vector2 head = new(x, y);
            Vector2 tail = head - direction * streak.Length;
            float depthBoost = 0.82f + streak.Depth * 0.15f;
            float alpha = streak.Alpha * strength * depthBoost;

            DrawTaperedStreak(
                tail,
                head,
                streak.Width * 3f,
                new Color(0.47f, 0.76f, 1f, alpha * 0.16f));
            DrawTaperedStreak(
                tail + direction * streak.Length * 0.10f,
                head,
                streak.Width,
                new Color(0.82f, 0.96f, 1.16f, alpha));

            if (streak.Depth == 2)
            {
                DrawTaperedStreak(
                    tail + direction * streak.Length * 0.38f,
                    head,
                    Mathf.Max(0.8f, streak.Width * 0.30f),
                    new Color(1.08f, 1.16f, 1.28f, alpha * 0.72f));
            }
        }
    }

    private void DrawSpecks(float strength)
    {
        float margin = 40f;
        float travelWidth = _viewportSize.X + margin * 2f;
        Vector2 topLeft = _viewportSize * -0.5f;

        foreach (SnowSpeck speck in _specks)
        {
            float wrappedX = Wrap(
                speck.Start.X * travelWidth + _time * speck.Speed,
                travelWidth);
            Vector2 position = new(
                topLeft.X - margin + wrappedX,
                topLeft.Y + speck.Start.Y * _viewportSize.Y +
                Mathf.Sin(_time * 2.6f + speck.Phase) * speck.Sway);
            float pulse =
                0.72f + Mathf.Sin(_time * 5.2f + speck.Phase) * 0.28f;

            Vector2 direction = new Vector2(
                1f,
                Mathf.Sin(speck.Phase + _time * 2.1f) * 0.16f)
                .Normalized();
            float length = 4f + speck.Radius * 6f;
            DrawTaperedStreak(
                position - direction * length,
                position,
                speck.Radius * 2.6f,
                new Color(
                    0.54f,
                    0.82f,
                    1.08f,
                    0.13f * strength * pulse));
            DrawCircle(
                position,
                speck.Radius * 0.72f,
                new Color(0.92f, 0.98f, 1.15f,
                    speck.Alpha * strength * pulse));
        }
    }

    private void DrawGustBands(float strength)
    {
        Vector2 topLeft = _viewportSize * -0.5f;
        for (int gust = 0; gust < GustBandCount; gust++)
        {
            Vector2[] points = new Vector2[GustSamples];
            float verticalRatio = (gust + 0.55f) / GustBandCount;
            float phase = gust * 1.73f + _time * (2.4f + gust * 0.08f);
            float amplitude = 22f + (gust % 3) * 14f;

            for (int sample = 0; sample < GustSamples; sample++)
            {
                float ratio = sample / (GustSamples - 1f);
                float x = topLeft.X - 90f + ratio * (_viewportSize.X + 180f);
                float y =
                    topLeft.Y + verticalRatio * _viewportSize.Y +
                    Mathf.Sin(ratio * 8.2f + phase) * amplitude +
                    Mathf.Sin(ratio * 19f - phase * 0.65f) * 7f;
                points[sample] = new Vector2(x, y);
            }

            DrawPolyline(
                points,
                new Color(0.45f, 0.67f, 0.88f, 0.016f * strength),
                18f + (gust % 2) * 10f,
                true);
            DrawPolyline(
                points,
                new Color(0.78f, 0.91f, 1.08f, 0.045f * strength),
                2f + (gust % 2),
                true);
        }
    }

    private void DrawTaperedStreak(
        Vector2 tail,
        Vector2 head,
        float width,
        Color color)
    {
        Vector2 direction = (head - tail).Normalized();
        Vector2 normal = direction.Orthogonal() * width * 0.5f;
        Vector2[] points =
        [
            tail,
            head + normal,
            head - normal
        ];
        DrawColoredPolygon(points, color);
    }

    private float GetStrength()
    {
        if (_time < FadeInDuration)
        {
            return Mathf.SmoothStep(0f, 1f, _time / FadeInDuration);
        }

        if (_time < FadeOutStart)
        {
            return 1f;
        }

        return 1f - Mathf.SmoothStep(
            0f,
            1f,
            (_time - FadeOutStart) /
            (VisualDuration - FadeOutStart));
    }

    private static float Wrap(float value, float length)
    {
        return value - Mathf.Floor(value / length) * length;
    }

    private readonly struct StormStreak(
        Vector2 start,
        float speed,
        float length,
        float width,
        float alpha,
        float slope,
        float sway,
        float phase,
        float frequency,
        int depth)
    {
        public Vector2 Start { get; } = start;
        public float Speed { get; } = speed;
        public float Length { get; } = length;
        public float Width { get; } = width;
        public float Alpha { get; } = alpha;
        public float Slope { get; } = slope;
        public float Sway { get; } = sway;
        public float Phase { get; } = phase;
        public float Frequency { get; } = frequency;
        public int Depth { get; } = depth;
    }

    private readonly struct SnowSpeck(
        Vector2 start,
        float speed,
        float radius,
        float alpha,
        float phase,
        float sway)
    {
        public Vector2 Start { get; } = start;
        public float Speed { get; } = speed;
        public float Radius { get; } = radius;
        public float Alpha { get; } = alpha;
        public float Phase { get; } = phase;
        public float Sway { get; } = sway;
    }
}
