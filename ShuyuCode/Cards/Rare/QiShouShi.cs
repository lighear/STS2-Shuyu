using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Commands;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class QiShouShi : ModCardTemplate
    {
        public QiShouShi() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            //HoverTipFactory.FromPower<IceShieldPower>(),
            ..HoverTipFactory.FromAffliction<Frozen>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            //new PowerVar<IceShieldPower>(3),
            new EnergyVar(1),
            new CardsVar(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            //await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            await ShuyuMechanismCmd.ChooseFromHandAndFreeze(choiceContext, Owner, 1, this);
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Innate);
        }
    }
}