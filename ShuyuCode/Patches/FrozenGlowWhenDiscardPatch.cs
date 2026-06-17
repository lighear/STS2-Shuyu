using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Commands;
using STS2RitsuLib.Patching.Models;
using System.Reflection;
using System.Reflection.Emit;

namespace Shuyu.Patches
{
    public class FrozenGlowWhenDiscardPatch : IPatchMethod
    {
        public static string PatchId => "shuyu_frozen_glow_when_discard_patch";
        public static string Description => "让封冻牌在被选择丢弃的时候发光";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() => [
            new ModPatchTarget(typeof(CardSelectCmd), 
                nameof(CardSelectCmd.FromHandForDiscard), 
                MethodType.Async)
            ];

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Type? d26 = typeof(CardSelectCmd).GetNestedType("<FromHandForDiscard>d__26", BindingFlags.NonPublic);
            if (d26 == null)
            {
                Entry.Logger.Error("[Shuyu][FrozenGlowWhenDiscardPatch] Failed to get async method CardSelectCmd.FromHandForDiscard.");
                return instructions;
            }
            FieldInfo prefsField = AccessTools.Field(d26, "prefs");
            MethodInfo setShouldGlow = AccessTools.Method(typeof(CardSelectorPrefs), "set_ShouldGlowGold");
            MethodInfo addCondition = AccessTools.Method(typeof(FrozenGlowWhenDiscardPatch), "AddFrozenGlowCondition");

            CodeMatcher matcher = new CodeMatcher(instructions).MatchEndForward(new CodeMatch(OpCodes.Call, setShouldGlow));
            if (matcher.IsInvalid)
            {
                Entry.Logger.Error("[Shuyu][FrozenGlowWhenDiscardPatch] Failed to match set_ShouldGlowGold.");
                return instructions;
            }

            return matcher.Advance(1)
                .Insert(new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldflda, prefsField),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, prefsField),
                    new CodeInstruction(OpCodes.Call, addCondition),
                    new CodeInstruction(OpCodes.Call, setShouldGlow))
                .InstructionEnumeration();
        }

        public static Func<CardModel, bool> AddFrozenGlowCondition(CardSelectorPrefs prefs)
        {
            Func<CardModel, bool>? originalDelegate = prefs.ShouldGlowGold;
            if (originalDelegate != null)
            {
                return card => originalDelegate(card) || card.IsFrozen();
            }
            return card => card.IsFrozen();
        }
    }
}
