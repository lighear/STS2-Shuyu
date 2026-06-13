using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class BingShiZi : ModCardTemplate
    {
        public BingShiZi() : base(
            baseCost: 3,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy)
        { }

        public override bool GainsBlock => true;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<FragilePower>(),
            HoverTipFactory.FromCard<BingZhen>(base.IsUpgraded)
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(11, ValueProp.Move),
            new BlockVar(11, ValueProp.Move),
            new PowerVar<WeakPower>(1),
            new PowerVar<FragilePower>(1),
            new CardsVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<FragilePower>(choiceContext, cardPlay.Target!, DynamicVars["FragilePower"].BaseValue, Owner.Creature, this);
            await BingZhen.CreateInHand(Owner, DynamicVars.Cards.IntValue, CombatState!, base.IsUpgraded);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4);
            DynamicVars.Block.UpgradeValueBy(4);
        }
    }
}