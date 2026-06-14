using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Interfaces;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using static Godot.OpenXRInterface;

namespace Shuyu.Patches
{
    public sealed class HookHandFullPatchSet : IModPatches
    {
        public static void AddTo(ModPatcher patcher)
        {
            patcher.RegisterPatch<DrawPatch>();
            //patcher.RegisterPatch<HandFullPatch>();
        }

        internal static bool handFull;

        public class DrawPatch : IPatchMethod
        {
            // patch的ID，取得不一样防撞车
            public static string PatchId => "shuyu_draw_patch";
            // 补丁用途说明
            public static string Description => "检测无法抽牌的数量并Hook";
            // 重要性。失败是否崩溃，false即为不会导致游戏报错。
            public static bool IsCritical => true;

            // 要改的原版方法
            public static ModPatchTarget[] GetTargets() => [
                new ModPatchTarget(typeof(CardPileCmd),
                    nameof(CardPileCmd.Draw),
                    [typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)])
                ];

            // 可用Prefix, Postfix, Transpiler等
            /*public static void Prefix()
            {
                Entry.Logger.Info($"[Shuyu] CardPileCmd.Draw Prefix Enter");
                handFull = false;
                Entry.Logger.Info($"[Shuyu] CardPileCmd.Draw Prefix handFull: {handFull}");
            }*/

            public static void Postfix(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result)
            {
                if (!Hook.ShouldDraw(player.Creature.CombatState!, player, fromHandDraw, out _))
                {
                    return;
                }
                if (PileType.Draw.GetPile(player).Cards.Count + PileType.Discard.GetPile(player).Cards.Count == 0)
                {
                    return;
                }
                int drawsRequested = ((count > 0m) ? ((int)Math.Ceiling(count)) : 0);
                int handFullCount = drawsRequested - __result.Result.Count();
                if (handFullCount > 0)
                {
                    foreach (ICantDrawForHandFull ip in player.Creature.CombatState!.IterateHookListeners().OfType<ICantDrawForHandFull>())
                    {
                        ip.CantDrawForHandFull(choiceContext, handFullCount, player);
                    }
                }

                /*Entry.Logger.Info($"[Shuyu] CardPileCmd.Draw Postfix Enter");
                Entry.Logger.Info($"[Shuyu] CardPileCmd.Draw Postfix handFull: {handFull}");
                if (handFull)
                {
                    IEnumerable<ICantDrawForHandFull>? ips = player.Creature.CombatState?.IterateHookListeners().OfType<ICantDrawForHandFull>();
                    if (ips != null)
                    {
                        int drawsRequested = ((count > 0m) ? ((int)Math.Ceiling(count)) : 0);
                        int handFullCount = drawsRequested - __result.Result.Count();
                        Entry.Logger.Info($"[Shuyu] CardPileCmd.Draw Postfix: {count} {drawsRequested} {handFullCount}");
                        foreach (ICantDrawForHandFull ip in ips)
                        {
                            ip.CantDrawForHandFull(choiceContext, handFullCount, player);
                        }
                    }
                }*/
            }
        }

        /*public class HandFullPatch : IPatchMethod
        {
            public static string PatchId => "shuyu_hand_full_patch";
            public static string Description => "检测是否因手牌数已满而无法抽牌";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets() => [
                new ModPatchTarget(typeof(CardPileCmd), nameof(CardPileCmd.CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot))
                ];

            
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    FieldInfo fieldInfo = AccessTools.Field(typeof(HookHandFullPatchSet), "handFull");
                    if (fieldInfo == null)
                    {
                        Entry.Logger.Error("[Shuyu] Failed to find handFull field in HookHandFullPatchSet");
                        return instructions;
                    }

                    CodeMatcher matcher = new CodeMatcher(instructions)
                        .MatchEndForward(
                            new CodeMatch(OpCodes.Ldstr, "HAND_FULL"),
                            new CodeMatch(OpCodes.Newobj),
                            new CodeMatch(OpCodes.Ldarg_0),
                            new CodeMatch(OpCodes.Callvirt),
                            new CodeMatch(OpCodes.Ldc_R8, 2.0),
                            new CodeMatch(OpCodes.Call))
                        .Advance(1);

                    if (matcher.IsInvalid)
                    {
                        Entry.Logger.Error("[Shuyu] Failed to match HAND_FULL pattern in CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot");
                        return instructions;
                    }

                    return matcher.Insert(
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Stsfld, fieldInfo))
                        .Instructions();
                }
                catch (Exception e)
                {
                    Entry.Logger.Error($"[Shuyu] Exception in Transpiler: {e}");
                    return instructions;
                }
            }
        }*/
    }
}
