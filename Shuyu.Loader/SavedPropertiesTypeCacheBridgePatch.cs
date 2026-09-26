using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Loader;

[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
[HarmonyPriority(Priority.First)]
internal static class SavedPropertiesTypeCacheBridgePatch
{
    private const string CacheTypeName =
        "MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache";
    private const string SavedPropertyAttributeTypeName =
        "MegaCrit.Sts2.Core.Saves.Runs.SavedPropertyAttribute";

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

        Type? cacheType = typeof(AbstractModel).Assembly.GetType(
            CacheTypeName,
            throwOnError: false);
        if (cacheType == null)
        {
            Log.Info(
                "[Shuyu.Loader] Native SavedProperty cache is unavailable; "
                + "using RitsuLib mod type discovery.");
            return;
        }

        MethodInfo? injectMethod = cacheType.GetMethod(
            "InjectTypeIntoCache",
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(Type)],
            modifiers: null);
        if (injectMethod == null)
        {
            Log.Warn("[Shuyu.Loader] Native SavedProperty cache injection API was not found.");
            return;
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
                injectMethod.Invoke(null, [modelType]);
                injectedCount++;
            }
            catch (Exception exception)
            {
                Exception rootCause = exception is TargetInvocationException { InnerException: not null }
                    ? exception.InnerException
                    : exception;
                Log.Warn(
                    $"[Shuyu.Loader] Failed to inject SavedProperty model type "
                    + $"'{modelType.FullName}': {rootCause.Message}");
            }
        }

        Log.Info(
            $"[Shuyu.Loader] Native SavedProperty cache bridge injected {injectedCount}/"
            + $"{modelTypes.Length} Shuyu model type(s).");
    }

    private static bool HasSavedProperty(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(property => property.CustomAttributes.Any(attribute =>
                attribute.AttributeType.FullName == SavedPropertyAttributeTypeName));
    }
}
