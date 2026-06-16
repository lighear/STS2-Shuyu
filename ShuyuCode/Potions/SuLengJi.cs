using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Potions;

// 注册药水。如果要写自定义池看添加人物的开头
[RegisterPotion(typeof(ShuyuPotionPool))]
public class SuLengJi : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [..HoverTipFactory.FromAffliction<Frozen>()];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"{Entry.ResPath}/images/potions/{GetType().Name}.png",
        OutlinePath: $"{Entry.ResPath}/images/potions/{GetType().Name}.png"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await ShuyuMechanismCmd.ChooseFromHandAndFreeze(choiceContext, Owner, 10, this, optional: true);
    }
}