using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class XunHuanShuShi : ModCardTemplate
    {
        public XunHuanShuShi() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromKeyword(CardKeyword.Retain)
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CardsVar(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var cards = PileType.Hand.GetPile(Owner).Cards;
            int drawCount = cards.Count(c => c.Keywords.Contains(CardKeyword.Retain)) + DynamicVars.Cards.IntValue;
            await CardCmd.Discard(choiceContext, cards);
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1);
        }
    }
}