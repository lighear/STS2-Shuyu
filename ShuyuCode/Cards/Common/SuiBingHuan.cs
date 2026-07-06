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
    public class SuiBingHuan : ModCardTemplate
    {
        public SuiBingHuan() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Common,
            TargetType.Self)
        { }

        public override bool GainsBlock => true;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceThornsPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new BlockVar(7, ValueProp.Move),
            new PowerVar<IceThornsPower>(5)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, DynamicVars["IceThornsPower"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(2);
            DynamicVars["IceThornsPower"].UpgradeValueBy(2);
        }
    }
}