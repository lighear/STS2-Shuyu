using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// Twelve ice diamonds spiral inward and meet at the target's center.
/// </summary>
public partial class NHuanRaoDaJiVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_HuanRaoDaJi.tscn";

    private const int CrystalCount = 12;
    private const float ConvergeDuration = 0.36f;
    private const float AttackResolveTime = 0.31f;
    private const float ImpactDuration = 0.08f;
    private const float SpinRadians = Mathf.Pi * 1.2f;

    [Export]
    private Sprite2D? _crystalTemplate;

    [Export]
    private Sprite2D? _impactFlash;

    private readonly List<Sprite2D> _crystals = [];
    private float _startRadius = 160f;

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

        NHuanRaoDaJiVfx vfx = VFXUtil.GenVFXNode<NHuanRaoDaJiVfx>(ScenePath);
        Vector2 targetSize = creatureNode.Visuals.Bounds.Size;
        vfx._startRadius = Mathf.Clamp(Mathf.Max(targetSize.X, targetSize.Y) * 0.5f + 45f, 130f, 240f);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;

        // Let the hit begin just before the diamonds fully meet at the target center.
        await Cmd.Wait(AttackResolveTime);
    }

    public override void _Ready()
    {
        CreateCrystalRing();
        SetConvergence(0f);
        TaskHelper.RunSafely(PlaySequence());
    }

    private void CreateCrystalRing()
    {
        if (_crystalTemplate == null)
        {
            return;
        }

        for (int i = 0; i < CrystalCount; i++)
        {
            if (_crystalTemplate.Duplicate() is not Sprite2D crystal)
            {
                continue;
            }

            crystal.Name = $"IceDiamond{i + 1:00}";
            crystal.Visible = true;
            AddChild(crystal);
            _crystals.Add(crystal);
        }
    }

    private async Task PlaySequence()
    {
        Tween convergence = CreateTween();
        convergence.TweenMethod(Callable.From<float>(SetConvergence), 0f, 1f, ConvergeDuration)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(convergence, Tween.SignalName.Finished);

        foreach (Sprite2D crystal in _crystals)
        {
            crystal.Visible = false;
        }

        if (_impactFlash != null)
        {
            _impactFlash.Visible = true;
            _impactFlash.Scale = Vector2.One * 0.12f;
            _impactFlash.Modulate = new Color(0.82f, 0.96f, 1f, 0.9f);

            Tween impact = CreateTween().SetParallel();
            impact.TweenProperty(_impactFlash, "scale", Vector2.One * 0.72f, ImpactDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            impact.TweenProperty(_impactFlash, "modulate:a", 0f, ImpactDuration)
                .SetTrans(Tween.TransitionType.Linear);
            await ToSignal(impact, Tween.SignalName.Finished);
        }

        this.QueueFreeSafely();
    }

    private void SetConvergence(float progress)
    {
        float easedRadius = 1f - Mathf.SmoothStep(0f, 1f, progress);
        float radius = Mathf.Lerp(4f, _startRadius, easedRadius);
        float alpha = 0.6f * Mathf.Clamp(progress / 0.12f, 0f, 1f);
        float crystalScale = Mathf.Lerp(0.41f, 0.31f, progress);

        for (int i = 0; i < _crystals.Count; i++)
        {
            float startAngle = -Mathf.Pi * 0.5f + Mathf.Tau * i / CrystalCount;
            float angle = startAngle + SpinRadians * progress;
            Sprite2D crystal = _crystals[i];
            crystal.Position = Vector2.FromAngle(angle) * radius;
            crystal.Rotation = angle - Mathf.Pi * 0.5f;
            crystal.Scale = Vector2.One * crystalScale;
            crystal.Modulate = new Color(1f, 1f, 1f, alpha);
        }
    }
}
