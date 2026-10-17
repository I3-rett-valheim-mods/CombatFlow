using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace AnimCancel.Tests;

/// <summary>
/// Reads the installed <c>assembly_valheim.dll</c> so the API guard tests can
/// assert that every game member the plugin depends on still exists, with the
/// same shape and the same visibility.
///
/// Why this exists: the plugin binds to the game at runtime by name. When Iron
/// Gate renames or hides a member, nothing fails at build time — you find out as
/// a MissingMethodException mid-session. These tests turn that into a red
/// <c>dotnet test</c> before you ever launch the game.
///
/// The game assembly is Iron Gate's property and is never committed. Point
/// VALHEIM_MANAGED at your own install if it is not in a standard location; when
/// no assembly is found the guard tests report as passing rather than failing, so
/// the repository still builds on a machine without Valheim.
/// </summary>
internal static class GameAssembly
{
    /// <summary>
    /// The assemblies the plugin references. Localization is in assembly_guiutils
    /// rather than assembly_valheim, which is exactly the kind of thing worth
    /// having a test remember.
    /// </summary>
    private static readonly string[] FileNames =
    {
        "assembly_valheim.dll",
        "assembly_guiutils.dll",
        // Cover.GetCoverForPoint, the roof check EggGrow runs, lives here.
        "assembly_utils.dll",
    };

    private static readonly Lazy<List<AssemblyDefinition>> Loaded = new(Load);

    public static bool IsAvailable => Loaded.Value.Count > 0;

    public static TypeDefinition Type(string name)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Game assemblies not available; guard with IsAvailable.");
        }

        foreach (AssemblyDefinition assembly in Loaded.Value)
        {
            TypeDefinition? found = assembly.MainModule.Types.FirstOrDefault(t => t.Name == name);
            if (found != null)
            {
                return found;
            }
        }

        throw new InvalidOperationException(
            $"Type '{name}' no longer exists in {string.Join(" or ", FileNames)}.");
    }

    /// <summary>The named method, matched on parameter type names when given.</summary>
    public static MethodDefinition Method(string typeName, string methodName, params string[] parameterTypes)
    {
        List<MethodDefinition> candidates = Type(typeName).Methods
            .Where(m => m.Name == methodName)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"'{typeName}.{methodName}' no longer exists.");
        }

        if (parameterTypes.Length == 0)
        {
            // Must not just take the first: if Iron Gate adds an overload and makes
            // the parameterless one internal, returning "some method with this name"
            // keeps the guard green while the call site breaks in game.
            MethodDefinition? parameterless = candidates.FirstOrDefault(m => m.Parameters.Count == 0);

            return parameterless ?? throw new InvalidOperationException(
                $"'{typeName}.{methodName}' exists but has no parameterless overload. " +
                $"Found: {string.Join(" | ", candidates.Select(Describe))}");
        }

        MethodDefinition? match = candidates.FirstOrDefault(m =>
            m.Parameters.Count == parameterTypes.Length &&
            m.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(parameterTypes));

        return match ?? throw new InvalidOperationException(
            $"'{typeName}.{methodName}' exists but no overload takes ({string.Join(", ", parameterTypes)}). " +
            $"Found: {string.Join(" | ", candidates.Select(Describe))}");
    }

    public static FieldDefinition Field(string typeName, string fieldName) =>
        Type(typeName).Fields.FirstOrDefault(f => f.Name == fieldName)
        ?? throw new InvalidOperationException($"'{typeName}.{fieldName}' no longer exists.");

    public static TypeDefinition NestedType(string typeName, string nestedName) =>
        Type(typeName).NestedTypes.FirstOrDefault(t => t.Name == nestedName)
        ?? throw new InvalidOperationException($"'{typeName}.{nestedName}' no longer exists.");

    private static string Describe(MethodDefinition method) =>
        $"{method.Name}({string.Join(", ", method.Parameters.Select(p => p.ParameterType.Name))})";

    private static List<AssemblyDefinition> Load()
    {
        foreach (string directory in CandidateDirectories())
        {
            if (!FileNames.All(name => File.Exists(Path.Combine(directory, name))))
            {
                continue;
            }

            return FileNames
                .Select(name => AssemblyDefinition.ReadAssembly(Path.Combine(directory, name)))
                .ToList();
        }

        return new List<AssemblyDefinition>();
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("VALHEIM_MANAGED");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            // An explicit setting wins outright. Falling back to the default paths
            // after it would quietly test a different install than the one asked
            // for, and would make a deliberately wrong value untestable.
            yield return fromEnvironment!;
            yield break;
        }

        yield return @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed";
        yield return @"C:\Program Files\Steam\steamapps\common\Valheim\valheim_Data\Managed";
    }
}
