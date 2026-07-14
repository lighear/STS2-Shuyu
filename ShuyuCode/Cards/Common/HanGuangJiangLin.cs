using MegaCrit.Sts2.Core.CardSelection;
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
    public class HanGuangJiangLin : ModCardTemplate
    {
        public HanGuangJiangLin() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromCard<BingZhen>(IsUpgraded),
            HoverTipFactory.FromKeyword(ShuyuKeywords.Targeted),
            ..HoverTipFactory.FromAffliction<Frozen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(5, ValueProp.Move),
            new CardsVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitFx("vfx/vfx_starry_impact")
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);

            CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, DynamicVars.Cards.IntValue);
            IEnumerable<CardModel> cards = await CardSelectCmd.FromHandForDiscard(choiceContext, Owner, prefs, null, this);
            foreach (CardModel card in cards)
            {
                if (card is FrozenCardModel frozenCard)
                {
                    await frozenCard.SetIcyDamageTargets(cardPlay.Target!);
                }
            }
            await CardCmd.Discard(choiceContext, cards);

            await BingZhen.CreateInHand(Owner, DynamicVars.Cards.IntValue, CombatState!, upgrade: IsUpgraded);
        }   

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3);
        }
    }
}