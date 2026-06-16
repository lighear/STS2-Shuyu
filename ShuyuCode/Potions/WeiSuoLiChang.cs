using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Potions;

// 注册药水。如果要写自定义池看添加人物的开头
[RegisterPotion(typeof(ShuyuPotionPool))]
public class WeiSuoLiChang : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<IceShieldPower>(10),
        new PowerVar<IceThornsPower>(10)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<IceShieldPower>(),
        HoverTipFactory.FromPower<IceThornsPower>()
    ];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"{Entry.ResPath}/images/potions/{GetType().Name}.png",
        OutlinePath: $"{Entry.ResPath}/images/potions/{GetType().Name}.png"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target != null)
        {
            await PowerCmd.Apply<IceShieldPower>(choiceContext, target, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, null);
            await PowerCmd.Apply<IceThornsPower>(choiceContext, target, DynamicVars["IceThornsPower"].BaseValue, Owner.Creature, null);
        }
    }
}