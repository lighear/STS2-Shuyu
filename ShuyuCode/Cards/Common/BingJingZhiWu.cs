using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    public class BingJingZhiWu : ModCardTemplate
    {
        public BingJingZhiWu() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Common,
            TargetType.Self)
        { }

        public override bool GainsBlock => true;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromCard<BingZhen>(base.IsUpgraded)   
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new BlockVar(3, ValueProp.Move),
            new CardsVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await BingZhen.CreateInHand(Owner, DynamicVars.Cards.IntValue, CombatState!, base.IsUpgraded);
            if (base.IsUpgraded)
            {
                await PowerCmd.Apply<BingJingZhiWuPlusPower>(choiceContext, Owner.Creature, DynamicVars.Cards.IntValue, Owner.Creature, this);
            }
            else
            {
                await PowerCmd.Apply<BingJingZhiWuPower>(choiceContext, Owner.Creature, DynamicVars.Cards.IntValue, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(2);
        }
    }
}