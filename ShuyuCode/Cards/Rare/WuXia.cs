using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class WuXia : ModCardTemplate
    {
        public WuXia() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override bool GainsBlock => true;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            ..HoverTipFactory.FromAffliction<Frozen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new BlockVar(5, ValueProp.Move),
            new CardsVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

            List<CardModel> cards = PileType.Draw.GetPile(Owner).Cards.Where(c => c.Type == CardType.Status || c.Type == CardType.Curse).ToList();
            foreach (CardModel card in cards)
            {
                await ShuyuMechanismCmd.FreezeCard(card);
            }
            await CardPileCmd.Draw(choiceContext, cards.Count * DynamicVars.Cards.IntValue, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(2);
            DynamicVars.Cards.UpgradeValueBy(1);
        }
    }
}