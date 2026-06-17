using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Characters;
using Shuyu.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class BingQiMengJiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(ShuyuKeywords.Frostforged)
    ];

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner.Creature == Owner
            && card.IsFrostforged()
            && (card.Pile?.Type == PileType.Hand || card.Pile?.Type == PileType.Play))
        {
            modifiedCost = 0;
            return true;
        }
        else
        {
            modifiedCost = originalCost;
            return false;
        }
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (card.Owner.Creature == Owner
            && card.IsFrostforged()
            && (card.Pile?.Type == PileType.Hand || card.Pile?.Type == PileType.Play))
        {
            await PowerCmd.Decrement(this);
        }
    }
}