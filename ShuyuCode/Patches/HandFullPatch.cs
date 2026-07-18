using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Interfaces;
using STS2RitsuLib.Patching.Models;

namespace Shuyu.Patches
{
    public class HookHandFullPatch : IPatchMethod
    {
        // patch的ID，取得不一样防撞车
        public static string PatchId => "shuyu_hand_full_patch";
        // 补丁用途说明
        public static string Description => "检测因手牌数已满无法抽牌的数量并Hook";
        // 重要性。失败是否崩溃，false即为不会导致游戏报错。
        public static bool IsCritical => true;

        // 要改的原版方法
        public static ModPatchTarget[] GetTargets() => [
            new ModPatchTarget(typeof(CardPileCmd),
                    nameof(CardPileCmd.Draw),
                    [typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)])
            ];

        // 可用Prefix, Postfix, Transpiler等
        public static void Postfix(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result)
        {
            __result = TaskWithHandFullHook(__result, choiceContext, count, player, fromHandDraw);
        }

        private static async Task<IEnumerable<CardModel>> TaskWithHandFullHook(Task<IEnumerable<CardModel>> originalTask,PlayerChoiceContext choiceContext,decimal count,Player player,bool fromHandDraw)
        {
            IEnumerable<CardModel> drawnCards = await originalTask;

            // CardPileCmd.Draw also returns no cards when combat is already over or
            // ending. That is not a hand-full event, and running a win check here can
            // tear down combat while the attack that caused the victory is still
            // resolving (for example, lethal thorns followed by Gremlin Horn).
            if (CombatManager.Instance.IsOverOrEnding)
            {
                return drawnCards;
            }

            if (!Hook.ShouldDraw(player.Creature.CombatState!, player, fromHandDraw, out _)
                || PileType.Draw.GetPile(player).Cards.Count + PileType.Discard.GetPile(player).Cards.Count == 0)
            {
                return drawnCards;
            }

            int drawsRequested = (count > 0) ? (int)Math.Ceiling(count) : 0;
            int handFullCount = drawsRequested - drawnCards.Count();
            if (handFullCount > 0)
            {
                foreach (ICantDrawForHandFull ip in player.Creature.CombatState!.IterateHookListeners().OfType<ICantDrawForHandFull>())
                {
                    await ip.CantDrawForHandFull(choiceContext, handFullCount, player);
                }
                await CombatManager.Instance.CheckWinCondition();
            }
            return drawnCards;
        }
    }
}
