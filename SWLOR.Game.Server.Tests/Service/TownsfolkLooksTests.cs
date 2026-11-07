using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.TownsfolkService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The looks street life is made from: each is an existing module creature template that does nothing on its own, so a
/// townsperson only walks and never fights, reacts, or carries loot.
/// </summary>
public class TownsfolkLooksTests
{
    private static readonly string[] AiScripts =
    {
        "ScriptHeartbeat", "ScriptOnNotice", "ScriptSpawn", "ScriptAttacked", "ScriptDamaged", "ScriptDeath",
        "ScriptDisturbed", "ScriptEndRound", "ScriptOnBlocked", "ScriptRested", "ScriptSpellAt", "ScriptUserDefine",
    };

    // The appearances of the playable species (RacialAppearanceRegistry): Human, then the SWLOR species rows.
    private static readonly int[] PlayableAppearances =
    {
        6, 10000, 10001, 10002, 10003, 10004, 10005, 10006, 10007, 10008, 10009, 10010, 10033, 10095, 10096, 10097, 10098, 10180, 10219,
    };

    private static readonly TownsfolkGroup[] AllGroups = { TownsfolkGroup.Wanderer, TownsfolkGroup.Guard, TownsfolkGroup.Critter };

    private static IEnumerable<string> AllResrefs() =>
        AllGroups.SelectMany(group => TownsfolkLooks.Get(group)).Select(look => look.Resref).Distinct();

    [Test]
    public void EveryGroup_HasLooks()
    {
        foreach (var group in AllGroups)
        {
            TownsfolkLooks.Get(group).Should().NotBeEmpty($"{group} needs looks");
        }

        TownsfolkLooks.Get(TownsfolkGroup.Invalid).Should().BeEmpty();
    }

    [Test]
    public void WanderersAndGuards_NeverShareOrRepeatATemplate()
    {
        TownsfolkLooks.Wanderers.Select(look => look.Resref).Should().OnlyHaveUniqueItems();
        TownsfolkLooks.Guards.Select(look => look.Resref).Should().OnlyHaveUniqueItems();
        TownsfolkLooks.Wanderers.Select(look => look.Resref).Should().NotIntersectWith(TownsfolkLooks.Guards.Select(look => look.Resref));
    }

    [Test]
    public void EveryTemplate_ExistsInTheModule()
    {
        foreach (var resref in AllResrefs())
        {
            File.Exists(TemplatePath(resref)).Should().BeTrue($"Module/utc/{resref}.utc.json is the template for a look");
        }
    }

    [Test]
    public void EveryTemplate_RunsNoAiScript_SoItDoesNothingUntilTheCityTellsItToWalk()
    {
        foreach (var resref in AllResrefs())
        {
            using var template = ReadTemplate(resref);
            foreach (var script in AiScripts)
            {
                Text(template.RootElement, script).Should().BeEmpty($"{resref} must not run {script}: it would give it an AI");
            }
        }
    }

    [Test]
    public void EveryTemplate_CarriesNoLootTableQuestGroupOrBeastType()
    {
        foreach (var resref in AllResrefs())
        {
            using var template = ReadTemplate(resref);
            var names = template.RootElement.TryGetProperty("VarTable", out var table)
                ? table.GetProperty("value").EnumerateArray().Select(variable => variable.GetProperty("Name").GetProperty("value").GetString()).ToList()
                : new List<string>();

            names.Should().NotContain(name => name.StartsWith("LOOT_TABLE") || name == "BEAST_TYPE" || name == "QUEST_NPC_GROUP_ID",
                $"{resref} would give loot, a quest kill, or a tameable beast");
        }
    }

    [Test]
    public void EveryPersonLook_IsAPlayableSpeciesOnTheNormalPhenotype()
    {
        foreach (var look in TownsfolkLooks.Wanderers.Concat(TownsfolkLooks.Guards))
        {
            using var template = ReadTemplate(look.Resref);
            Number(template.RootElement, "Appearance_Type").Should().BeOneOf(PlayableAppearances, $"{look.Resref} is not a playable species");
            Number(template.RootElement, "Phenotype").Should().Be(0, $"{look.Resref} must use the normal phenotype");
            look.Appearance.Should().BeNull("a person keeps the template's own body");
        }
    }

    [Test]
    public void TheWanderers_AreUnarmedCivilians()
    {
        foreach (var look in TownsfolkLooks.Wanderers)
        {
            using var template = ReadTemplate(look.Resref);
            var equipped = template.RootElement.GetProperty("Equip_ItemList").GetProperty("value").EnumerateArray()
                .Select(item => item.GetProperty("EquippedRes").GetProperty("value").GetString())
                .ToList();

            equipped.Should().NotContain(resref => resref.Contains("blaster") || resref.Contains("rifle") || resref.Contains("pistol"),
                $"{look.Resref} is a civilian");
        }
    }

    [Test]
    public void TheCritters_AreSmallAnimalsMadeFromOneScriptFreeAnimalTemplate()
    {
        TownsfolkLooks.Critters.Select(look => look.Resref).Should().OnlyContain(resref => resref == TownsfolkLooks.CritterTemplate);
        TownsfolkLooks.Critters.Select(look => look.Appearance).Should().OnlyContain(appearance => appearance.HasValue);
        TownsfolkLooks.Critters.Select(look => look.Appearance.Value).Should().OnlyHaveUniqueItems();

        using var template = ReadTemplate(TownsfolkLooks.CritterTemplate);
        Number(template.RootElement, "Race").Should().Be(8, "the animal racial type");
    }

    [Test]
    public void TheCritterAppearances_AreRowsOfAppearance2da()
    {
        var rows = File.ReadLines(Path.Combine(RepositoryRoot, "SWLOR_Haks", "sw_2da", "appearance.2da"), Encoding.Latin1)
            .Skip(3)
            .Select(line => line.TrimStart().Split(' ', 2)[0])
            .Where(first => int.TryParse(first, out _))
            .Select(int.Parse)
            .ToHashSet();

        foreach (var look in TownsfolkLooks.Critters)
        {
            rows.Should().Contain(look.Appearance.Value, $"{look.Name} needs its appearance row");
        }
    }

    private static JsonDocument ReadTemplate(string resref) =>
        JsonDocument.Parse(Encoding.Latin1.GetString(File.ReadAllBytes(TemplatePath(resref))));

    private static string TemplatePath(string resref) => Path.Combine(RepositoryRoot, "Module", "utc", $"{resref}.utc.json");

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var field) ? field.GetProperty("value").GetString() ?? string.Empty : string.Empty;

    private static int Number(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
