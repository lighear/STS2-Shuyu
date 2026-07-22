using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// The debuff-count damage impact for Ning Yu: a blue polar ring and broad glow.
/// </summary>
public partial class NNingYuDebuffImpactVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_NingYuDebuffImpact.tscn";

    private const float DamageResolveDelay = 0.08f;
    private const float CleanupDelay = 1.05f;

    [Export]
    private GpuParticles2D? _ring;

    [Export]
    private GpuParticles2D? _glow;

    private float _effectScale = 1f;

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

        NNingYuDebuffImpactVfx vfx = VFXUtil.GenVFXNode<NNingYuDebuffImpactVfx>(ScenePath);
        Vector2 targetSize = creatureNode.Visuals.Bounds.Size;
        vfx._effectScale = Mathf.Clamp(Mathf.Max(targetSize.X, targetSize.Y) / 360f, 0.78f, 1.22f);

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;

        await Cmd.Wait(DamageResolveDelay);
    }

    public override void _Ready()
    {
        Scale = Vector2.One * _effectScale;
        _ring?.Restart();
        _glow?.Restart();
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
        TaskHelper.RunSafely(CleanupAfterAnimation());
    }

    private async Task CleanupAfterAnimation()
    {
        await Cmd.Wait(CleanupDelay);
        this.QueueFreeSafely();
    }
}
