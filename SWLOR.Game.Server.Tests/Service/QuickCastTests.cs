using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.AbilityDefinition.Leadership;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class QuickCastTests
{
    [Test]
    public void Preferences_StartDisabledForNewOrMissingSettings()
    {
        var settings = new PlayerSettings();
        QuickCast.IsEnabled(settings, QuickCastTargetType.Self).Should().BeFalse();
        QuickCast.IsEnabled(settings, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        QuickCast.IsEnabled((PlayerSettings)null, QuickCastTargetType.Self).Should().BeFalse();
        QuickCast.IsEnabled((PlayerSettings)null, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void Preferences_ReadIndependentSavedFlags(bool self, bool enemy)
    {
        var settings = new PlayerSettings { SelfQuickCastEnabled = self, EnemyQuickCastEnabled = enemy };
        QuickCast.IsEnabled(settings, QuickCastTargetType.Self).Should().Be(self);
        QuickCast.IsEnabled(settings, QuickCastTargetType.CurrentEnemy).Should().Be(enemy);
        QuickCast.IsEnabled(settings, (QuickCastTargetType)(-1)).Should().BeFalse();
    }

    [TestCase(EffectTypeScript.Paralyze)]
    [TestCase(EffectTypeScript.CutsceneParalyze)]
    [TestCase(EffectTypeScript.Dazed)]
    [TestCase(EffectTypeScript.Stunned)]
    [TestCase(EffectTypeScript.Sleep)]
    [TestCase(EffectTypeScript.Petrify)]
    [TestCase(EffectTypeScript.Knockdown)]
    [TestCase(EffectTypeScript.Confused)]
    [TestCase(EffectTypeScript.Frightened)]
    [TestCase(EffectTypeScript.Dominated)]
    [TestCase(EffectTypeScript.CutsceneDominated)]
    [TestCase(EffectTypeScript.Turned)]
    public void NativeActionControl_BlocksQuickCasting(EffectTypeScript effectType) =>
        QuickCastPolicy.BlocksCasting(effectType).Should().BeTrue();

    [TestCase(EffectTypeScript.CutsceneImmobilize)]
    [TestCase(EffectTypeScript.Entangle)]
    [TestCase(EffectTypeScript.MovementSpeedDecrease)]
    [TestCase(EffectTypeScript.Blindness)]
    public void EffectsThatAllowStationaryAbilities_DoNotBlockQuickCasting(EffectTypeScript effectType) =>
        QuickCastPolicy.BlocksCasting(effectType).Should().BeFalse();

    [Test]
    public void SelfCast_RequiresExplicitDefinitionOptIn()
    {
        var builder = new AbilityBuilder().Create(FeatType.MedKit1, PerkType.MedKit)
            .IsCastedAbility().IsSingleTargetAbility().RequiresTarget();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.Self).Should().BeFalse();
        builder.AllowsSelfQuickCast();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.Self).Should().BeTrue();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void ActualDefinitions_AllowSelfSupportAndHostileCasts_ExcludeRevivalAndAimedAreas()
    {
        var support = new MedKitAbilityDefinition().BuildAbilities();
        support.Values.Should().OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new BenevolenceAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new RousingShoutAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ResuscitationAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ForceInterceptAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ForceDrainAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
        new ForcePushAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
        new ForceLightningAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
    }

    [Test]
    public void QuickCast_DoesNotOfferQueuedWeaponsSelfAreasOrMissingTargets()
    {
        var ability = new AbilityDetail
        {
            ActivationType = AbilityActivationType.Casted, IsSingleTargetAbility = true,
            RequiresTarget = true, IsHostileAbility = true
        };
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeTrue();
        ability.ActivationType = AbilityActivationType.Weapon;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.ActivationType = AbilityActivationType.Casted;
        ability.IsAreaAbility = true;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.IsAreaAbility = false;
        ability.RequiresTarget = false;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.RequiresTarget = true;
        ability.UsesActiveAttackTarget = true;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.UsesActiveAttackTarget = false;
        ability.Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Cone, 5f, 2f, AbilityTargetingFlags.HarmsEnemies, null);
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void PowerCell_AllRanksAllowSelfCasting_AndKeepManualTargetRequirements()
    {
        foreach (var ability in new PowerCellAbilityDefinition().BuildAbilities().Values)
        {
            QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self).Should().BeTrue();
            QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
            ability.RequiresTarget.Should().BeTrue();
            ability.CustomValidation.Should().NotBeNull();
        }
    }

    [TestCase(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.HelpsAllies, true)]
    [TestCase(AbilityTargetingShapeType.Cone, AbilityTargetingFlags.HelpsAllies, false)]
    [TestCase(AbilityTargetingShapeType.Rect, AbilityTargetingFlags.HelpsAllies, false)]
    [TestCase(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.IgnoresSelf, false)]
    [TestCase(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.HarmsEnemies, false)]
    [TestCase(AbilityTargetingShapeType.Sphere, AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf, false)]
    public void AreaSelfCast_RequiresOptInAndAnAllyTargetedSphere(
        AbilityTargetingShapeType shape, AbilityTargetingFlags flags, bool eligible)
    {
        var ability = new PowerCellAbilityDefinition().BuildAbilities()[FeatType.PowerCell3];
        ability.Targeting = new AbilityTargetingDetail(Spell.PowerCell3, shape, 5f, 0f, flags, null);
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self).Should().Be(eligible);
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.AllowsSelfQuickCast = false;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self).Should().BeFalse();
        ability.AllowsSelfQuickCast = true;
        ability.IsHostileAbility = true;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self).Should().BeFalse();
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void EveryEligibleDefinition_HasANativeAliasThatResolvesToItsCanonicalFeat()
    {
        var eligible = GetAllAbilities()
            .Where(entry => QuickCastPolicy.IsEligible(entry.Value, QuickCastTargetType.Self) ||
                            QuickCastPolicy.IsEligible(entry.Value, QuickCastTargetType.CurrentEnemy))
            .Select(entry => entry.Key)
            .ToArray();
        eligible.Should().NotBeEmpty();
        QuickCastFeatMap.Originals.Values.Should().Contain(eligible,
            "every eligible rank needs an alias so its ordinary native button can skip targeting");

        foreach (var original in eligible)
        {
            QuickCastFeatMap.TryGetAlias(original, out var alias).Should().BeTrue($"{original} is eligible");
            alias.ToString().Should().Be(QuickCastFeatMap.AliasPrefix + original);
            QuickCastFeatMap.IsAlias(alias).Should().BeTrue();
            QuickCastFeatMap.IsAlias(original).Should().BeFalse();
            QuickCastFeatMap.GetOriginal(alias).Should().Be(original);
            QuickCastFeatMap.GetOriginal(original).Should().Be(original);
            QuickCastFeatMap.TryGetAlias(alias, out var repeatedAlias).Should().BeTrue();
            repeatedAlias.Should().Be(alias, "normalizing a saved alias shortcut must be idempotent");
        }
    }

    [Test]
    public void EveryNativeAlias_PreservesCanonicalFeatCellsAndSpellId()
    {
        var (columns, rows) = ReadFeatRows();
        QuickCastFeatMap.Originals.Should().NotBeEmpty();
        QuickCastFeatMap.Originals.Values.Should().OnlyHaveUniqueItems();

        foreach (var (alias, original) in QuickCastFeatMap.Originals)
        {
            rows.Should().ContainKeys(alias, original);
            var aliasCells = rows[alias];
            var originalCells = rows[original];
            aliasCells[Array.IndexOf(columns, "LABEL")].Should().Be(alias.ToString());
            aliasCells[Array.IndexOf(columns, "TARGETSELF")].Should().Be("1");
            aliasCells[Array.IndexOf(columns, "HostileFeat")].Should().BeOneOf("****", "0");
            aliasCells[Array.IndexOf(columns, "SPELLID")].Should()
                .Be(originalCells[Array.IndexOf(columns, "SPELLID")], $"{alias} shares its canonical targeting spell");
            for (var index = 0; index < columns.Length; index++)
            {
                if (columns[index] is "LABEL" or "TARGETSELF" or "HostileFeat")
                    continue;
                aliasCells[index].Should().Be(originalCells[index],
                    $"{alias} must preserve {original}'s {columns[index]} cell");
            }
        }
    }

    private static Dictionary<FeatType, AbilityDetail> GetAllAbilities()
    {
        var definitions = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => typeof(IAbilityListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .OrderBy(type => type.Name);
        var abilities = new Dictionary<FeatType, AbilityDetail>();
        foreach (var type in definitions)
            foreach (var entry in ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
                abilities.Add(entry.Key, entry.Value);
        return abilities;
    }

    private static (string[] Columns, Dictionary<FeatType, string[]> Rows) ReadFeatRows()
    {
        var path = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_2da", "feat.2da");
        var lines = File.ReadAllLines(path);
        var columns = lines[2].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        columns.Should().Contain(new[] { "LABEL", "TARGETSELF", "HostileFeat", "SPELLID" });
        var rows = new Dictionary<FeatType, string[]>();
        foreach (var line in lines.Skip(3))
        {
            var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                continue;
            tokens.Should().HaveCount(columns.Length + 1, "each feat row has an ID followed by every declared cell");
            rows.Add((FeatType)int.Parse(tokens[0]), tokens.Skip(1).ToArray());
        }
        return (columns, rows);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        foreach (var start in new[] { TestContext.CurrentContext.TestDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                    Directory.Exists(Path.Combine(directory.FullName, "SWLOR_Haks")))
                    return directory;
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
