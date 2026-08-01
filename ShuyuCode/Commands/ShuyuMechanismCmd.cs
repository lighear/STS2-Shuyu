using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Afflictions;
using Shuyu.Cards;
using Shuyu.Interfaces;
using Shuyu.Vfx;
using Shuyu.Powers;
using Shuyu.Characters;

namespace Shuyu.Commands
{
    public static class ShuyuMechanismCmd
    {
        public static bool IsFrozen(this CardModel card) => card is FrozenCardModel;

        public static bool IsFrostforged(this CardModel card) => card is IFrostforged;

        public static async Task FreezeCard(CardModel card)
        {
            if (card == null || card.IsFrozen())
            {
                return;
            }
            CardCmd.ClearAffliction(card);

            bool unfreeze = false;
            if (card is IFrostforged frostforged)
            {
                unfreeze = true;

                int count = 1;
                HuiXiangYongChangPower? power = card.Owner.Creature.GetPower<HuiXiangYongChangPower>();
                if (power != null)
                {
                    power.Flash();
                    count += power.Amount;
                }
                for (int i = 0; i < count; i++)
                {
                    await frostforged.FrostforgedEffect();
                }
            }

            var ips = card.CombatState?.IterateHookListeners().OfType<IOnFreezingCard>();
            if (ips != null)
            {
                foreach (IOnFreezingCard ip in ips)
                {
                    unfreeze = !(await ip.OnFreezingCard(card)) || unfreeze;
                }
            }

            if (unfreeze)
            {
                var ips2 = card.CombatState?.IterateHookListeners().OfType<IAfterUnfreezingCard>();
                if (ips2 != null)
                {
                    foreach (IAfterUnfreezingCard ip in ips2)
                    {
                        await ip.AfterUnfreezingCard(card);
                    }
                }
                return;
            }
            
            FrozenCardModel? frozenCard = card.CombatState?.CreateCard<FrozenCardModel>(card.Owner);
            if (frozenCard == null)
            {
                return;
            }
            frozenCard.InitFrom(card);

            await ReplaceCardModelInPile(
                card,
                frozenCard,
                keepPosition: card.Pile?.Type == PileType.Draw);
            await CardCmd.Afflict<Frozen>(frozenCard, 1);
        }

        public static async Task UnfreezeCard(FrozenCardModel frozenCard)
        {
            CardModel? original = frozenCard._visualCardModel;
            if (original == null)
            {
                return;
            }

            CardCmd.ClearAffliction(frozenCard);
            await ReplaceCardModelInPile(frozenCard, original);

            var ips = original.CombatState?.IterateHookListeners().OfType<IAfterUnfreezingCard>();
            if (ips != null)
            {
                foreach (IAfterUnfreezingCard ip in ips)
                {
                    await ip.AfterUnfreezingCard(original);
                }
            }
        }

        public static async Task ChooseFromHandAndFreeze(PlayerChoiceContext choiceContext, Player player, int selectCount, AbstractModel source, bool optional = false)
        {
            CardSelectorPrefs prefs = optional ? new CardSelectorPrefs(new LocString("card_selection", "TO_FREEZE_OPTIONAL"), 0, selectCount)
                                    : new CardSelectorPrefs(new LocString("card_selection", "TO_FREEZE"), selectCount);
            prefs.ShouldGlowGold = card => card.IsFrostforged();
            IEnumerable<CardModel> cards =
                await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: player,
                    prefs: prefs,
                    filter: c => !c.IsFrozen(),
                    source: source);
            foreach (CardModel card in cards)
            {
                await FreezeCard(card);
            }
        }

        public static async Task ChooseFromHandAndUnfreeze(PlayerChoiceContext choiceContext, Player player, int selectCount, AbstractModel source, bool optional = false)
        {
            IEnumerable<CardModel> cards =
                await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: player,
                    prefs: optional ? new CardSelectorPrefs(new LocString("card_selection", "TO_UNFREEZE_OPTIONAL"), 0, selectCount)
                                    : new CardSelectorPrefs(new LocString("card_selection", "TO_UNFREEZE"), selectCount),
                    filter: c => c.IsFrozen(),
                    source: source);
            foreach (FrozenCardModel card in cards.OfType<FrozenCardModel>())
            {
                await UnfreezeCard(card);
            }
        }

        public static async Task ChooseFromHandAndChangeFrozenState(PlayerChoiceContext choiceContext, Player player, int selectCount, AbstractModel source)
        {
            CardSelectorPrefs prefs = new CardSelectorPrefs(new LocString("card_selection", "CHANGE_FROZEN_STATE"), selectCount)
            {
                ShouldGlowGold = card => card.IsFrostforged()
            };
            IEnumerable<CardModel> cards =
                await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: player,
                    prefs: prefs,
                    filter: null,
                    source: source);
            foreach (CardModel card in cards)
            {
                if (card is FrozenCardModel frozenCard)
                {
                    await UnfreezeCard(frozenCard);
                }
                else
                {
                    await FreezeCard(card);
                }
            }
        }

        public static async Task IcyDamage(
            PlayerChoiceContext choiceContext,
            decimal damage,
            List<Creature> targets,
            CardModel cardSource,
            int effectiveEnergyCost)
        {
            await ConfirmIcyDamageTargets(targets, cardSource);
            if (targets.Count > 0)
            {
                NBingShuangChongJiVfx.SpawnFrozenCardProjectiles(
                    cardSource.Owner.Creature,
                    targets,
                    effectiveEnergyCost);
                await CreatureCmd.Damage(choiceContext, targets, damage, ValueProp.Move, cardSource.Owner.Creature, cardSource, null);
            }
        }

        private static async Task ReplaceCardModelInPile(CardModel oldCard, CardModel newCard, bool keepPosition = false)
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
            int index = keepPosition ? pile.Cards.ToList().IndexOf(oldCard) : -1;
            oldCard.RemoveFromCurrentPile();
            pile.AddInternal(newCard, index);
        }

        private static async Task ConfirmIcyDamageTargets(List<Creature> targets, CardModel cardSource)
        {
            targets.RemoveAll(t => t.IsDead);
            if (targets.Count == 0)
            {
                IReadOnlyList<Creature>? hittableEnemies = cardSource.CombatState?.HittableEnemies;
                if (hittableEnemies != null && hittableEnemies.Count > 0)
                {
                    targets.Add(cardSource.Owner.RunState.Rng.CombatTargets.NextItem(hittableEnemies)!);
                }
            }
        }
    }
}
