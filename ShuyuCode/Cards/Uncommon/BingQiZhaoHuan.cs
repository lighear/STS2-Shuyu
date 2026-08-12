using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
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
    public class BingQiZhaoHuan : ModCardTemplate
    {
        public BingQiZhaoHuan() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromKeyword(ShuyuKeywords.Frostforged),
            ..HoverTipFactory.FromAffliction<Frozen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CardsVar(1),
            new EnergyVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            IEnumerable<CardModel> cards = PileType.Draw.GetPile(Owner).Cards
                .Concat(PileType.Discard.GetPile(Owner).Cards)
                .Where(c => c.IsFrostforged())
                .TakeRandom(DynamicVars.Cards.IntValue, CombatState!.RunState.Rng.CombatCardSelection);
            foreach (CardModel card in cards)
            {
                await CardPileCmd.Add(card, PileType.Hand);
            }
            List<CardModel> cardsfrost = PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsFrostforged()).ToList();
            foreach (CardModel card in cardsfrost)
            {
                await ShuyuMechanismCmd.FreezeCard(choiceContext, card);
            }
            await PowerCmd.Apply<NextTurnEnergyPower>(choiceContext, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1);
        }
    }
}