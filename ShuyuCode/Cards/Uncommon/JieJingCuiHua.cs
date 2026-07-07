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
            HoverTipFactory.FromPower<IceThornsPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DynamicVar("Multiple", 2),
            new PowerVar<IceThornsPower>(4),
            new CalculationBaseVar(0),
            new ExtraDamageVar(1),
            new CalculatedDamageVar(ValueProp.Move | ValueProp.Unpowered).WithMultiplier((card, _) =>
            {
                return card.Owner.Creature.GetPowerAmount<IceThornsPower>() 
                    * (card.DynamicVars["Multiple"].IntValue - 1)
                    / card.DynamicVars["IceThornsPower"].IntValue;
            })
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int amount = Owner.Creature.GetPowerAmount<IceThornsPower>() * (DynamicVars["Multiple"].IntValue - 1);
            await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
            int damage = amount / DynamicVars["IceThornsPower"].IntValue;
            if (damage > 0)
            {
                await CreatureCmd.Damage(choiceContext, Owner.Creature, damage, ValueProp.Move | ValueProp.Unpowered, this, cardPlay);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars["Multiple"].UpgradeValueBy(1);
            DynamicVars["IceThornsPower"].UpgradeValueBy(4);
        }
    }
}