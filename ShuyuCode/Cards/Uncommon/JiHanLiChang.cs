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
            new BlockVar(3, ValueProp.Move),
            new DynamicVar("IceShieldPower", 3)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int count = ResolveEnergyXValue();
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(DynamicVars.Block.BaseValue * count, ValueProp.Move), cardPlay);
            await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue * count, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(1);
            DynamicVars["IceShieldPower"].UpgradeValueBy(1);
        }
    }
}