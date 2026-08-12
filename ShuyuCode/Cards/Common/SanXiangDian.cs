using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class SanXiangDian : ModCardTemplate, IOnFreezingCard
    {
        public SanXiangDian() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Common,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            ..HoverTipFactory.FromAffliction<Frozen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CardsVar(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await ShuyuMechanismCmd.ChooseFromHandAndFreeze(choiceContext, Owner, DynamicVars.Cards.IntValue, null, optional: true);
            await ShuyuMechanismCmd.ChooseFromHandAndUnfreeze(choiceContext, Owner, DynamicVars.Cards.IntValue, this, optional: true);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1);
        }

        public Task FrostforgedEffect()
        {
            return Task.CompletedTask;
        }

        public async Task<bool> OnFreezingCard(PlayerChoiceContext choiceContext, CardModel card)
        {
            return !(card == this);
        }
    }
}