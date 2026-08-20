using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JieJingCuiHua : ModCardTemplate
    {
        public JieJingCuiHua() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceThornsPower>(),
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            decimal amount = 0;

            if (this.IsUpgraded)
            {
                amount = Owner.Creature.GetPowerAmount<IceThornsPower>() * 0.75m;
            }
            else
            {
                amount = Owner.Creature.GetPowerAmount<IceThornsPower>() * 0.5m;
            }

            if (amount >= 1)
            {
                await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
                await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            
        }
    }
}