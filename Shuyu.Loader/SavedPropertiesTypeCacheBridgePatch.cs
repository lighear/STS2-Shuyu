using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Shuyu.Loader;

[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
[HarmonyPriority(Priority.First)]
internal static class SavedPropertiesTypeCacheBridgePatch
{
    private static readonly Lock Gate = new();
    private static bool _completed;

    private static void Prefix()
    {
        using (Gate.EnterScope())
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
        }

        Type[] modelTypes = Bootstrap.GetVariantModTypes()
            .Where(type =>
                !type.IsAbstract
                && !type.IsInterface
                && typeof(AbstractModel).IsAssignableFrom(type)
                && HasSavedProperty(type))
            .OrderBy(type => type.Assembly.FullName, StringComparer.Ordinal)
            .ThenBy(type => type.FullName ?? type.Name, StringComparer.Ordinal)
            .ToArray();

        int injectedCount = 0;
        foreach (Type modelType in modelTypes)
        {
            try
            {
                SavedPropertiesTypeCache.InjectTypeIntoCache(modelType);
                injectedCount++;
            }
            catch (Exception exception)
            {
                Log.Warn(
                    $"[Shuyu.Loader] Failed to inject SavedProperty model type "
                    + $"'{modelType.FullName}': {exception.Message}");
            }
        }

        Log.Info(
            $"[Shuyu.Loader] SavedProperty cache bridge injected {injectedCount}/"
            + $"{modelTypes.Length} Shuyu model type(s).");
    }

    private static bool HasSavedProperty(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
    }
}
