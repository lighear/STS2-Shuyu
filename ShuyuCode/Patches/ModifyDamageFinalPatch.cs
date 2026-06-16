using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Cards;
using Shuyu.Interfaces;
using STS2RitsuLib.Diagnostics.DevConsole;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;
using static MegaCrit.Sts2.Core.Models.CardModel;

namespace Shuyu.Patches
{
    public class ModifyDamageFinalPatch : IPatchMethod
    {
        public static string PatchId => "shuyu_modify_damage_final_patch";
        public static string Description => "在ModifyDamage最后增加一个时点，用于实现完璧不破的效果";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets() => [
            new ModPatchTarget(typeof(Hook), nameof(Hook.ModifyDamage))
            ];

        public static void Postfix(ICombatState? combatState, Creature? target, Creature? dealer, ValueProp props, ref IEnumerable<AbstractModel> modifiers, ref decimal __result)
        {
            if (combatState == null)
            {
                return;
            }
            foreach (IModifyDamageFinal ip in combatState.IterateHookListeners().OfType<IModifyDamageFinal>())
            {
                __result = ip.ModifyDamageFinal(combatState, target, dealer, props, __result, ref modifiers);
            }
        }
    }
}
