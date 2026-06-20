using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Powers;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class DuanXue : ModCardTemplate, IFrostforged
    {
        public DuanXue() : base(
            baseCost: 2,
            CardType.Attack,
            CardRarity.Rare,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Retain,
            ShuyuKeywords.Frostforged
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(6, ValueProp.Move),
            new RepeatVar(2),
            new DynamicVar("ExtraDamage", 3)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int hitCount = DynamicVars.Repeat.IntValue;
            AttackCommand attackCommand = await DamageCmd
                .Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(hitCount)
                .WithAttackerFx(() => NDuanXueVfx.Create(cardPlay.Target!, hitCount))
                .OnlyPlayAnimOnce()
                .AfterAttackerAnim(async () => await Cmd.Wait(0.15f * hitCount + 0.6f))
                .FromCard(this)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);

            int remainHitCount = hitCount - attackCommand.Results.Count();
            if (remainHitCount > 0)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .WithHitCount(remainHitCount)
                    .FromCard(this)
                    .TargetingRandomOpponents(CombatState!)
                    .Execute(choiceContext);
            }

            DynamicVars.Repeat.BaseValue = 2;
        }

        public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
        {
            if (card == this)
            {
                DynamicVars.Repeat.BaseValue++;
                await CardPileCmd.Add(this, PileType.Hand);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2);
            DynamicVars["ExtraDamage"].UpgradeValueBy(1);
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