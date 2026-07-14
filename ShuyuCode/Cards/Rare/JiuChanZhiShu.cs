using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    //[RegisterCard(typeof(ShuyuCardPool))]
    public class JiuChanZhiShu : ModCardTemplate, IFrostforged
    {
        public JiuChanZhiShu() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Rare,
            TargetType.AnyEnemy)
        { }

        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Innate,
            CardKeyword.Retain,
            ShuyuKeywords.Frostforged
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(6, ValueProp.Move),
            new RepeatVar(1)
        ];

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player == Owner && Owner.PlayerCombatState!.TurnNumber <= 1 && !IsClone)
            {
                foreach (Creature ally in CombatState!.GetTeammatesOf(Owner.Creature)
                    .Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature))
                {
                    CardModel card = CreateCloneForPlayer(ally.Player!);
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
                }
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);

            foreach (Creature ally in CombatState!.GetTeammatesOf(Owner.Creature)
                    .Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature))
            {
                IEnumerable<JiuChanZhiShu> cards = ally.Player!.PlayerCombatState!.AllCards.OfType<JiuChanZhiShu>()
                    .Where(c =>
                    {
                        CardPile? pile = c.Pile;
                        return pile == null || pile.Type != PileType.Hand;
                    });
                await CardPileCmd.Add(cards, PileType.Hand);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2);
        }

        private decimal _extraRepeatFromFrozen;

        public async Task FrostforgedEffect()
        {
            foreach (Creature ally in CombatState!.GetTeammatesOf(Owner.Creature).Where(c => c != null && c.IsAlive && c.IsPlayer))
            {
                foreach (JiuChanZhiShu item in ally.Player!.PlayerCombatState!.AllCards.OfType<JiuChanZhiShu>())
                {
                    item.DynamicVars.Repeat.BaseValue++;
                    item._extraRepeatFromFrozen++;
                }
            }
        }

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars.Repeat.BaseValue += _extraRepeatFromFrozen;
        }
    }
}