using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

/// <summary>
/// A thin blue laser that remains connected to its target until the card finishes resolving.
/// </summary>
public partial class NCuiHuaGuangShuLaserVfx : Node2D
{
    public static readonly string ScenePath =
        $"{VFXUtil.CardVfxPath}/vfx_CuiHuaGuangShuLaser.tscn";

    private const float SourceTextureWidth = 120f;
    private const float BeamThicknessScale = 3f;
    private const float ExtendDuration = 0.15f;
    private const float RetractDuration = 0.15f;

    private NCreature? _ownerNode;
    private NCreature? _targetNode;
    private Sprite2D _beam = null!;
    private Vector2 _lastStartPosition;
    private Vector2 _lastEndPosition;
    private float _extension;
    private bool _isFinishing;
    private Tween? _motionTween;

    public static NCuiHuaGuangShuLaserVfx? Create(Creature owner, Creature? target)
    {
        if (TestMode.IsOn || target == null || NCombatRoom.Instance == null)
        {
            return null;
        }

        NCreature? ownerNode = NCombatRoom.Instance.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance.GetCreatureNode(target);
        if (ownerNode == null || targetNode == null)
        {
            return null;
        }

        NCuiHuaGuangShuLaserVfx vfx =
            VFXUtil.GenVFXNode<NCuiHuaGuangShuLaserVfx>(ScenePath);
        vfx._ownerNode = ownerNode;
        vfx._targetNode = targetNode;
        vfx._lastStartPosition = VFXUtil.GetShuyuStaffHeadPosition(ownerNode);
        vfx._lastEndPosition = targetNode.VfxSpawnPosition;

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        return vfx;
    }

    public override void _Ready()
    {
        _beam = GetNode<Sprite2D>("Beam");

        _extension = 0f;
        _motionTween = CreateTween();
        _motionTween.TweenMethod(
                Callable.From<float>(value => _extension = value),
                0f,
                1f,
                ExtendDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    public override void _Process(double delta)
    {
        UpdateTrackedPositions();
        UpdateBeamTransform();
    }

    public async Task FinishAsync()
    {
        if (_isFinishing || !GodotObject.IsInstanceValid(this))
        {
            return;
        }

        _isFinishing = true;
        _motionTween?.Kill();

        Tween retract = CreateTween();
        retract.TweenMethod(
                Callable.From<float>(value => _extension = value),
                _extension,
                0f,
                RetractDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Expo);
        await ToSignal(retract, Tween.SignalName.Finished);
        this.QueueFreeSafely();
    }

    private void UpdateTrackedPositions()
    {
        if (_ownerNode != null
            && GodotObject.IsInstanceValid(_ownerNode)
            && _ownerNode.IsInsideTree())
        {
            _lastStartPosition = VFXUtil.GetShuyuStaffHeadPosition(_ownerNode);
        }

        if (_targetNode != null
            && GodotObject.IsInstanceValid(_targetNode)
            && _targetNode.IsInsideTree())
        {
            _lastEndPosition = _targetNode.VfxSpawnPosition;
        }
    }

    private void UpdateBeamTransform()
    {
        Vector2 offset = _lastEndPosition - _lastStartPosition;
        float fullLength = offset.Length();
        float visibleLength = fullLength * _extension;
        if (visibleLength < 1f)
        {
            Visible = false;
            return;
        }

        Visible = true;
        GlobalPosition = _lastStartPosition;
        GlobalRotation = offset.Angle();

        _beam.Position = new Vector2(visibleLength * 0.5f, 0f);
        _beam.Scale = new Vector2(
            visibleLength / SourceTextureWidth,
            BeamThicknessScale);
    }
}
