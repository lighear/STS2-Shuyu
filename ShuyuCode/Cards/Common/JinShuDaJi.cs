using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JinShuDaJi : ModCardTemplate
    {
        public JinShuDaJi() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>(),
            HoverTipFactory.FromCard<EYun>()
        ];

        protected override HashSet<CardTag> CanonicalTags => [
            CardTag.Strike,
            ShuyuCardTags.Taboo
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(8, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(2)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);

            await PowerCmd.Apply<ChillPower>(choiceContext, cardPlay.Target!, 1, Owner.Creature, this);

            await EYun.CreateInDrawPile(Owner, 1, CombatState!);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3);
        }
    }
}