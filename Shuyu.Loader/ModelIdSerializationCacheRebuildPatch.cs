using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Shuyu.Loader;

[HarmonyPatch(typeof(ModelIdSerializationCache), "Init")]
[HarmonyPriority(int.MaxValue)]
internal static class ModelIdSerializationCacheRebuildPatch
{
    private static readonly FieldInfo? CategoryMapField =
        typeof(ModelIdSerializationCache).GetField(
            "_categoryNameToNetIdMap",
            BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? CategoryListField =
        typeof(ModelIdSerializationCache).GetField(
            "_netIdToCategoryNameMap",
            BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? EntryMapField =
        typeof(ModelIdSerializationCache).GetField(
            "_entryNameToNetIdMap",
            BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? EntryListField =
        typeof(ModelIdSerializationCache).GetField(
            "_netIdToEntryNameMap",
            BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly PropertyInfo? CategoryBitSizeProperty =
        typeof(ModelIdSerializationCache).GetProperty(
            "CategoryIdBitSize",
            BindingFlags.Static | BindingFlags.Public);

    private static readonly PropertyInfo? EntryBitSizeProperty =
        typeof(ModelIdSerializationCache).GetProperty(
            "EntryIdBitSize",
            BindingFlags.Static | BindingFlags.Public);

    private static readonly PropertyInfo? HashProperty =
        typeof(ModelIdSerializationCache).GetProperty(
            "Hash",
            BindingFlags.Static | BindingFlags.Public);

    private static readonly FieldInfo? ContentByIdField =
        typeof(ModelDb).GetField(
            "_contentById",
            BindingFlags.Static | BindingFlags.NonPublic);

    private static void Postfix()
    {
        TryRebuild();
    }

    public static void TryRebuild()
    {
        if (CategoryMapField == null
            || CategoryListField == null
            || EntryMapField == null
            || EntryListField == null)
        {
            Log.Warn("[Shuyu.Loader] ModelIdSerializationCache internals not accessible; skipping rebuild.");
            return;
        }

        Dictionary<string, int> categoryMap =
            (Dictionary<string, int>)CategoryMapField.GetValue(null)!;
        List<string> categoryList =
            (List<string>)CategoryListField.GetValue(null)!;
        Dictionary<string, int> entryMap =
            (Dictionary<string, int>)EntryMapField.GetValue(null)!;
        List<string> entryList =
            (List<string>)EntryListField.GetValue(null)!;

        if (ContentByIdField?.GetValue(null) is not IDictionary { Count: not 0 } contentById)
        {
            return;
        }

        SortedSet<string> missingCategories = new(StringComparer.Ordinal);
        SortedSet<string> missingEntries = new(StringComparer.Ordinal);
        foreach (DictionaryEntry item in contentById)
        {
            if (item.Key is not ModelId modelId)
            {
                continue;
            }

            if (!categoryMap.ContainsKey(modelId.Category))
            {
                missingCategories.Add(modelId.Category);
            }

            if (!entryMap.ContainsKey(modelId.Entry))
            {
                missingEntries.Add(modelId.Entry);
            }
        }

        if (missingCategories.Count == 0 && missingEntries.Count == 0)
        {
            return;
        }

        foreach (string category in missingCategories)
        {
            categoryMap[category] = categoryList.Count;
            categoryList.Add(category);
        }

        foreach (string entry in missingEntries)
        {
            entryMap[entry] = entryList.Count;
            entryList.Add(entry);
        }

        Log.Info(
            $"[Shuyu.Loader] Rebuilt ModelIdSerializationCache: +{missingCategories.Count} categories, "
            + $"+{missingEntries.Count} entries (total: {categoryList.Count} categories, "
            + $"{entryList.Count} entries).");

        CategoryBitSizeProperty
            ?.GetSetMethod(nonPublic: true)
            ?.Invoke(
                null,
                [(int)Math.Ceiling(Math.Log2(Math.Max(categoryList.Count, 2)))]);
        EntryBitSizeProperty
            ?.GetSetMethod(nonPublic: true)
            ?.Invoke(
                null,
                [(int)Math.Ceiling(Math.Log2(Math.Max(entryList.Count, 2)))]);

        if (HashProperty != null)
        {
            uint hash = ComputeStableHash(categoryList, entryList);
            HashProperty
                .GetSetMethod(nonPublic: true)
                ?.Invoke(null, [hash]);
        }
    }

    private static uint ComputeStableHash(
        List<string> categories,
        List<string> entries)
    {
        uint hash = 2166136261u;
        foreach (string category in categories)
        {
            foreach (char character in category)
            {
                hash ^= character;
                hash *= 16777619;
            }

            hash ^= 0xFF;
            hash *= 16777619;
        }

        foreach (string entry in entries)
        {
            foreach (char character in entry)
            {
                hash ^= character;
                hash *= 16777619;
            }

            hash ^= 0xFF;
            hash *= 16777619;
        }

        return hash;
    }
}
