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
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class YuJiaXue : ModCardTemplate, IOnFreezingCard
    {
        public YuJiaXue() : base(
            baseCost: 5,
            CardType.Power,
            CardRarity.Rare,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>(),
            ..HoverTipFactory.FromAffliction<Frozen>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromKeyword(CardKeyword.Retain)
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new BlockVar(3, ValueProp.Unpowered),
            new EnergyVar(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);

            await PowerCmd.Apply<YuJiaXuePower>(choiceContext, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            base.EnergyCost.UpgradeBy(-1);
        }

        public async Task<bool> OnFreezingCard(PlayerChoiceContext choiceContext, CardModel card)
        {
            if (card == this)
            {
                await FreezingEffect(choiceContext);
                base.EnergyCost.AddThisCombat(-base.DynamicVars.Energy.IntValue);
            }
            return !(card == this);
        }
        
        private async Task FreezingEffect(PlayerChoiceContext choiceContext)
        {
            NYuJiaXueVfx.Create();
            //await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<ChillPower>(choiceContext, CombatState!.HittableEnemies, 1, Owner.Creature, this);
        }
    }
}
