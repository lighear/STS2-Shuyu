using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class IceShieldPower : ModPowerTemplate
{
    private const string VfxNodeName = "VfxIceShieldPower";
    private readonly string VfxScenePath = $"{VFXUtil.PowerVfxPath}/vfx_IceShieldPower.tscn";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DisplayAmountChanged -= OnDisplayAmountChanged;
        DisplayAmountChanged += OnDisplayAmountChanged;
        UpdateVfx();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        DisplayAmountChanged -= OnDisplayAmountChanged;
        var creatureBounds = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals.Bounds;
        creatureBounds?.GetNodeOrNull<NIceShieldPowerVfx>(VfxNodeName)?.QueueFree();
        return Task.CompletedTask;
    }

    private void OnDisplayAmountChanged()
    {
        UpdateVfx();
    }

    private void UpdateVfx()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        var creatureVisuals = creatureNode?.Visuals;
        var creatureBounds = creatureVisuals?.Bounds;
        if (creatureNode == null || creatureVisuals == null || creatureBounds == null)
        {
            return;
        }

        NPowerVfxLayout.Resolve(
            Owner,
            creatureNode,
            out Vector2 visualSize,
            out Vector2 visualCenter);

        NIceShieldPowerVfx? vfx = creatureBounds.GetNodeOrNull<NIceShieldPowerVfx>(VfxNodeName);
        if (vfx == null)
        {
            vfx = VFXUtil.GenVFXNode<NIceShieldPowerVfx>(VfxScenePath);
            creatureBounds.AddChildSafely(vfx);
        }

        vfx.Position = visualCenter;
        vfx.Configure(visualSize, Amount, Owner.HasPower<JinShuJieJiePower>());
    }

    public void RefreshVfx()
    {
        UpdateVfx();
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            await CreatureCmd.GainBlock(Owner, amount, ValueProp.Unpowered, null);
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        }
    }

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && props.IsPoweredAttack() && amount >= 1)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
