using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class YinZhiShu : ModCardTemplate
    {
        public YinZhiShu() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Rare,
            TargetType.AllEnemies)
        { }

        protected override bool ShouldGlowGoldInternal => ShouldCreate();

        private bool ShouldCreate()
        {
            return PileType.Hand.GetPile(Owner).Cards.Count(c => c != this) >= DynamicVars.Cards.IntValue;
        }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>(),
            HoverTipFactory.FromCard<BingZhen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(4, ValueProp.Move),
            new CardsVar(6)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState!)
                .Execute(choiceContext);
            await PowerCmd.Apply<ChillPower>(choiceContext, CombatState!.HittableEnemies, 1, Owner.Creature, this);
            if (ShouldCreate())
            {
                await BingZhen.CreateInHand(Owner, 1, CombatState, false);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3);
            DynamicVars.Cards.UpgradeValueBy(-1);
        }
    }
}