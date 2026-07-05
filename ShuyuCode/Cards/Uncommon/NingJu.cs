using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class NingJu : ModCardTemplate
    {
        public NingJu() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromKeyword(CardKeyword.Retain)
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(0),
            new CalculationExtraVar(1),
            new CalculatedVar("CalculatedEnergy").WithMultiplier((c, _) => PileType.Hand.GetPile(c.Owner).Cards.Count(c => c.Keywords.Contains(CardKeyword.Retain)))
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PlayerCmd.GainEnergy(((CalculatedVar)DynamicVars["CalculatedEnergy"]).Calculate(Owner.Creature), Owner);
            var cards = (await CardSelectCmd.FromHandForDiscard(choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), c => c.Keywords.Contains(CardKeyword.Retain), this));
            await CardCmd.Discard(choiceContext, cards);
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}