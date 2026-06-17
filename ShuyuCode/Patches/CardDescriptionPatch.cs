using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Cards;
using STS2RitsuLib.Patching.Models;
using static MegaCrit.Sts2.Core.Models.CardModel;

namespace Shuyu.Patches
{
    public class CardDescriptionPatch : IPatchMethod
    {
        public static string PatchId => "shuyu_card_description_patch";
        public static string Description => "修改封冻牌的卡面描述";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() => [
            new ModPatchTarget(typeof(CardModel), 
                nameof(CardModel.GetDescriptionForPile),
                [typeof(PileType), typeof(DescriptionPreviewType), typeof(Creature)])
            ];

        public static void Postfix(CardModel __instance, ref string __result)
        {
            if (__instance is FrozenCardModel frozenCard)
            {
                __result = $"{__result}\n[color=gray]{frozenCard._visualCardModel?.GetDescriptionForPile(PileType.Deck)}[/color]";
            }
        }
    }
}
