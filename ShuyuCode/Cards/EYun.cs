using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(StatusCardPool))]
    public class EYun : ModCardTemplate
    {
        public EYun() : base(
            baseCost: 2,
            CardType.Status,
            CardRarity.Status,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override int MaxUpgradeLevel => 0;
        public override bool HasTurnEndInHandEffect => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new HpLossVar(6)
        ];

        protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
        {
            await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        }

        public override void AfterCreated()
        {
            base.AfterCreated();
        }
    }
}
