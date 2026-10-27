using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the built perk definitions: for every perk its id, category, group and flags, and for every perk level its
/// price, droid AI slots, granted feats, requirements and stat bonuses. Delegates are not locked; the snapshot only
/// records which of them a definition sets. Stat bonuses are evaluated once for no creature, because most are
/// constants written as a lambda; descriptions are locked by length and fingerprint, not by text.
/// </summary>
public class PerkDefinitionCharacterizationTests
{
    private const string EngineValue = "engine";

    // PerkDetail properties that have a column, are collections of other locked data, or are code.
    private static readonly string[] PerkProperties =
    {
        nameof(PerkDetail.Category),
        nameof(PerkDetail.Description),
        nameof(PerkDetail.EquippedTriggers),
        nameof(PerkDetail.ForceAffinityType),
        nameof(PerkDetail.GroupType),
        nameof(PerkDetail.HotBarActionModes),
        nameof(PerkDetail.IconResref),
        nameof(PerkDetail.IsActive),
        nameof(PerkDetail.Name),
        nameof(PerkDetail.PerkLevels),
        nameof(PerkDetail.PurchasedTriggers),
        nameof(PerkDetail.PurchaseRequirement),
        nameof(PerkDetail.RefundedTriggers),
        nameof(PerkDetail.RefundRequirement),
        nameof(PerkDetail.StatBonuses),
        nameof(PerkDetail.Type),
        nameof(PerkDetail.UnequippedTriggers),
    };

    private static readonly string[] PerkLevelProperties =
    {
        nameof(PerkLevel.Description),
        nameof(PerkLevel.DroidAISlots),
        nameof(PerkLevel.GrantedFeats),
        nameof(PerkLevel.Price),
        nameof(PerkLevel.Requirements),
        nameof(PerkLevel.StatBonuses),
    };

    [Test]
    public void PerkCategories()
    {
        var table = new SnapshotTable(
            "PerkCategoryType metadata",
            "The display name and active flag every perk category declares through its PerkCategory attribute.",
            "Every PerkCategoryType member, sorted by id.",
            new[] { "category", "categoryName" },
            new[] { "displayName", "isActive" })
        {
            AllowConstantOutputs = true
        };

        foreach (var category in Enum.GetValues<PerkCategoryType>().OrderBy(category => DefinitionCell.Id(category)))
        {
            var attribute = category.GetAttribute<PerkCategoryType, PerkCategoryAttribute>();
            table.Add(DefinitionCell.Id(category), category, attribute.Name, attribute.IsActive);
        }

        Snapshot.Verify("Perk", "PerkCategories", table);
    }

    [Test]
    public void PerkDefinitions()
    {
        var perks = PerkDefinitionSource.Load();

        var table = new SnapshotTable(
            "Perk definitions (every IPerkListDefinition)",
            "The built data of every perk: name, category, group, active flag, explicitly set icon, force affinity, hotbar modes, " +
            "perk-wide stat bonuses and the number of levels. Delegates are listed by name when set but their bodies are not locked.",
            $"{perks.Count} perks sorted by perk id. Stat bonuses are Stat=value for no creature (\"engine\" if the bonus needs the engine). " +
            "Icon is only what the definition set explicitly (null otherwise); the rest come from feat.2da at load time. " +
            "Perk descriptions are always unset; the text lives on the levels.",
            new[] { "perk", "perkName" },
            new[]
            {
                "name", "category", "group", "isActive", "icon", "forceAffinity", "hotBarActionModes", "statBonuses",
                "descriptionFingerprint", "levelCount", "delegates"
            })
        {
            AllowConstantOutputs = true
        };

        foreach (var (perkType, perk) in perks)
        {
            table.Add(
                DefinitionCell.Id(perkType),
                perkType,
                perk.Name,
                perk.Category,
                perk.GroupType,
                perk.IsActive,
                perk.IconResref,
                perk.ForceAffinityType,
                perk.HotBarActionModes.Select(mode => DefinitionCell.Text(mode)).ToArray(),
                perk.StatBonuses.Select(StatBonusText).ToArray(),
                DefinitionCell.Fingerprint(perk.Description),
                perk.PerkLevels.Count,
                DelegateNames(perk));
        }

        Snapshot.Verify("Perk", "PerkDefinitions", table);
    }

