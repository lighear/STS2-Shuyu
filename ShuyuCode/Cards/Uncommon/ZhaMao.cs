using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class ZhaMao : ModCardTemplate
    {
        public ZhaMao() : base(
            baseCost: 1,
            CardType.Power,
            CardRarity.Uncommon,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceThornsPower>(),
            HoverTipFactory.FromPower<FragilePower>(),
            HoverTipFactory.FromPower<VulnerablePower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<FragilePower>(1),
            new PowerVar<ZhaMaoPower>(25)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);
            ZhaMaoPower? power = await PowerCmd.Apply<ZhaMaoPower>(choiceContext, Owner.Creature, DynamicVars["ZhaMaoPower"].BaseValue, Owner.Creature, this);
            power?.AddFragilePowerAmount(DynamicVars["FragilePower"].BaseValue);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["ZhaMaoPower"].UpgradeValueBy(25);
        }
    }
}