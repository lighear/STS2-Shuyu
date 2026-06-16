using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class ZhaRenXianSheng : ModCardTemplate, IFrostforged
    {
        public ZhaRenXianSheng() : base(
            baseCost: 3,
            CardType.Power,
            CardRarity.Uncommon,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceShieldPower>(),
            HoverTipFactory.FromPower<IceThornsPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Retain,
            ShuyuKeywords.Frostforged
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceShieldPower>(8),
            new PowerVar<IceThornsPower>(8),
            new PowerVar<IceShieldPower>("ExtraIceShieldPower", 4),
            new PowerVar<IceThornsPower>("ExtraIceThornsPower", 4)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, DynamicVars["IceThornsPower"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["IceShieldPower"].UpgradeValueBy(2);
            DynamicVars["IceThornsPower"].UpgradeValueBy(2);
            DynamicVars["ExtraIceShieldPower"].UpgradeValueBy(1);
            DynamicVars["ExtraIceThornsPower"].UpgradeValueBy(1);
        }

        private decimal ExtraIceShiledFromFrozen;
        private decimal ExtraIceThornsFromFrozen;

        public async Task FrostforgedEffect()
        {
            decimal extraIceShield = DynamicVars["ExtraIceShieldPower"].BaseValue;
            decimal extraIceThorns = DynamicVars["ExtraIceThornsPower"].BaseValue;
            DynamicVars["IceShieldPower"].BaseValue += extraIceShield;
            DynamicVars["IceThornsPower"].BaseValue += extraIceThorns;
            ExtraIceShiledFromFrozen += extraIceShield;
            ExtraIceThornsFromFrozen += extraIceThorns;
        }

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars["IceShieldPower"].BaseValue += ExtraIceShiledFromFrozen;
            DynamicVars["IceThornsPower"].BaseValue += ExtraIceThornsFromFrozen;
        }
    }
}