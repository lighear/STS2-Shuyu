using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A dense, irregular black-violet fog ring that contracts from beyond the
/// viewport to the character, matching Jin Shu Ni Shang's sweeping card art.
/// </summary>
public partial class NJinShuNiShangFogRingVfx : Node2D
{
    public static readonly string ScenePath = $"{VFXUtil.CardVfxPath}/vfx_JinShuNiShangFogRing.tscn";

    private const float ResolveDelay = 0.60f;
    private const float FadeInDuration = 0.12f;
    private const float ContractionDuration = 1.50f;

    [Export]
    private ColorRect? _fogRing;

    private Vector2 _centerUv = new(0.5f, 0.5f);
    private float _startRadius = 1.4f;
    private float _endRadius = 0.1f;

    public static async Task PlayOpening(Creature? owner)
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

        Vector2 viewportSize = NCombatRoom.Instance.GetViewportRect().Size;
        Vector2 center = creatureNode.VfxSpawnPosition;
        NJinShuNiShangFogRingVfx vfx = VFXUtil.GenVFXNode<NJinShuNiShangFogRingVfx>(ScenePath);
        vfx._centerUv = new Vector2(center.X / viewportSize.X, center.Y / viewportSize.Y);
        vfx._startRadius = CalculateFarthestCornerDistance(center, viewportSize) / viewportSize.Y + 0.22f;

        Vector2 creatureSize = creatureNode.Visuals.Bounds.Size;
        vfx._endRadius = Mathf.Clamp(Mathf.Max(creatureSize.X, creatureSize.Y) * 0.34f / viewportSize.Y, 0.075f, 0.15f);

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = viewportSize * 0.5f;

        // Let the gameplay result occur while the dark ring is visibly closing in.
        await Cmd.Wait(ResolveDelay);
    }

    public override void _Ready()
    {
        ConfigureForViewport();
        TaskHelper.RunSafely(PlaySequence());
    }

    private void ConfigureForViewport()
    {
        if (_fogRing == null)
        {
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        _fogRing.Position = viewportSize * -0.5f;
        _fogRing.Size = viewportSize;

        if (_fogRing.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("center_uv", _centerUv);
            material.SetShaderParameter("aspect_ratio", viewportSize.X / viewportSize.Y);
            material.SetShaderParameter("start_radius", _startRadius);
            material.SetShaderParameter("end_radius", _endRadius);
            material.SetShaderParameter("progress", 0f);
            material.SetShaderParameter("opacity", 0f);
        }
    }

    private async Task PlaySequence()
    {
        Tween fadeIn = CreateTween();
        fadeIn.TweenMethod(Callable.From<float>(SetOpacity), 0f, 1f, FadeInDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        Tween contraction = CreateTween();
        contraction.TweenMethod(Callable.From<float>(SetProgress), 0f, 1f, ContractionDuration)
            .SetTrans(Tween.TransitionType.Linear);
        await ToSignal(contraction, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void SetProgress(float progress)
    {
        if (_fogRing?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("progress", progress);
        }
    }

    private void SetOpacity(float opacity)
    {
        if (_fogRing?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("opacity", opacity);
        }
    }

    private static float CalculateFarthestCornerDistance(Vector2 center, Vector2 viewportSize)
    {
        float topLeft = center.Length();
        float topRight = (center - new Vector2(viewportSize.X, 0f)).Length();
        float bottomLeft = (center - new Vector2(0f, viewportSize.Y)).Length();
        float bottomRight = (center - viewportSize).Length();
        return Mathf.Max(Mathf.Max(topLeft, topRight), Mathf.Max(bottomLeft, bottomRight));
    }
}
