using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    public class WanNengShuShi : ModCardTemplate
    {
        public WanNengShuShi() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Uncommon,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            ..HoverTipFactory.FromAffliction<Frozen>(),
            HoverTipFactory.FromPower<ChillPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(4, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);

            IEnumerable<CardModel> cards =
                await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: Owner,
                    prefs: new CardSelectorPrefs(new LocString("card_selection", "CHANGE_FROZEN_STATE"), 1),
                    filter: null,
                    source: this);
            foreach (CardModel card in cards)
            {
                if (card is FrozenCardModel frozenCard)
                {
                    await ShuyuMechanismCmd.UnfreezeCard(frozenCard);
                }
                else
                {
                    await ShuyuMechanismCmd.FreezeCard(card);
                    await PowerCmd.Apply<ChillPower>(choiceContext, cardPlay.Target!, 1, Owner.Creature, this);
                }
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3);
        }
    }
}