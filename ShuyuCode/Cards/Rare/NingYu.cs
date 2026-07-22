using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
    public class NingYu : ModCardTemplate, IFrostforged
    {
        public NingYu() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.AnyEnemy)
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
            new PowerVar<FragilePower>(1),
            new RepeatVar(2),
            new DynamicVar("Multiple", 6)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await NNingYuFragileSlashVfx.Play(cardPlay.Target!);
            for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
            {
                await PowerCmd.Apply<FragilePower>(choiceContext, cardPlay.Target!, 1, Owner.Creature, this);
            }
            if (cardPlay.Target!.IsAlive)
            {
                decimal damage = cardPlay.Target!.Powers.Count(p => p.TypeForCurrentAmount == PowerType.Debuff) * DynamicVars["Multiple"].BaseValue;
                await Cmd.Wait(0.15f);
                await NNingYuDebuffImpactVfx.Play(cardPlay.Target);
                await CreatureCmd.Damage(choiceContext, cardPlay.Target, damage, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner.Creature, this, cardPlay);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Repeat.UpgradeValueBy(1);
            DynamicVars["Multiple"].UpgradeValueBy(3);
        }

        private decimal ExtraRepeatFromFrozen;

        public async Task FrostforgedEffect()
        {
            DynamicVars.Repeat.BaseValue++;
            ExtraRepeatFromFrozen++;
        }

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars.Repeat.BaseValue += ExtraRepeatFromFrozen;
        }
    }
}
