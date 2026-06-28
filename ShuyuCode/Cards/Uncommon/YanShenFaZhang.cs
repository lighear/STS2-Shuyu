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
    public class YanShenFaZhang : ModCardTemplate
    {
        public YanShenFaZhang() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Uncommon,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(8),
            new ExtraDamageVar(1),
            new CardsVar(9),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                (card, _) => PileType.Hand.GetPile(card.Owner).Cards.Count(c => c != card))
        ];
        
        protected override bool ShouldGlowGoldInternal => ShouldDouble();
        
        private bool ShouldDouble()
        {
            return PileType.Hand.GetPile(Owner).Cards.Count(c => c != this) >= DynamicVars.Cards.BaseValue;
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .WithHitCount(ShouldDouble()?2:1)
                .FromCard(this)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.ExtraDamage.UpgradeValueBy(1);
        }
    }
}