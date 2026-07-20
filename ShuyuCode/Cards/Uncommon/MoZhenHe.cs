using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class MoZhenHe : ModCardTemplate, IFrostforged
    {
        public MoZhenHe() : base(
            baseCost: 2,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>(),
            HoverTipFactory.FromCard<BingZhen>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Retain,
            ShuyuKeywords.Frostforged
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CardsVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await BingZhen.CreateInHand(Owner, DynamicVars.Cards.IntValue, CombatState!, false);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1);
        }

        private decimal ExtraNeedlesFromFrozen;

        public async Task FrostforgedEffect()
        {
            DynamicVars.Cards.BaseValue += 1;
            ExtraNeedlesFromFrozen += 1;
        }

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars.Cards.BaseValue += ExtraNeedlesFromFrozen;
        }
    }
}