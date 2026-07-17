using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Helpers;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Shuyu.Characters;

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
        var creatureBounds = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals.Bounds;
        if (creatureBounds != null && creatureBounds.GetNodeOrNull<ColorRect>("VfxBingWuPower") == null)
        {
            string scenePath = $"{VFXUtil.PowerVfxPath}/vfx_BingWuPower.tscn";
            ColorRect vfxBingWuPower = VFXUtil.GenVFXNode<ColorRect>(scenePath);
            creatureBounds.AddChildSafely(vfxBingWuPower);

            if (Owner.Player?.Character is ShuyuCharacter)
            {
                vfxBingWuPower.Size = new Vector2(1924 * 0.194f, 2378 * 0.194f);
                vfxBingWuPower.Position = new Vector2(-962 * 0.194f + 123, -1189 * 0.194f);
            }
            else
            {
                vfxBingWuPower.AnchorLeft = 0;
                vfxBingWuPower.AnchorTop = 0;
                vfxBingWuPower.AnchorRight = 1;
                vfxBingWuPower.AnchorBottom = 1;
                Vector2 expandSize = creatureBounds.Size * 0.2f;
                vfxBingWuPower.OffsetLeft = -expandSize.X;
                vfxBingWuPower.OffsetTop = -expandSize.Y;
                vfxBingWuPower.OffsetRight = expandSize.X;
                vfxBingWuPower.OffsetBottom = expandSize.Y;
            }
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        var creatureBounds = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals.Bounds;
        creatureBounds?.GetNodeOrNull<ColorRect>("VfxBingWuPower")?.QueueFree();
    }
}