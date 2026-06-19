using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    public class MoLiXianDan : ModCardTemplate
    {
        public MoLiXianDan() : base(
            baseCost: 3,
            CardType.Attack,
            CardRarity.Rare,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<FragilePower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(0),
            new ExtraDamageVar(3),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                (card, _) => CombatManager.Instance.History.Entries
                    .OfType<PowerReceivedEntry>()
                    .Where(entry => entry.Power is FragilePower && entry.Applier == card.Owner.Creature)
                    .Sum(entry => entry.Amount))
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            /*decimal damage = CombatManager.Instance.History.Entries
                .OfType<PowerReceivedEntry>()
                .Where(entry => entry.Power is FragilePower && entry.Applier == Owner.Creature)
                .Sum(entry => entry.Amount)
                * DynamicVars["Multiple"].BaseValue;*/
            AttackCommand attackCommand = await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);

            DamageResult? damageResult = attackCommand.Results.FirstOrDefault()?.FirstOrDefault();
            if (damageResult != null)
            {
                await DamageCmd.Attack(damageResult.TotalDamage + damageResult.OverkillDamage)
                    .FromCard(this)
                    .TargetingAllOpponents(CombatState!)
                    .Execute(choiceContext);
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}