
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
    public class NingBingHuDun : ModCardTemplate
    {
        public NingBingHuDun() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Common,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceShieldPower>(4),
            new PowerVar<IceShieldPower>("ExtraIceShieldPower", 4)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            decimal amount = DynamicVars["IceShieldPower"].BaseValue;
            if (!Owner.Creature.HasPower<IceShieldPower>())
            {
                amount += DynamicVars["ExtraIceShieldPower"].BaseValue;
            }
            await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["IceShieldPower"].UpgradeValueBy(1);
            DynamicVars["ExtraIceShieldPower"].UpgradeValueBy(1);
        }
    }
}