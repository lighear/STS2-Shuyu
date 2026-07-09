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
    //[RegisterCard(typeof(ShuyuCardPool))]
    public class BengLie : ModCardTemplate
    {
        public BengLie() : base(
            baseCost: 1,
            CardType.Power,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<FragilePower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(16, ValueProp.Unpowered),
            new ExtraDamageVar(8),
            new PowerVar<FragilePower>(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            BengLiePower? power = await PowerCmd.Apply<BengLiePower>(choiceContext, Owner.Creature, DynamicVars.Damage.BaseValue, Owner.Creature, this);
            power?.AddExtraDamage(DynamicVars.ExtraDamage.BaseValue);
            power?.AddFragilePowerAmount(DynamicVars["FragilePower"].BaseValue);

            await PowerCmd.Apply<FragilePower>(choiceContext, CombatState!.HittableEnemies, 2, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(8);
            DynamicVars.ExtraDamage.UpgradeValueBy(4);
            DynamicVars["FragilePower"].UpgradeValueBy(1);
        }
    }
}