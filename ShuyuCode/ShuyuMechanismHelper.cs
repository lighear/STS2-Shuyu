using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Cards;

namespace Shuyu
{
    public static class ShuyuMechanismHelper
    {
        public static bool IsFrozen(this CardModel card) => card is FrozenCardModel;

        public static void FreezeCard(CardModel card)
        {
            //FrozenCardModel frozenCard = (FrozenCardModel)ModelDb.Card<FrozenCardModel>().ToMutable();
            FrozenCardModel? frozenCard = card.CombatState?.CreateCard<FrozenCardModel>(card.Owner);
            if (frozenCard == null)
            {
                return;
            }
            frozenCard.InitFrom(card);

            Frozen frozenAffliction = (Frozen)ModelDb.Affliction<Frozen>().ToMutable();
            frozenCard.AfflictInternal(frozenAffliction, 1);

            ReplaceCardModelInPile(card, frozenCard);
        }

        public static void UnfreezeCard(FrozenCardModel frozenCard)
        {
            CardModel? original = frozenCard._visualCardModel;
            if (original == null)
            {
                return;
            }
            original.ClearAfflictionInternal();

            ReplaceCardModelInPile(frozenCard, original);
        }

        public static async Task ChooseFromHandAndFreeze(PlayerChoiceContext choiceContext, Player player, int selectCount, AbstractModel source)
        {
            IEnumerable<CardModel> cards =
                await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: player,
                    prefs: new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, selectCount),
                    filter: c => !c.IsFrozen(),
                    source: source);
            foreach (CardModel card in cards)
            {
                await CardCmd.Afflict<Frozen>(card, 1);
            }
        }

        public static async Task IcyDamage(PlayerChoiceContext choiceContext, decimal damage, List<Creature> targets, CardModel cardSource)
        {
            if (targets.Count == 0)
            {
                IReadOnlyList<Creature>? hittableEnemies = cardSource.CombatState?.HittableEnemies;
                if (hittableEnemies != null && hittableEnemies.Count > 0)
                {
                    targets.Add(cardSource.Owner.RunState.Rng.CombatTargets.NextItem(hittableEnemies)!);
                }
            }
            if (targets.Count > 0)
            {
                await CreatureCmd.Damage(choiceContext, targets, damage, ValueProp.Unpowered, cardSource.Owner.Creature, cardSource);
            }
        }

        private static void ReplaceCardModelInPile(CardModel oldCard, CardModel newCard)
        {
            CardPile? pile = oldCard.Pile;
            if (pile == null)
            {
                return;
            }

            // 找到桌面上的 NCard，刷新 UI model
            NCard? nCard = NCard.FindOnTable(oldCard, pile.Type);
            if (nCard != null)
            {
                nCard.Model = newCard;
                nCard.UpdateVisuals(pile.Type, CardPreviewMode.Normal);
            }

            // 从当前牌堆移除原牌，加入 frozen
            int index = pile.Cards.IndexOf(oldCard);
            Entry.Logger.Warn(oldCard.Title + " Index: " + index);
            if (index < 0)
            {
                return;
            }
            oldCard.RemoveFromCurrentPile();
            pile.AddInternal(newCard, index);
        }
    }
}
