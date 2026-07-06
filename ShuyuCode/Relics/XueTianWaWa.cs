using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Cards;
using Shuyu.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Relics;

//[RegisterRelic(typeof(ShuyuRelicPool))]
public sealed class XueTianWaWa : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<EYun>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner == Owner && card.Tags.Contains(ShuyuCardTags.Taboo))
        {
            modifiedCost = originalCost - 1;
            return true;
        }
        else
        {
            modifiedCost = originalCost;
            return false;
        }
    }
}
