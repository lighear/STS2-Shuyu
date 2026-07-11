using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class BingWuPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        if (CombatState.CurrentSide == Owner.Side)
        {
            return 1;
        }

        if (target == Owner && props.IsPoweredAttack() && amount >= 1)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    public override async Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Flash();
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner.Side != side)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Node2D? body = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals?.GetCurrentBody();
        if (body != null && body.GetNode<ColorRect>("VfxBingWuPower") == null)
        {
            string scenePath = $"res://Shuyu/scenes/vfx_BingWuPower.tscn";
            ColorRect vfxBingWuPower;
            if (VFXUtil.ModSceneCache.TryGetValue(scenePath, out var modScene))
            {
                vfxBingWuPower = modScene.Instantiate<ColorRect>();
            }
            else
            {
                vfxBingWuPower = PreloadManager.Cache.GetScene(scenePath).Instantiate<ColorRect>();
            }
            body.AddChild(vfxBingWuPower);
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        Node2D? body = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals?.GetCurrentBody();
        body?.GetNode<ColorRect>("VfxBingWuPower")?.QueueFree();
    }
}