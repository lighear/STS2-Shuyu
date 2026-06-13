using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    public class ChuXueShengJie : ModCardTemplate
    {
        public ChuXueShengJie() : base(
            baseCost: 3,
            CardType.Power,
            CardRarity.Uncommon,
            TargetType.AllAllies)
        { }

        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceShieldPower>(8),
            new HealVar(3)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            IEnumerable<Creature> enumerable = CombatState!.GetTeammatesOf(Owner.Creature).Where(c => c.IsAlive && c.IsPlayer);
            foreach (Creature creature in enumerable)
            {
                await PowerCmd.Apply<IceShieldPower>(choiceContext, creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, this);
                await PowerCmd.Apply<ChuXueShengJiePower>(choiceContext, creature, DynamicVars.Heal.BaseValue, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars["IceShieldPower"].UpgradeValueBy(2);
            DynamicVars.Heal.UpgradeValueBy(2);
        }
    }
}