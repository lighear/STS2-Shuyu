using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JueWangYongChang : ModCardTemplate
    {
        public JueWangYongChang() : base(
            baseCost: 2,
            CardType.Attack,
            CardRarity.Uncommon,
            TargetType.AllEnemies)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            ..HoverTipFactory.FromAffliction<Frozen>(),
            HoverTipFactory.FromPower<ChillPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(12),
            new ExtraDamageVar(3),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier((_, _) => PileType.Hand.GetPile(Owner).Cards.Count(c => !c.IsFrozen()))
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this)
                .TargetingAllOpponents(CombatState!)
                .Execute(choiceContext);
            await PowerCmd.Apply<ChillPower>(choiceContext, CombatState!.HittableEnemies, 1, Owner.Creature, this);

            List<CardModel> cards = PileType.Hand.GetPile(Owner).Cards.Where(c => !c.IsFrozen()).ToList();
            foreach (CardModel card in cards)
            {
                await ShuyuMechanismCmd.FreezeCard(card);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.CalculationBase.UpgradeValueBy(3);
            DynamicVars.ExtraDamage.UpgradeValueBy(1);
        }
    }
}