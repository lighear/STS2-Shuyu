using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace Shuyu.Loader;

[HarmonyPatch(typeof(ReflectionHelper), "ModTypes", MethodType.Getter)]
internal static class ReflectionHelperModTypesPatch
{
    private static void Postfix(ref Type[] __result)
    {
        Type[] variantModTypes = Bootstrap.GetVariantModTypes();
        if (variantModTypes.Length != 0)
        {
            __result = __result.Concat(variantModTypes).Distinct().ToArray();
        }
    }
}
