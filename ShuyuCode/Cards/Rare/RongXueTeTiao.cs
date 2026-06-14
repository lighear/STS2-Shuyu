using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Characters;
using Shuyu.Commands;
using Shuyu.Interfaces;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class RongXueTeTiao : ModCardTemplate, IFrostforged, IOnFreezingCard
    {
        public RongXueTeTiao() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<DexterityPower>(),
            ..HoverTipFactory.FromAffliction<Frozen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<StrengthPower>(2),
            new PowerVar<DexterityPower>(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<RongXueTeTiaoStrengthUpPower>(choiceContext, Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<RongXueTeTiaoDexterityUpPower>(choiceContext, Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);

            if (IsUpgraded)
            {
                await ShuyuMechanismCmd.ChooseFromHandAndUnfreeze(choiceContext, Owner, 1, this);
            }
            else
            {
                CardModel? card = PileType.Hand.GetPile(Owner).Cards
                    .Where(c => c.IsFrozen())
                    .TakeRandom(1, Owner.RunState.Rng.CombatCardSelection)
                    .FirstOrDefault();
                if (card is FrozenCardModel frozenCard)
                {
                    await ShuyuMechanismCmd.UnfreezeCard(frozenCard);
                }
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Strength.UpgradeValueBy(1);
            DynamicVars.Dexterity.UpgradeValueBy(1);
        }

        public Task FrostforgedEffect()
        {
            return Task.CompletedTask;
        }

        public async Task OnFreezingCard(CardModel card)
        {
            if (card.Owner == Owner && this.Pile?.Type == PileType.Discard)
            {
                await CardPileCmd.Add(this, PileType.Hand);
            }
        }
    }
}