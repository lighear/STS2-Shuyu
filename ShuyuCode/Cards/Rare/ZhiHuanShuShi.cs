using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
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
    public class ZhiHuanShuShi : ModCardTemplate
    {
        public ZhiHuanShuShi() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<FragilePower>(),
            ..HoverTipFactory.FromAffliction<Frozen>()
        ];
        
        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new EnergyVar(2),
            new CardsVar(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ZhiHuanShuShiPower? power = await PowerCmd.Apply<ZhiHuanShuShiPower>(choiceContext, cardPlay.Target!, DynamicVars.Energy.IntValue, Owner.Creature, this);
            power?.AddExtraCards(DynamicVars.Cards.IntValue);
            power?.AddExtraEnergy(DynamicVars.Energy.IntValue);

            CardSelectorPrefs prefs =
                new CardSelectorPrefs(new LocString("card_selection", "TO_FREEZE_OPTIONAL"), 0, 5);
            prefs.ShouldGlowGold = card => card.IsFrostforged();
            
            IEnumerable<CardModel> cards =
                await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: Owner,
                    prefs: prefs,
                    filter: c => !c.IsFrozen(),
                    source: this);
            
            int cardCount = cards.Count();
            
            foreach (CardModel card in cards)
            {
                await ShuyuMechanismCmd.FreezeCard(card);
            }
            
            await PowerCmd.Apply<FragilePower>(choiceContext, cardPlay.Target!, cardCount, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Energy.UpgradeValueBy(1);
            DynamicVars.Cards.UpgradeValueBy(1);
        }
    }
}