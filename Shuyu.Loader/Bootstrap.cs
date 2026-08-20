using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Shuyu.Loader;

[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private sealed record VariantCandidate(string CompatTarget, Version Version, string DllPath);

    private const string ModId = "Shuyu";
    private const string RealDllName = "Shuyu.dll";

    private static readonly string[] KnownVersions = ["0.111.0", "0.107.1"];
    private static readonly Lock VariantAssembliesLock = new();
    private static readonly List<Assembly> VariantAssemblies = [];

    private static bool _reflectionBridgePatched;

    public static void Initialize()
    {
        string? directoryName = Path.GetDirectoryName(typeof(Bootstrap).Assembly.Location);
        if (string.IsNullOrEmpty(directoryName))
        {
            Log.Error("[Shuyu.Loader] Could not resolve loader directory.");
            return;
        }

        string libRoot = Path.Combine(directoryName, "lib");
        if (!Directory.Exists(libRoot))
        {
            Log.Error("[Shuyu.Loader] Missing lib directory: " + libRoot);
            return;
        }

        Version? hostVersion = DetectHostVersion();
        VariantCandidate? variant = PickVariant(directoryName, libRoot, hostVersion);
        if (variant == null)
        {
            Log.Error($"[Shuyu.Loader] No compatible variant under {libRoot} (host={hostVersion?.ToString() ?? "unknown"}).");
            return;
        }

        Log.Info($"[Shuyu.Loader] Host version {hostVersion}; picked variant {variant.CompatTarget}.");
        string dllPath = variant.DllPath;
        if (!File.Exists(dllPath))
        {
            Log.Error("[Shuyu.Loader] Variant folder missing Shuyu.dll: " + dllPath);
            return;
        }

        AssemblyLoadContext loadContext =
            AssemblyLoadContext.GetLoadContext(typeof(Bootstrap).Assembly)
            ?? AssemblyLoadContext.Default;

        Assembly assembly;
        try
        {
            assembly = loadContext.LoadFromAssemblyPath(dllPath);
            RegisterVariantAssembly(assembly);
        }
        catch (Exception exception)
        {
            Log.Error($"[Shuyu.Loader] Failed to load {dllPath}: {exception}");
            return;
        }

        try
        {
            InvokeRealInitializer(assembly);
        }
        catch (Exception exception)
        {
            Log.Error($"[Shuyu.Loader] Failed to initialize Shuyu: {exception}");
        }

        try
        {
            EnsureGodotScriptsRegistered(assembly);
        }
        catch (Exception exception)
        {
            Log.Warn("[Shuyu.Loader] EnsureGodotScriptsRegistered failed: " + exception.Message);
        }

        try
        {
            ModelIdSerializationCacheRebuildPatch.TryRebuild();
        }
        catch (Exception exception)
        {
            Log.Warn("[Shuyu.Loader] ModelIdSerializationCache rebuild failed: " + exception.Message);
        }
    }

    private static Version? DetectHostVersion()
    {
        try
        {
            string? rawVersion = ReleaseInfoManager.Instance.ReleaseInfo?.Version;
            Log.Info("[Shuyu.Loader] ReleaseInfo.Version raw: '" + (rawVersion ?? "NULL") + "'");
            if (!string.IsNullOrWhiteSpace(rawVersion))
            {
                string trimmedVersion = rawVersion.StartsWith('v') || rawVersion.StartsWith('V')
                    ? rawVersion[1..]
                    : rawVersion;
                Log.Info("[Shuyu.Loader] Trimmed: '" + trimmedVersion + "'");
                if (Version.TryParse(trimmedVersion, out Version? result))
                {
                    Log.Info($"[Shuyu.Loader] Parsed version: {result}");
                    return result;
                }

                Log.Warn("[Shuyu.Loader] Failed to parse version: '" + trimmedVersion + "'");
            }
        }
        catch (Exception exception)
        {
            Log.Warn("[Shuyu.Loader] ReleaseInfo lookup failed: " + exception.Message);
        }

        Version? assemblyVersion = typeof(ModManager).Assembly.GetName().Version;
        Log.Info($"[Shuyu.Loader] Fallback assembly version: {assemblyVersion}");
        if (assemblyVersion != null
            && (assemblyVersion.Major != 0
                || assemblyVersion.Minor != 0
                || assemblyVersion.Build != 0))
        {
            return assemblyVersion;
        }

        return null;
    }

    private static VariantCandidate? PickVariant(
        string loaderDir,
        string libRoot,
        Version? host)
    {
        List<VariantCandidate> candidates = [];
        foreach (string knownVersion in KnownVersions)
        {
            if (!Version.TryParse(knownVersion, out Version? version))
            {
                continue;
            }

            string dllPath = Path.Combine(libRoot, knownVersion, RealDllName);
            if (File.Exists(dllPath))
            {
                candidates.Add(new VariantCandidate(knownVersion, version, dllPath));
            }
        }

        if (candidates.Count == 0)
        {
            Log.Error("[Shuyu.Loader] No variant DLLs found under " + libRoot + ".");
            return null;
        }

        if (host == null)
        {
            Log.Error("[Shuyu.Loader] Host version is unknown; refusing to guess a variant.");
            return null;
        }

        string? requiredTarget = null;
        if (host.Major == 0 && host.Minor == 107 && host.Build == 1)
        {
            requiredTarget = "0.107.1";
        }
        else if (host >= new Version(0, 111, 0))
        {
            requiredTarget = "0.111.0";
            if (host.Major != 0 || host.Minor != 111)
            {
                Log.Warn($"[Shuyu.Loader] Host version {host} is newer than 0.111.x; attempting the 0.111.0 variant as a forward-compatibility fallback.");
            }
        }

        if (requiredTarget == null)
        {
            Log.Error($"[Shuyu.Loader] Unsupported host version {host}; 0.107.1 is supported, 0.108-0.110 are rejected, and 0.111.0+ uses the 0.111.0 variant.");
            return null;
        }

        VariantCandidate? selected = candidates.FirstOrDefault(
            candidate => candidate.CompatTarget == requiredTarget);
        if (selected == null)
        {
            Log.Error($"[Shuyu.Loader] Required variant {requiredTarget} is missing under {libRoot}.");
        }

        return selected;
    }

    private static void RegisterVariantAssembly(Assembly variantAssembly)
    {
        bool registered = false;
        try
        {
            MethodInfo? associateMethod = typeof(ModManager).GetMethod(
                "AssociateAssemblyWithMod",
                BindingFlags.Static | BindingFlags.Public,
                binder: null,
                [typeof(string), typeof(Assembly)],
                modifiers: null);
            if (associateMethod != null)
            {
                associateMethod.Invoke(null, [ModId, variantAssembly]);
                Log.Info("[Shuyu.Loader] Registered via AssociateAssemblyWithMod (v0.110.0+).");
                registered = true;
            }
        }
        catch (Exception exception)
        {
            Log.Info("[Shuyu.Loader] AssociateAssemblyWithMod failed: " + exception.Message);
        }

        if (!registered)
        {
            try
            {
                FieldInfo? assemblyField = typeof(Mod).GetField(
                    "assembly",
                    BindingFlags.Instance | BindingFlags.Public);
                if (assemblyField == null)
                {
                    Log.Warn("[Shuyu.Loader] Mod.assembly field not found (v0.110.0+ renamed it).");
                }
                else
                {
                    foreach (Mod mod in ModManager.Mods)
                    {
                        if (mod.manifest?.id != ModId)
                        {
                            continue;
                        }

                        assemblyField.SetValue(mod, variantAssembly);
                        Log.Info("[Shuyu.Loader] Registered via direct Mod.assembly set (v0.107 fallback).");
                        registered = true;
                        break;
                    }

                    if (!registered)
                    {
                        Log.Warn("[Shuyu.Loader] Could not find our Mod entry to register assembly.");
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warn("[Shuyu.Loader] Fallback registration failed: " + exception.Message);
            }
        }

        if (!registered)
        {
            return;
        }

        using (VariantAssembliesLock.EnterScope())
        {
            VariantAssemblies.Add(variantAssembly);
        }

        EnsureReflectionBridgePatch();
    }

    internal static Type[] GetVariantModTypes()
    {
        Assembly[] assemblies;
        using (VariantAssembliesLock.EnterScope())
        {
            assemblies = VariantAssemblies.ToArray();
        }

        return assemblies
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    return exception.Types.OfType<Type>();
                }
                catch
                {
                    return [];
                }
            })
            .Distinct()
            .ToArray();
    }

    private static void EnsureReflectionBridgePatch()
    {
        if (_reflectionBridgePatched)
        {
            return;
        }

        new Harmony("Shuyu.Loader.ReflectionBridge").PatchAll(typeof(Bootstrap).Assembly);
        _reflectionBridgePatched = true;
        Log.Info("[Shuyu.Loader] Reflection bridge patch installed.");
    }

    private static void InvokeRealInitializer(Assembly realAssembly)
    {
        foreach (Type type in realAssembly.GetTypes())
        {
            ModInitializerAttribute? initializer =
                type.GetCustomAttribute<ModInitializerAttribute>();
            if (initializer == null)
            {
                continue;
            }

            MethodInfo? method = type.GetMethod(
                initializer.initializerMethod,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                continue;
            }

            method.Invoke(null, null);
            return;
        }

        Log.Error("[Shuyu.Loader] No ModInitializer found in " + realAssembly.FullName + ".");
    }

    private static void EnsureGodotScriptsRegistered(Assembly assembly)
    {
        Assembly? godotSharp = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(candidate => candidate.GetName().Name == "GodotSharp");
        if (godotSharp == null)
        {
            Log.Warn("[Shuyu.Loader] GodotSharp not found, skipping script registration.");
            return;
        }

        Type? bridgeType = godotSharp.GetType("Godot.Bridge.ScriptManagerBridge");
        MethodInfo? lookupMethod = bridgeType?.GetMethod(
            "LookupScriptsInAssembly",
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(Assembly)],
            modifiers: null);
        if (bridgeType == null || lookupMethod == null)
        {
            Log.Warn("[Shuyu.Loader] ScriptManagerBridge.LookupScriptsInAssembly not found.");
            return;
        }

        if (AreShuyuScriptsRegistered(assembly, bridgeType))
        {
            Log.Info("[Shuyu.Loader] Godot scripts already registered, skipping.");
            return;
        }

        lookupMethod.CreateDelegate<Action<Assembly>>()(assembly);
        Log.Info("[Shuyu.Loader] Registered Godot scripts for " + assembly.GetName().Name + ".");
    }

    private static bool AreShuyuScriptsRegistered(Assembly assembly, Type bridgeType)
    {
        try
        {
            string[] scriptPaths = EnumerateShuyuScriptPaths(assembly).ToArray();
            if (scriptPaths.Length == 0)
            {
                return true;
            }

            object? pathTypeMap = bridgeType
                .GetField("_pathTypeBiMap", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null);
            if (pathTypeMap == null)
            {
                return false;
            }

            MethodInfo? tryGetScriptType = pathTypeMap.GetType().GetMethod(
                "TryGetScriptType",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                [typeof(string), typeof(Type).MakeByRefType()],
                modifiers: null);
            if (tryGetScriptType == null)
            {
                return false;
            }

            object?[] arguments = new object?[2];
            foreach (string scriptPath in scriptPaths)
            {
                arguments[0] = scriptPath;
                arguments[1] = null;
                object? result = tryGetScriptType.Invoke(pathTypeMap, arguments);
                if (result is not bool found || !found)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateShuyuScriptPaths(Assembly assembly)
    {
        Type? assemblyHasScriptsAttribute =
            assembly.GetType("Godot.AssemblyHasScriptsAttribute")
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(candidate => candidate.GetTypes())
                .FirstOrDefault(type => type.FullName == "Godot.AssemblyHasScriptsAttribute");
        Type? scriptPathAttribute =
            assembly.GetType("Godot.ScriptPathAttribute")
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(candidate => candidate.GetTypes())
                .FirstOrDefault(type => type.FullName == "Godot.ScriptPathAttribute");
        if (assemblyHasScriptsAttribute == null || scriptPathAttribute == null)
        {
            yield break;
        }

        object? assemblyAttribute = assembly
            .GetCustomAttributes(assemblyHasScriptsAttribute, inherit: false)
            .FirstOrDefault();
        if (assemblyAttribute == null)
        {
            yield break;
        }

        PropertyInfo? requiresLookupProperty =
            assemblyHasScriptsAttribute.GetProperty("RequiresLookup");
        bool requiresLookup = requiresLookupProperty != null
            && (bool)(requiresLookupProperty.GetValue(assemblyAttribute) ?? true);
        PropertyInfo? pathProperty = scriptPathAttribute.GetProperty("Path");

        IEnumerable<Type> scriptTypes;
        if (requiresLookup)
        {
            Type? godotObjectType = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == "GodotSharp")
                ?.GetType("Godot.GodotObject");
            if (godotObjectType == null)
            {
                yield break;
            }

            scriptTypes = assembly.GetTypes()
                .Where(type => !type.IsNested && godotObjectType.IsAssignableFrom(type));
        }
        else
        {
            PropertyInfo? scriptTypesProperty =
                assemblyHasScriptsAttribute.GetProperty("ScriptTypes");
            if (scriptTypesProperty == null)
            {
                yield break;
            }

            scriptTypes =
                (IEnumerable<Type>?)scriptTypesProperty.GetValue(assemblyAttribute)
                ?? [];
        }

        foreach (Type scriptType in scriptTypes)
        {
            object? scriptPath = scriptType
                .GetCustomAttributes(scriptPathAttribute, inherit: false)
                .FirstOrDefault();
            if (scriptPath == null)
            {
                continue;
            }

            string? path = pathProperty?.GetValue(scriptPath) as string;
            if (!string.IsNullOrWhiteSpace(path))
            {
                yield return path;
            }
        }
    }
}
