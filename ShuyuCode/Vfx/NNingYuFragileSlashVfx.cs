using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A single ice-blue thrust flash shared by Ning Yu's full Fragile sequence.
/// </summary>
public partial class NNingYuFragileSlashVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_NingYuFragileSlash.tscn";

    private const float HitDelay = 0.08f;
    private const float CleanupDelay = 0.28f;
    private const float SourceTextureWidth = 1000f;
    private const string ThrustSfx = "event:/sfx/characters/silent/silent_attack";

    [Export]
    private GpuParticles2D? _slash;

    private float _effectScale = 0.32f;

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

        NNingYuFragileSlashVfx vfx = VFXUtil.GenVFXNode<NNingYuFragileSlashVfx>(ScenePath);
        Vector2 targetSize = creatureNode.Visuals.Bounds.Size;
        float slashWidth = Mathf.Clamp(targetSize.X * 1.05f, 260f, 380f);
        vfx._effectScale = slashWidth / SourceTextureWidth;

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;

        await Cmd.Wait(HitDelay);
    }

    public override void _Ready()
    {
        Scale = Vector2.One * _effectScale;
        Rotation = Mathf.DegToRad(-6f);
        SfxCmd.Play(ThrustSfx, 0.75f);
        _slash?.Restart();
        TaskHelper.RunSafely(CleanupAfterAnimation());
    }

    private async Task CleanupAfterAnimation()
    {
        await Cmd.Wait(CleanupDelay);
        this.QueueFreeSafely();
    }
}
