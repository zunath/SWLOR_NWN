using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The spawn service reads an area's spawn tables with GetLocalString, so a table stored as any other variable type, or
/// naming a table no definition builds, leaves the area without those spawns.
/// </summary>
public class AreaSpawnTableVariableTests
{
    private const int StringVariableType = 3;

    private static readonly string[] SpawnTableVariables =
    [
        "RESOURCE_SPAWN_TABLE_ID", "CREATURE_SPAWN_TABLE_ID", "SLICING_TERMINAL_SPAWN_TABLE_ID",
    ];

    [Test]
    public void AreaSpawnTableVariables_AreStringsNamingDefinedTables()
    {
        var tableIds = typeof(ISpawnListDefinition).Assembly.GetTypes()
            .Where(t => typeof(ISpawnListDefinition).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (ISpawnListDefinition)Activator.CreateInstance(t)!)
            .SelectMany(d => d.BuildSpawnTables().Keys)
            .ToHashSet();

        var problems = new List<string>();
        var checkedVariables = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "Module", "git"), "*.git.json"))
        {
            var area = Path.GetFileName(path)[..^".git.json".Length];
            using var variables = ReadAreaVariables(path);
            if (variables == null)
                continue;

            foreach (var variable in variables.RootElement.EnumerateArray())
            {
                var name = variable.GetProperty("Name").GetProperty("value").GetString();
                if (!SpawnTableVariables.Contains(name))
                    continue;

                checkedVariables++;
                var type = variable.GetProperty("Type").GetProperty("value").GetInt32();
                var value = variable.GetProperty("Value").GetProperty("value");
                if (type != StringVariableType || value.ValueKind != JsonValueKind.String)
                    problems.Add($"{area} {name} is not a string ({value})");
                else if (!tableIds.Contains(value.GetString()!))
                    problems.Add($"{area} {name} names undefined table {value.GetString()}");
            }
        }

        checkedVariables.Should().BeGreaterThan(50);
        problems.Should().BeEmpty();
    }

    // Area git files reach 25 MB, so only the root-level VarTable is parsed; it usually comes first.
    private static JsonDocument? ReadAreaVariables(string path)
    {
        var reader = new Utf8JsonReader(File.ReadAllBytes(path));
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1 &&
                reader.ValueTextEquals("VarTable"))
            {
                reader.Read();
                using var table = JsonDocument.ParseValue(ref reader);
                return table.RootElement.TryGetProperty("value", out var list)
                    ? JsonDocument.Parse(list.GetRawText())
                    : null;
            }

            if (reader.CurrentDepth == 1 && reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                reader.Skip();
        }

        return null;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