    [Test]
    public void PerkLevels()
    {
        var perks = PerkDefinitionSource.Load();
        var levelCount = perks.Sum(perk => perk.Value.PerkLevels.Count);

        var table = new SnapshotTable(
            "Perk levels (every level of every IPerkListDefinition perk)",
            "The built data of every perk level: SP price, droid AI slots, granted feats, requirements, stat bonuses and a fingerprint of the description.",
            $"{levelCount} levels of {perks.Count} perks, sorted by perk id then level. Requirements are Category(values) in the order the definition adds them. " +
            "Stat bonuses are Stat=value for no creature (\"engine\" if the bonus needs the engine), so a bonus that depends on equipment reads as its unequipped value. " +
            "descriptionFingerprint is the first 12 hex digits of the SHA-256 of the UTF-8 description text.",
            new[] { "perk", "perkName", "level" },
            new[]
            {
                "price", "droidAISlots", "grantedFeats", "requirements", "statBonuses", "descriptionLength", "descriptionFingerprint"
            })
        {
            AllowConstantOutputs = true
        };

        foreach (var (perkType, perk) in perks)
        {
            foreach (var (level, perkLevel) in perk.PerkLevels.OrderBy(entry => entry.Key))
            {
                table.Add(
                    DefinitionCell.Id(perkType),
                    perkType,
                    level,
                    perkLevel.Price,
                    perkLevel.DroidAISlots,
                    perkLevel.GrantedFeats.Select(feat => DefinitionCell.Text(feat)).ToArray(),
                    perkLevel.Requirements.Select(RequirementText).ToArray(),
                    perkLevel.StatBonuses.Select(StatBonusText).ToArray(),
                    perkLevel.Description?.Length,
                    DefinitionCell.Fingerprint(perkLevel.Description));
            }
        }

        Snapshot.Verify("Perk", "PerkLevels", table);
    }

    [Test]
    public void EveryPerkDetailProperty_IsLockedOrDeliberatelyLeftOut()
    {
        Assert.That(PropertyNames(typeof(PerkDetail)), Is.EquivalentTo(PerkProperties),
            "PerkDetail gained or lost a property. Lock new plain data in PerkDefinitions (and add it to PerkProperties), " +
            "or list new code (a delegate) as a delegate name in DelegateNames.");
        Assert.That(PropertyNames(typeof(PerkLevel)), Is.EquivalentTo(PerkLevelProperties),
            "PerkLevel gained or lost a property. Lock new plain data in PerkLevels and add it to PerkLevelProperties.");
    }

    [Test]
    public void PerkDefinitionSource_FindsTheSamePerksAsTheRealBuildPerks()
    {
        // BuildPerks cannot finish outside the engine, so the snapshots read the perks through the definitions' private
        // methods. This proves that route finds exactly the perks, levels and prices BuildPerks itself collects.
        foreach (var definitionType in PerkDefinitionSource.DefinitionTypes())
        {
            var viaMethods = Summarize(PerkDefinitionSource.LoadDefinition(definitionType));
            var viaBuildPerks = Summarize(PerkDefinitionSource.LoadThroughBuildPerks(definitionType));
            Assert.That(viaMethods, Is.EqualTo(viaBuildPerks), definitionType.FullName);
            Assert.That(viaMethods, Is.Not.Empty, definitionType.FullName);
        }
    }

    private static string[] Summarize(IEnumerable<KeyValuePair<PerkType, PerkDetail>> perks)
    {
        return perks
            .OrderBy(entry => DefinitionCell.Id(entry.Key))
            .Select(entry => $"{entry.Key}:{entry.Value.Category}:" +
                             string.Join(",", entry.Value.PerkLevels.OrderBy(level => level.Key).Select(level => $"{level.Key}/{level.Value.Price}")))
            .ToArray();
    }

    private static string[] PropertyNames(Type type)
    {
        return type.GetProperties().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }

    private static string[] DelegateNames(PerkDetail perk)
    {
        var names = new List<string>();
        if (perk.PurchaseRequirement != null)
            names.Add(nameof(PerkDetail.PurchaseRequirement));
        if (perk.RefundRequirement != null)
            names.Add(nameof(PerkDetail.RefundRequirement));
        if (perk.EquippedTriggers.Count > 0)
            names.Add($"{nameof(PerkDetail.EquippedTriggers)}:{perk.EquippedTriggers.Count}");
        if (perk.UnequippedTriggers.Count > 0)
            names.Add($"{nameof(PerkDetail.UnequippedTriggers)}:{perk.UnequippedTriggers.Count}");
        if (perk.PurchasedTriggers.Count > 0)
            names.Add($"{nameof(PerkDetail.PurchasedTriggers)}:{perk.PurchasedTriggers.Count}");
        if (perk.RefundedTriggers.Count > 0)
            names.Add($"{nameof(PerkDetail.RefundedTriggers)}:{perk.RefundedTriggers.Count}");
        return names.ToArray();
    }

    private static string StatBonusText(PerkStatBonus bonus)
    {
        try
        {
            return $"{bonus.Stat}={bonus.Calculate(0)}";
        }
        catch (Exception)
        {
            return $"{bonus.Stat}={EngineValue}";
        }
    }

    // The requirement classes keep most of their data in private fields, so every instance field is read in
    // declaration order: Skill(Lightsaber, 5), MustHavePerk(Foo, 2), BeastLevel(10).
    private static string RequirementText(IPerkRequirement requirement)
    {
        var fields = requirement.GetType()
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .OrderBy(field => field.MetadataToken)
            .Select(field => DefinitionCell.Text(field.GetValue(requirement)));
        return $"{requirement.Category}({string.Join(", ", fields)})";
    }
}
