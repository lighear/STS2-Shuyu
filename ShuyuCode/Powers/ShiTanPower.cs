using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using Shuyu.Vfx;

namespace Shuyu.Powers;

[RegisterPower]
public class ShiTanPower : ModPowerTemplate, IPowerExtraIconAmountLabelSpecsProvider
{
    private const string VfxNodeName = "VfxShiTanPower";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(0)
    ];

    private class Data
    {
        public bool triggeredThisTurn;
    }

    private bool TriggeredThisTurn
    {
        get
        {
            return GetInternalData<Data>().triggeredThisTurn;
        }
        set
        {
            GetInternalData<Data>().triggeredThisTurn = value;
        }
    }

    protected override object? InitInternalData()
    {
        return new Data() { triggeredThisTurn = false };
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        RefreshVfx(animateAppearance: true);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        RemoveVfx(oldOwner);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RemoveVfx(Owner);
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult _, ValueProp props, Creature? dealer, CardModel? __)
    {
        if (target == Owner && !TriggeredThisTurn)
        {
            Flash();
            TriggeredThisTurn = true;
            GetVfx()?.Consume();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            if (!TriggeredThisTurn)
            {
                Flash();
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, DynamicVars.Strength.BaseValue, Owner, null);
            }
            TriggeredThisTurn = false;
            RefreshVfx(animateAppearance: true);
        }
    }

    public void AddStrenthPowerAmount(decimal amount)
    {
        DynamicVars.Strength.BaseValue += amount;
        InvokeDisplayAmountChanged();
        RefreshVfx();
    }

    private NShiTanPowerVfx? GetVfx()
    {
        return NCombatRoom.Instance?
            .GetCreatureNode(Owner)?
            .Visuals.Bounds
            .GetNodeOrNull<NShiTanPowerVfx>(VfxNodeName);
    }

    private void RefreshVfx(bool animateAppearance = false)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        var creatureVisuals = creatureNode?.Visuals;
        var creatureBounds = creatureVisuals?.Bounds;
        if (creatureNode == null || creatureVisuals == null || creatureBounds == null)
        {
            return;
        }

        NShiTanPowerVfx? vfx =
            creatureBounds.GetNodeOrNull<NShiTanPowerVfx>(VfxNodeName);
        if (vfx == null && !TriggeredThisTurn)
        {
            vfx = VFXUtil.GenVFXNode<NShiTanPowerVfx>(NShiTanPowerVfx.ScenePath);
            vfx.Name = VfxNodeName;
            creatureBounds.AddChildSafely(vfx);
        }

        if (vfx == null)
        {
            return;
        }

        Node2D? idleFollowAnchor = creatureVisuals
            .GetCurrentBody()
            .GetNodeOrNull<Node2D>("ShiTanIdleFollowAnchor");
        vfx.Configure(creatureBounds.Size, creatureNode, idleFollowAnchor);
        vfx.SetAvailable(!TriggeredThisTurn, animateAppearance);
    }

    private static void RemoveVfx(Creature owner)
    {
        var creatureBounds = NCombatRoom.Instance?.GetCreatureNode(owner)?.Visuals.Bounds;
        creatureBounds?.GetNodeOrNull<NShiTanPowerVfx>(VfxNodeName)?.QueueFreeSafely();
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        return
        [
            ExtraIconAmountLabelSpec.Plain(ExtraIconAmountLabelCorner.TopRight, DynamicVars.Strength.BaseValue.ToString())
        ];
    }
}
