using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class YinZhiBiLei : ModCardTemplate
    {
        public YinZhiBiLei() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<IceThornsPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<YinZhiBiLeiPower>(1),
            new PowerVar<IceThornsPower>(7)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<YinZhiBiLeiPower>(choiceContext, Owner.Creature, DynamicVars["YinZhiBiLeiPower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, DynamicVars["IceThornsPower"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["YinZhiBiLeiPower"].UpgradeValueBy(1);
            //DynamicVars["IceThornsPower"].UpgradeValueBy(1);
        }
    }
}