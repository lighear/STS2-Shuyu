using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class NiZhuanJieJie : ModCardTemplate
    {
        public NiZhuanJieJie() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceThornsPower>(),
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust,
            CardKeyword.Retain
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceThornsPower>(2),
            new PowerVar<IceShieldPower>(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            IceThornsPower? iceThornsPower = await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, DynamicVars["IceThornsPower"].BaseValue, Owner.Creature, this);
            IceShieldPower? iceShieldPower = await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
            int iceThornsPowerAmount = iceThornsPower?.Amount ?? 0;
            int iceShieldPowerAmount = iceShieldPower?.Amount ?? 0;

            if (iceThornsPowerAmount < iceShieldPowerAmount)
            {
                await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature,iceShieldPowerAmount - iceThornsPowerAmount, Owner.Creature, this);
                iceShieldPower?.SetAmount(iceThornsPowerAmount);
            }
            
            else if (iceThornsPowerAmount > iceShieldPowerAmount)
            {
                await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature,iceThornsPowerAmount - iceShieldPowerAmount, Owner.Creature, this);
                iceThornsPower?.SetAmount(iceShieldPowerAmount);
            }
            
            
        }

        protected override void OnUpgrade()
        {
            DynamicVars["IceThornsPower"].UpgradeValueBy(2);
            DynamicVars["IceShieldPower"].UpgradeValueBy(2);
        }
    }
}