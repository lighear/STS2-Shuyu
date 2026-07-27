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
    public class CheGu : ModCardTemplate
    {
        public CheGu() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ArtifactPower>(),
            HoverTipFactory.FromPower<ChillPower>()
        ];
        
        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CardsVar(1)
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Attack",
                Owner.Character.CastAnimDelay);
            if (cardPlay.Target!.HasPower<ArtifactPower>())
            {
                await PowerCmd.Remove<ArtifactPower>(cardPlay.Target);
                await PowerCmd.Apply<ChillPower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
            }
            await PowerCmd.Apply<ChillPower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
            if (base.IsUpgraded)
            {
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
            }
        }

        protected override void OnUpgrade()
        {
            //EnergyCost.UpgradeBy(-1);
        }
    }
}