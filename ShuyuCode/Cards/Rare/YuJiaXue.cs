using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class YuJiaXue : ModCardTemplate, IOnFreezingCard, IAfterUnfreezingCard
    {
        public YuJiaXue() : base(
            baseCost: 2,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.AllEnemies)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>(),
            ..HoverTipFactory.FromAffliction<Frozen>(),
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceShieldPower>(3)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);
            await PowerCmd.Apply<ChillPower>(choiceContext, CombatState!.HittableEnemies, 1, Owner.Creature, this);
            await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["IceShieldPower"].UpgradeValueBy(2);
        }

        public async Task<bool> OnFreezingCard(CardModel card)
        {
            if (card == this)
            {
                await FreezingEffect(new ThrowingPlayerChoiceContext());
            }
            return true;
        }

        public async Task AfterUnfreezingCard(CardModel card)
        {
            if (card == this)
            {
                await FreezingEffect(new ThrowingPlayerChoiceContext());
            }
        }

        private async Task FreezingEffect(PlayerChoiceContext choiceContext)
        {
            //await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<ChillPower>(choiceContext, CombatState!.HittableEnemies, 1, Owner.Creature, this);
        }
    }
}