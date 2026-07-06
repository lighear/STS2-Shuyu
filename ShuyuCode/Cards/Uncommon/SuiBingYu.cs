using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class SuiBingYu : ModCardTemplate
    {
        public SuiBingYu() : base(
            baseCost: 1,
            CardType.Attack,
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
            //new PowerVar<IceThornsPower>(5),
            new CalculationBaseVar(0m),
            new ExtraDamageVar(1m),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => card.Owner.Creature.GetPowerAmount<IceThornsPower>()),
            new RepeatVar(4)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            //await PowerCmd.Apply<IceThornsPower>(choiceContext, Owner.Creature, DynamicVars["IceThornsPower"].BaseValue, Owner.Creature, this);
            await DamageCmd.Attack(base.DynamicVars.CalculatedDamage)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(CombatState!)
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            //DynamicVars.Repeat.UpgradeValueBy(1);
            RemoveKeyword(CardKeyword.Exhaust);
        }
    }
}