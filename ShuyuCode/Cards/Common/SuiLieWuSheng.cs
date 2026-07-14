using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Powers;
using Shuyu.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class SuiLieWuSheng : ModCardTemplate
    {
        public SuiLieWuSheng() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            ..HoverTipFactory.FromAffliction<Frozen>(),
            HoverTipFactory.FromKeyword(ShuyuKeywords.Targeted),
            HoverTipFactory.FromPower<FragilePower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(10, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);

            IEnumerable<CardModel> cards = (await CardSelectCmd.FromHandForDiscard(choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this));

            foreach (CardModel card in cards)
            {
                int amount = card.EnergyCost.GetAmountToSpend() * 2;
                if (card is FrozenCardModel frozenCard)
                {
                    CardModel? original = frozenCard._visualCardModel;
                
                    await frozenCard.SetIcyDamageTargets(cardPlay.Target!);
                    await CardCmd.Discard(choiceContext, frozenCard);
                
                    if (original != null) await CardPileCmd.Add(original, PileType.Hand);
                }
                else
                {
                    await CardCmd.Discard(choiceContext, card);
                    await CardPileCmd.Add(card, PileType.Hand);
                }
                
                if (base.IsUpgraded)
                {
                    foreach (Creature enemy in CombatState!.HittableEnemies)
                    {
                        await PowerCmd.Apply<FragilePower>(choiceContext, enemy, amount, Owner.Creature, this);
                    }
                }
                else
                {
                    await PowerCmd.Apply<FragilePower>(choiceContext, cardPlay.Target!, amount, Owner.Creature, this);
                }
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(1);
        }
    }
}