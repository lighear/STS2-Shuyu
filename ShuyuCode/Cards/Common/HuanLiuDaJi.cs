using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Linq.Expressions;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class HuanLiuDaJi : ModCardTemplate
    {
        public HuanLiuDaJi() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromKeyword(CardKeyword.Retain)
        ];

        protected override HashSet<CardTag> CanonicalTags => [
            CardTag.Strike
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(9, ValueProp.Move),
            new CalculationBaseVar(1),
            new CalculationExtraVar(1),
            new CalculatedVar("CalculatedHits").WithMultiplier(
                (card, _) => PileType.Hand.GetPile(card.Owner).Cards.Count(c => c != card && c.Keywords.Contains(CardKeyword.Retain)) / 2)
            
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount((int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(cardPlay.Target))
                .FromCard(this, cardPlay)
                .WithHitFx("vfx/vfx_starry_impact")
                .OnlyPlayAnimOnce()
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2);
        }
    }
}
