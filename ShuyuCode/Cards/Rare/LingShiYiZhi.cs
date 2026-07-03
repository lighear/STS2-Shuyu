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
    public class LingShiYiZhi : ModCardTemplate
    {
        public LingShiYiZhi() : base(
            baseCost: 0,
            CardType.Attack,
            CardRarity.Rare,
            TargetType.AllEnemies)
        { }

        protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Count(c => c != this) >= 9;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromCard<BingZhen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(12, ValueProp.Move),
            new CardsVar(6)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState!)
                .Execute(choiceContext);

            await CardCmd.Discard(choiceContext, PileType.Hand.GetPile(Owner).Cards);
            await BingZhen.CreateInHand(Owner, DynamicVars.Cards.IntValue, CombatState!, false);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(3);
        }
    }
}