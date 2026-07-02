using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Afflictions;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Enchantments;

[RegisterEnchantment]
public class CantFreeze : ModEnchantmentTemplate, IOnFreezingCard
{
    public override bool ShowAmount => false;

    // 重载这个以改变显示的数字
    // public override int DisplayAmount => DynamicVars.Cards.IntValue;

    // 是否会添加额外的卡牌描述文本
    public override bool HasExtraCardText => true;
    

    // 像卡牌、遗物、药水等一样，可以使用DynamicVars和ExtraHoverTips
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..HoverTipFactory.FromAffliction<Frozen>()];

    // 图标位置。大小1:1就行，原版是64x64
    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/enchantments/{GetType().Name}.png"
    );

    // 决定是否可以附魔到某张卡牌上，这里我们让它只能附魔到获得格挡的卡牌上。
    public override bool CanEnchant(CardModel card)
    {
        if (base.CanEnchant(card))
        {
            return true;
        }
        return false;
    }

    public async Task<bool> OnFreezingCard(CardModel card)
    {
        return !(card == Card);
    }
}