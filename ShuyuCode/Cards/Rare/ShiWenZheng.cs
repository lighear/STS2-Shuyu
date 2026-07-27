using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class ShiWenZheng : ModCardTemplate
    {
        public ShiWenZheng() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.AllEnemies)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DynamicVar("EnemyStrengthLoss", 2),
            new DynamicVar("ExtraEnemyStrengthLoss", 1),
            new RepeatVar(3)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);
            foreach (Creature enemy in CombatState!.HittableEnemies)
            {
                await PowerCmd.Apply<StrengthPower>(choiceContext,enemy, -DynamicVars["EnemyStrengthLoss"].BaseValue, Owner.Creature, this);
                ShiWenZhengPower? power = await PowerCmd.Apply<ShiWenZhengPower>(choiceContext, enemy, DynamicVars["ExtraEnemyStrengthLoss"].BaseValue, Owner.Creature, this);
                power?.SetMaxStrengthLossCount(DynamicVars.Repeat.IntValue);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars["EnemyStrengthLoss"].UpgradeValueBy(1);
        }
    }
}