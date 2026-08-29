using MegaCrit.Sts2.Core.CardSelection;
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
    public class QingShen : ModCardTemplate
    {
        public QingShen() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Common,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<DexterityPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<StrengthPower>(3),
            new PowerVar<DexterityPower>(3)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<QingShenStrengthUpPower>(choiceContext, Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<QingShenDexterityUpPower>(choiceContext, Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);
            await CardCmd.Discard(choiceContext, await CardSelectCmd.FromHandForDiscard(choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this));
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Strength.UpgradeValueBy(2);
            DynamicVars.Dexterity.UpgradeValueBy(2);
        }
    }
}