using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
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
    public class HongNi : ModCardTemplate
    {
        public HongNi() : base(
            baseCost: 2,
            CardType.Attack,
            CardRarity.Rare,
            TargetType.AllEnemies)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(11, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState!)
                .Execute(choiceContext);

            foreach (Creature enemy in CombatState!.HittableEnemies)
            {
                List<PowerModel> debuffs = enemy.Powers.Where(p => p.TypeForCurrentAmount == PowerType.Debuff).ToList();
                foreach (PowerModel debuff in debuffs)
                {
                    await PowerCmd.Apply(choiceContext, debuff, enemy, debuff.Amount, Owner.Creature, this);
                }
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(5);
        }
    }
}