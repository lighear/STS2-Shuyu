using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using Shuyu.Cards;
using Shuyu.Characters;
using Shuyu.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Relics;

[RegisterRelic(typeof(ShuyuRelicPool))]
public sealed class WeiXingXueRen : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relit/{GetType().Name}.png");
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ..HoverTipFactory.FromEnchantment<CantFreeze>()
    ];
    
    public override async Task AfterObtained()
    {
        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, base.DynamicVars.Cards.IntValue)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };
        CantFreeze canonicalEnchantment = ModelDb.Enchantment<CantFreeze>();
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(base.Owner, canonicalEnchantment, 1, prefs))
        {
            CardCmd.Enchant<CantFreeze>(item, 1);
            CardCmd.Preview(item);
        }
    }
    
}