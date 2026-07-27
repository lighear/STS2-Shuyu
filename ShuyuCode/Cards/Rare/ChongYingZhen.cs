using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class ChongYingZhen : ModCardTemplate
    {
        public ChongYingZhen() : base(
            baseCost: 2,
            CardType.Power,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips
        {
            get
            {
                if (IsUpgraded)
                {
                    return [HoverTipFactory.FromCard<BingZhen>(),
                        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
                        HoverTipFactory.FromPower<StrengthPower>(),
                        HoverTipFactory.FromPower<ChillPower>(),];
                }
                else
                {
                    return [HoverTipFactory.FromCard<BingZhen>(),
                        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
                        HoverTipFactory.FromPower<ChillPower>(),];
                }
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CardsVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);
            ChongYingZhenPower? power = await PowerCmd.Apply<ChongYingZhenPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
            if (IsUpgraded)
            {
                power?.AddStrenthPowerAmount(1);
            }
            await BingZhen.CreateInHand(Owner, DynamicVars.Cards.IntValue, CombatState!, false);
        }
    }
}