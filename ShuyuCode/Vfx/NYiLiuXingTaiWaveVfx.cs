using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A rotating square ripple that expands outward from the power owner.
/// The ripple shader also applies a subtle, tightly-localized screen distortion.
/// </summary>
public partial class NYiLiuXingTaiWaveVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.PowerVfxPath}/vfx_YiLiuXingTaiPowerWave.tscn";

    private const float Duration = 1.30f;
    private const float CoverageMultiplier = 2.2f;
    private const float StartingHalfSize = 40f;

    [Export]
    private ColorRect? _wavePanel;

    public static void Play(Creature? owner)
    {
        if (TestMode.IsOn || owner == null || NCombatRoom.Instance == null)
        {
            return;
        }

        NCreature? creatureNode = NCombatRoom.Instance.GetCreatureNode(owner);
        if (creatureNode == null)
        {
            return;
        }

        NYiLiuXingTaiWaveVfx vfx = VFXUtil.GenVFXNode<NYiLiuXingTaiWaveVfx>(ScenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;
    }

    public override void _Ready()
    {
        ConfigureFullScreenCoverage();
        SetProgress(0f);
        TaskHelper.RunSafely(PlaySequence());
    }

    private void ConfigureFullScreenCoverage()
    {
        if (_wavePanel == null)
        {
            return;
        }

        // The panel extends beyond a full viewport diagonal in every direction.
        // This leaves the procedural square free to rotate past every screen corner
        // without ever exposing the ColorRect's clipping boundary.
        float panelSide = GetViewportRect().Size.Length() * CoverageMultiplier;
        _wavePanel.Position = Vector2.One * (-panelSide * 0.5f);
        _wavePanel.Size = Vector2.One * panelSide;

        if (_wavePanel.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("panel_side_pixels", panelSide);
            material.SetShaderParameter("start_radius", StartingHalfSize / panelSide);
        }
    }

    private async Task PlaySequence()
    {
        Tween ripple = CreateTween();
        ripple.TweenMethod(Callable.From<float>(SetProgress), 0f, 1f, Duration)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(ripple, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void SetProgress(float progress)
    {
        if (_wavePanel?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("progress", progress);
        }
    }
}
