using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.DynamicVars;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JiHanLiChang : ModCardTemplate
    {
        public JiHanLiChang() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.Self)
        { }

        protected override bool HasEnergyCostX => true;
        public override bool GainsBlock => true;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(0),
            new CalculationExtraVar(4),
            new CurrentEnergyXBlockVar(ValueProp.Move),
            new PowerVar<IceShieldPower>(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int count = ResolveEnergyXValue();
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(Owner.Creature), ValueProp.Move, cardPlay);
            await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue * count, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.CalculationExtra.UpgradeValueBy(2);
            //DynamicVars["IceShieldPower"].UpgradeValueBy(2);
        }
    }
}
