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
    public class BingYue : ModCardTemplate
    {
        public BingYue() : base(
            baseCost: 2,
            CardType.Power,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceShieldPower>(1),
            new PowerVar<IceShieldPower>("ExtraIceShieldPower", 5)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);
            await PowerCmd.Apply<BingYuePower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["ExtraIceShieldPower"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            //DynamicVars["IceShieldPower"].UpgradeValueBy(1);
            DynamicVars["ExtraIceShieldPower"].UpgradeValueBy(2);
        }
    }
}