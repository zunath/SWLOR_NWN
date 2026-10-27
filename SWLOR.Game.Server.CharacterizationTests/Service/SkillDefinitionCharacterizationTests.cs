using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the skill and skill category definitions that the XP rules read: maximum rank, whether a skill counts toward
/// the skill cap, which menus show it and which character types may train it. Descriptions are locked by length and
/// fingerprint, not by text.
/// </summary>
[NonParallelizable]
public class SkillDefinitionCharacterizationTests
{
    private static readonly string[] SkillProperties =
    {
        nameof(SkillAttribute.Category),
        nameof(SkillAttribute.CharacterTypeRestriction),
        nameof(SkillAttribute.CombatPointCategory),
        nameof(SkillAttribute.ContributesToSkillCap),
        nameof(SkillAttribute.Description),
        nameof(SkillAttribute.IsActive),
        nameof(SkillAttribute.IsShownInCraftMenu),
        nameof(SkillAttribute.IsShownInResearchMenu),
        nameof(SkillAttribute.MaxRank),
        nameof(SkillAttribute.Name),
    };

    [OneTimeSetUp]
    public void LoadItemMappings()
    {
        // Pure: fills the base item to skill table (and prints how many it loaded). The same call runs at module load.
        Skill.LoadMappings();
    }

    [Test]
    public void GetSkillTypeByBaseItem()
    {
        var items = Enum.GetValues<BaseItem>().Distinct().OrderBy(item => (int)item).ToArray();

        var table = new SnapshotTable(
            "Skill.GetSkillTypeByBaseItem(BaseItem) after Skill.LoadMappings()",
            "The skill a base item type trains and is trained by: weapons and ammunition map to their weapon skill, everything else to Invalid.",
            $"Every BaseItem member ({items.Length}) in numeric order.",
            new[] { "baseItem", "baseItemName" },
            new[] { "skill" });

        foreach (var item in items)
            table.Add((int)item, item, Skill.GetSkillTypeByBaseItem(item));

        Snapshot.Verify("Skill", "GetSkillTypeByBaseItem", table);
    }

    [Test]
    public void PublicConstants()
    {
        var table = new SnapshotTable(
            "Skill (public const fields and APCap)",
            "The skill point cap, the starting skill points, the total spendable skill points and the ability point cap derived from the skill point cap.",
            "All public const fields of Skill, sorted by name, then the APCap property.",
            new[] { "constant" },
            new[] { "type", "value" })
        {
            AllowConstantOutputs = true
        };

        var constants = typeof(Skill)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal);
        foreach (var field in constants)
            table.Add(field.Name, field.FieldType.Name, field.GetRawConstantValue());

        table.Add(nameof(Skill.APCap), typeof(int).Name, Skill.APCap);

        Snapshot.Verify("Skill", "PublicConstants", table);
    }

    [Test]
    public void SkillCategories()
    {
        var table = new SnapshotTable(
            "SkillCategoryType metadata",
            "The display name, active flag and menu sequence every skill category declares through its SkillCategory attribute.",
            "Every SkillCategoryType member, sorted by id.",
            new[] { "category", "categoryName" },
            new[] { "displayName", "isActive", "sequence" })
        {
            AllowConstantOutputs = true
        };

        foreach (var category in Enum.GetValues<SkillCategoryType>().OrderBy(category => DefinitionCell.Id(category)))
        {
            var attribute = category.GetAttribute<SkillCategoryType, SkillCategoryAttribute>();
            table.Add(DefinitionCell.Id(category), category, attribute.Name, attribute.IsActive, attribute.Sequence);
        }

        Snapshot.Verify("Skill", "SkillCategories", table);
    }

    [Test]
    public void SkillDefinitions()
    {
        var skills = Enum.GetValues<SkillType>().OrderBy(skill => DefinitionCell.Id(skill)).ToArray();

        var table = new SnapshotTable(
            "SkillType metadata (SkillAttribute of every skill)",
            "The data every skill declares: category, display name, maximum rank, active flag, whether it counts toward the skill cap, " +
            "the craft and research menu flags, the combat point category and the character type restriction.",
            $"{skills.Length} SkillType members sorted by id. descriptionFingerprint is the first 12 hex digits of the SHA-256 of the UTF-8 description text.",
            new[] { "skill", "skillName" },
            new[]
            {
                "category", "name", "maxRank", "isActive", "contributesToSkillCap", "isShownInCraftMenu", "isShownInResearchMenu",
                "combatPointCategory", "characterTypeRestriction", "descriptionLength", "descriptionFingerprint"
            })
        {
            AllowConstantOutputs = true
        };

        foreach (var skill in skills)
        {
            var attribute = skill.GetAttribute<SkillType, SkillAttribute>();
            table.Add(
                DefinitionCell.Id(skill),
                skill,
                attribute.Category,
                attribute.Name,
                attribute.MaxRank,
                attribute.IsActive,
                attribute.ContributesToSkillCap,
                attribute.IsShownInCraftMenu,
                attribute.IsShownInResearchMenu,
                attribute.CombatPointCategory,
                attribute.CharacterTypeRestriction,
                attribute.Description?.Length,
                DefinitionCell.Fingerprint(attribute.Description));
        }

        Snapshot.Verify("Skill", "SkillDefinitions", table);
    }

    [Test]
    public void IsAvailableToCharacterType()
    {
        var table = new SnapshotTable(
            "SkillAttribute.IsAvailableToCharacterType",
            "Whether a character type may train a skill: skills without a restriction are open to every type, others only to the restricted type.",
            "Every skill whose CharacterTypeRestriction is not Invalid, plus one unrestricted skill (Armor), x every CharacterType member.",
            new[] { "skill", "characterType" },
            new[] { "isAvailable" });

        var restricted = Enum.GetValues<SkillType>()
            .Where(skill => skill.GetAttribute<SkillType, SkillAttribute>().CharacterTypeRestriction != CharacterType.Invalid)
            .Append(SkillType.Armor)
            .OrderBy(skill => DefinitionCell.Id(skill));
        foreach (var skill in restricted)
        {
            var attribute = skill.GetAttribute<SkillType, SkillAttribute>();
            foreach (var characterType in Enum.GetValues<CharacterType>().OrderBy(type => DefinitionCell.Id(type)))
                table.Add(skill, characterType, attribute.IsAvailableToCharacterType(characterType));
        }

        Snapshot.Verify("Skill", "IsAvailableToCharacterType", table);
    }

    [Test]
    public void EverySkillAttributeProperty_IsLockedOrDeliberatelyLeftOut()
    {
        var properties = typeof(SkillAttribute)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.That(properties, Is.EquivalentTo(SkillProperties),
            "SkillAttribute gained or lost a property. Lock new plain data in SkillDefinitions and add it to SkillProperties.");
    }
}
