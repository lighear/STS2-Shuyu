using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class BingChaDaPao : ModCardTemplate, IFrostforged
    {
        public BingChaDaPao() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Uncommon,
            TargetType.AllEnemies)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<FragilePower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Retain,
            ShuyuKeywords.Frostforged
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(4, ValueProp.Move),
            new PowerVar<FragilePower>(1),
            new RepeatVar(2),
            new DynamicVar("ExtraDamage", 2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int repeatCount = 1;
            if (CombatState!.HittableEnemies.Count == 1)
            {
                repeatCount += DynamicVars.Repeat.IntValue;
            }

            for (int i = 0; i < repeatCount; i++)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .TargetingAllOpponents(CombatState)
                    .Execute(choiceContext);

                await PowerCmd.Apply<FragilePower>(choiceContext, CombatState.HittableEnemies, DynamicVars["FragilePower"].BaseValue, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2);
        }

        private decimal ExtraDamageFromFrozen;

        public async Task FrostforgedEffect()
        {
            decimal extraDamage = DynamicVars["ExtraDamage"].BaseValue;
            DynamicVars.Damage.BaseValue += extraDamage;
            ExtraDamageFromFrozen += extraDamage;
        }

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars.Damage.BaseValue += ExtraDamageFromFrozen;
        }
    }
}