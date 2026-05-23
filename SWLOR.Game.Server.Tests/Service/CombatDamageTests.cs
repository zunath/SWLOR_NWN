using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NativeDamageType = NWN.Native.API.DamageType;
using NWNScriptDamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatDamageTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Environment.SetEnvironmentVariable(
            "SWLOR_APP_LOG_DIRECTORY",
            Path.Combine(TestContext.CurrentContext.WorkDirectory, "logs") + Path.DirectorySeparatorChar);
        Log.Register();
    }

    [Test]
    public void CalculateDamageRange_FloorsPositiveDmgHitsAtOne()
    {
        var (minDamage, maxDamage) = Combat.CalculateDamageRange(
            attackerAttack: 1,
            attackerDMG: 18,
            attackerStat: 10,
            defenderDefense: 100000,
            defenderStat: 10,
            critical: 0);

        minDamage.Should().Be(1);
        maxDamage.Should().Be(1);
    }

    [Test]
    public void CalculateDamageRange_PreservesZeroDmgImpacts()
    {
        var (minDamage, maxDamage) = Combat.CalculateDamageRange(
            attackerAttack: 1,
            attackerDMG: 0,
            attackerStat: 10,
            defenderDefense: 100000,
            defenderStat: 10,
            critical: 0);

        minDamage.Should().Be(0);
        maxDamage.Should().Be(0);
    }

    [Test]
    public void ForceDamage_UsesForceCombatMetadataAndMagicEnginePayload()
    {
        var forceDamage = CombatDamageType.Force.GetDetails();

        forceDamage.Category.Should().Be(CombatDamageCategoryType.Force);
        forceDamage.DefenseDamageType.Should().Be(CombatDamageType.Force);
        forceDamage.SourceResistanceType.Should().Be(ResistanceType.Disruption);
        forceDamage.NWScriptDamageType.Should().Be(NWNScriptDamageType.Force);
        forceDamage.NativeDamageType.Should().Be(NativeDamageType.Magical);
    }

    [Test]
    public void PhysicalDamage_UsesPhysicalCombatMetadataAndSlashingEnginePayload()
    {
        var physicalDamage = CombatDamageType.Physical.GetDetails();

        physicalDamage.Category.Should().Be(CombatDamageCategoryType.Physical);
        physicalDamage.DefenseDamageType.Should().Be(CombatDamageType.Physical);
        physicalDamage.SourceResistanceType.Should().Be(ResistanceType.Trauma);
        physicalDamage.NWScriptDamageType.Should().Be(NWNScriptDamageType.Slashing);
        physicalDamage.NativeDamageType.Should().Be(NativeDamageType.Slashing);
    }

    [Test]
    public void AbilityCombatImpact_IncludesWeaponDamageAndKeepsPhysicalEffectDamagePhysical()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));
        var damageTypeSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "CombatService", "CombatDamageType.cs"));

        abilitySource.Should().Contain("Combat.GetCombatImpactWeaponDamage(activator, skillType)");
        abilitySource.Should().Contain("effectDamageType ?? damageType.GetNWScriptDamageType()");
        abilitySource.Should().NotContain("GetNWScriptDamagePower");
        abilitySource.Should().NotContain("GetCombatImpactEffectDamagePower");
        abilitySource.Should().NotContain("private static int GetCombatImpactWeaponDamage");
        damageTypeSource.Should().NotContain("GetNWScriptDamagePower");
        damageTypeSource.Should().NotContain("DamagePower");
    }

    [Test]
    public void CombatReadiness_AppliesToActivatedAbilityDamageBeforeTargetMitigation()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"))
            .ReplaceLineEndings("\n");

        abilitySource.Should().Contain("public static int ApplyCombatReadinessToActivatedAbilityMagnitude(uint activator, int amount)");
        abilitySource.Should().Contain("if (amount <= 0 || GetTrackedAbilityImpact(activator) == null)");
        abilitySource.Should().Contain("var combatReadiness = Stat.GetCombatReadinessPercent(activator);");
        abilitySource.Should().Contain(
            "calculatedDamage = Combat.ApplyDamageDealtModifiers(activator, target, calculatedDamage, skillType, damageType, true);\n" +
            "            calculatedDamage = ApplyCombatReadinessToActivatedAbilityMagnitude(activator, calculatedDamage);\n" +
            "            calculatedDamage = Resistance.ApplyResistanceToDamage(target, damageType, calculatedDamage);");
    }

    [Test]
    public void CombatReadiness_AppliesToDirectActivatedHealingButNotStatusTicks()
    {
        var root = FindRepositoryRoot();
        var healingSources = new[]
        {
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "AbilityEffectScaling.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "FirstAid", "FirstAidTreatmentAdjustments.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "FirstAid", "MedKitAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Force", "ForceDrainAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "HeavyVibroblade", "HeavyVibrobladeActiveAbilityDefinitionBase.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Beastmaster", "InnervateAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Beastmaster", "RewardAbilityDefinition.cs"),
        };

        foreach (var sourcePath in healingSources)
        {
            File.ReadAllText(sourcePath).Should().Contain("Ability.ApplyCombatReadinessToActivatedAbilityMagnitude");
        }

        var directScaledHealingSources = new[]
        {
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "FirstAid", "EmergencyTriageAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "FirstAid", "ResuscitationAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Force", "BenevolenceAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Force", "CircleOfHarmonyAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Force", "ForceMendAbilityDefinition.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Force", "PurifyingWaveAbilityDefinition.cs"),
        };

        foreach (var sourcePath in directScaledHealingSources)
        {
            File.ReadAllText(sourcePath).Should().Contain("Activated");
        }

        var statusEffectDirectory = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition");
        foreach (var sourcePath in Directory.EnumerateFiles(statusEffectDirectory, "*.cs"))
        {
            File.ReadAllText(sourcePath).Should().NotContain("ApplyCombatReadinessToActivatedAbilityMagnitude");
        }
    }

    [Test]
    public void GuardedHitModifiers_OnlyRunForPhysicalDamageFromDamageRoll()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var damageRollSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));

        combatSource.Should().Contain("ApplyGuardedHitModifiers(uint defender, uint attacker, int damage, CombatDamageType damageType)");
        combatSource.Should().Contain("!damageType.IsPhysicalDamageType()");
        damageRollSource.Should().Contain("Combat.ApplyGuardedHitModifiers(target.m_idSelf, attacker.m_idSelf, damage, damageType);");
        damageRollSource.Should().NotContain("Combat.ApplyGuardedHitModifiers(target.m_idSelf, attacker.m_idSelf, damage);");
    }

    [Test]
    public void TemporaryHitPointDamageFeedback_IsSentBeforeEngineDamageIsApplied()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var abilitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));
        var damageRollSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));

        combatSource.Should().Contain("public static void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage)");
        combatSource.Should().Contain("GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints");
        abilitySource.Should().Contain("Combat.SendTemporaryHitPointDamageFeedback(activator, target, damage);");
        damageRollSource.Should().Contain("Combat.SendTemporaryHitPointDamageFeedback(attacker.m_idSelf, defender.m_idSelf, totalDamage);");
    }

    [Test]
    public void NormalDamageMitigation_IsCappedSeparatelyFromExplicitImmunity()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var invincibleSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "InvincibleStatusEffect.cs"));

        combatSource.Should().Contain("MaximumNormalDamageReductionPercent = 95");
        combatSource.Should().Contain("Math.Max(adjustment, -MaximumNormalDamageReductionPercent)");
        combatSource.Should().Contain("HasDamageImmunity(defender, damageType)");
        invincibleSource.Should().Contain("PhysicalDamageTakenPercentAdjustment] = -50");
        invincibleSource.Should().NotContain("StatType.PhysicalDamageImmunity");
        invincibleSource.Should().NotContain("PhysicalDamageTakenPercentAdjustment] = -100");
    }

    [Test]
    public void DamageRoll_FallsBackToCreatureNaturalWeapons()
    {
        var root = FindRepositoryRoot();
        var damageRollSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));

        damageRollSource.Should().Contain("weapon = GetFallbackAttackWeapon(attacker);");
        damageRollSource.Should().Contain("private static CNWSItem GetCreatureNaturalWeapon(CNWSCreature attacker)");
        damageRollSource.Should().Contain("EquipmentSlot.CreatureWeaponRight");
        damageRollSource.Should().Contain("EquipmentSlot.CreatureWeaponLeft");
        damageRollSource.Should().Contain("EquipmentSlot.CreatureWeaponBite");
    }

    [Test]
    public void ModuleWeaponItems_UseUntypedDmgAndSeparateDamageTypeProperty()
    {
        var root = FindRepositoryRoot();
        var weaponBaseItems = SWLOR.Game.Server.Service.Item.WeaponBaseItemTypes
            .Concat(new[]
            {
                BaseItem.CreatureSlashWeapon,
                BaseItem.CreaturePierceWeapon,
                BaseItem.CreatureBludgeonWeapon,
                BaseItem.CreatureSlashPierceWeapon
            })
            .Select(x => (int)x)
            .ToHashSet();
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(ReadJsonText(file));
            var item = document.RootElement;
            if (!TryGetNestedInt(item, "BaseItem", "value", out var baseItem) ||
                !weaponBaseItems.Contains(baseItem) ||
                !item.TryGetProperty("PropertiesList", out var propertiesList) ||
                !propertiesList.TryGetProperty("value", out var properties))
            {
                continue;
            }

            var dmgCount = 0;
            var damageTypeCount = 0;
            var hasTypedDmg = false;
            var hasInvalidDamageType = false;
            foreach (var property in properties.EnumerateArray())
            {
                if (!TryGetNestedInt(property, "PropertyName", "value", out var propertyName))
                {
                    continue;
                }

                if (propertyName == (int)ItemPropertyType.DMG)
                {
                    dmgCount++;
                    if (!TryGetNestedInt(property, "Subtype", "value", out var subtype) ||
                        subtype != 0)
                    {
                        hasTypedDmg = true;
                    }
                }
                else if (propertyName == (int)ItemPropertyType.WeaponDamageType)
                {
                    damageTypeCount++;
                    if (!TryGetNestedInt(property, "Subtype", "value", out var subtype) ||
                        subtype < (int)CombatDamageType.Physical ||
                        subtype > (int)CombatDamageType.Sonic)
                    {
                        hasInvalidDamageType = true;
                    }
                }
            }

            if (dmgCount > 1 || damageTypeCount > 1 || hasTypedDmg || hasInvalidDamageType)
            {
                offenders.Add(Path.GetRelativePath(root.FullName, file));
            }
        }

        offenders.Should().BeEmpty("weapon DMG is a plain amount and WeaponDamageType selects the whole damage calculation type");
    }

    [Test]
    public void CZ220DroidWeapons_UsePhysicalDamageType()
    {
        var root = FindRepositoryRoot();
        foreach (var resref in new[]
                 {
                     "cz220_dr_pistol",
                     "patroldroid_wp",
                     "probedroid_wp",
                     "malfsecdroid_wp",
                     "malfspiddroi_wp"
                 })
        {
            var file = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            using var document = JsonDocument.Parse(ReadJsonText(file));
            var properties = document.RootElement.GetProperty("PropertiesList").GetProperty("value");

            var weaponDamageTypes = properties.EnumerateArray()
                .Where(property =>
                    TryGetNestedInt(property, "PropertyName", "value", out var propertyName) &&
                    propertyName == (int)ItemPropertyType.WeaponDamageType)
                .Select(property => TryGetNestedInt(property, "Subtype", "value", out var subtype) ? subtype : -1)
                .ToList();

            weaponDamageTypes.Should().Equal(
                new[] { (int)CombatDamageType.Physical },
                $"{resref} should not inherit or declare poison damage");
        }
    }

    [Test]
    public void ModuleWeaponEnhancements_UseDmgAndSeparateDamageTypeProperty()
    {
        var root = FindRepositoryRoot();
        var expectedRawDamageAmounts = new Dictionary<string, int>
        {
            ["gimp_tooth"] = 2,
            ["imp_melee_1"] = 2,
            ["imp_melee_2"] = 3,
            ["imp_melee_3"] = 4,
            ["imp_melee_4"] = 5,
            ["imp_melee_5"] = 6,
            ["slug_tooth"] = 3,
            ["wen_dmg_phy1"] = 2,
            ["wen_dmg_phy2"] = 3,
            ["wen_dmg_phy3"] = 4,
            ["womprattooth"] = 4,
        };
        var legacyDamageEnhancementSubtypes = new HashSet<int> { 19, 20, 21, 22, 23 };
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(ReadJsonText(file));
            var item = document.RootElement;
            if (!item.TryGetProperty("PropertiesList", out var propertiesList) ||
                !propertiesList.TryGetProperty("value", out var properties))
            {
                continue;
            }

            var damageEnhancementCount = 0;
            var hasLegacyDamageEnhancementSubtype = false;
            var damageEnhancementAmount = 0;
            var damageTypeCount = 0;
            var hasInvalidDamageType = false;
            var hasStaleElementalDamageName = item.TryGetProperty("LocalizedName", out var localizedName) &&
                                               localizedName.TryGetProperty("value", out var localizedNameValues) &&
                                               localizedNameValues.TryGetProperty("0", out var localizedNameValue) &&
                                               localizedNameValue.GetString()?.Contains("Weapon Enhancement - DMG -") == true;
            foreach (var property in properties.EnumerateArray())
            {
                if (!TryGetNestedInt(property, "PropertyName", "value", out var propertyName))
                    continue;

                if (propertyName == (int)ItemPropertyType.WeaponEnhancement)
                {
                    if (!TryGetNestedInt(property, "Subtype", "value", out var subtype))
                    {
                        continue;
                    }

                    if (legacyDamageEnhancementSubtypes.Contains(subtype))
                    {
                        hasLegacyDamageEnhancementSubtype = true;
                        continue;
                    }

                    if (subtype != (int)EnhancementSubType.DMG)
                        continue;

                    damageEnhancementCount++;
                    if (TryGetNestedInt(property, "CostValue", "value", out var amount))
                        damageEnhancementAmount = amount;
                }
                else if (propertyName == (int)ItemPropertyType.WeaponDamageType)
                {
                    damageTypeCount++;
                    if (!TryGetNestedInt(property, "Subtype", "value", out var subtype) ||
                        subtype < (int)CombatDamageType.Force ||
                        subtype > (int)CombatDamageType.Ice)
                    {
                        hasInvalidDamageType = true;
                    }
                }
            }

            if (damageEnhancementCount <= 0 && !hasLegacyDamageEnhancementSubtype)
                continue;

            var resref = Path.GetFileName(file).Replace(".uti.json", "");
            var isRawDamageEnhancement = damageTypeCount == 0;
            var hasInvalidRawAmount = isRawDamageEnhancement &&
                                      expectedRawDamageAmounts.TryGetValue(resref, out var expectedAmount) &&
                                      damageEnhancementAmount != expectedAmount;

            if (damageEnhancementCount > 1 ||
                damageTypeCount > 1 ||
                hasLegacyDamageEnhancementSubtype ||
                hasInvalidDamageType ||
                hasInvalidRawAmount ||
                hasStaleElementalDamageName)
            {
                offenders.Add(Path.GetRelativePath(root.FullName, file));
            }
        }

        offenders.Should().BeEmpty("enhancement DMG is a plain amount, raw DMG is stronger, and WeaponDamageType selects converted damage");
    }

    [Test]
    public void ModuleResistanceEnhancements_UseRebalancedAmounts()
    {
        var root = FindRepositoryRoot();
        var expectedEnhancements = new Dictionary<string, (int PropertyType, int SubType, int Amount)>();
        var armorAndFoodAmounts = new Dictionary<int, int>
        {
            [1] = 3,
            [2] = 6,
            [3] = 9,
            [4] = 12,
            [5] = 15,
        };
        var droidAmounts = new Dictionary<int, int>
        {
            [1] = 8,
            [2] = 15,
        };

        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_def_fir", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistanceFire, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_def_psn", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistancePoison, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_def_elec", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistanceElectrical, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_def_ice", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistanceIce, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_res_mnd", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistanceMind, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_res_mob", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistanceMobility, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_res_tra", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistanceTrauma, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "aen_res_dis", ItemPropertyType.ArmorEnhancement, EnhancementSubType.ResistanceDisruption, armorAndFoodAmounts);

        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_fir", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusFireResistance, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_psn", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusPoisonResistance, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_elec", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusElectricalResistance, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_ice", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusIceResistance, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_mnd", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusMindResistance, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_mob", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusMobilityResistance, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_tra", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusTraumaResistance, armorAndFoodAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "cen_res_dis", ItemPropertyType.FoodEnhancement, EnhancementSubType.FoodBonusDisruptionResistance, armorAndFoodAmounts);

        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_fir", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistanceFire, droidAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_psn", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistancePoison, droidAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_elec", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistanceElectrical, droidAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_ice", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistanceIce, droidAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_mnd", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistanceMind, droidAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_mob", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistanceMobility, droidAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_tra", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistanceTrauma, droidAmounts);
        AddResistanceEnhancementExpectations(expectedEnhancements, "de_res_dis", ItemPropertyType.DroidEnhancement, EnhancementSubType.DroidResistanceDisruption, droidAmounts);

        var offenders = new List<string>();
        foreach (var (resref, expected) in expectedEnhancements)
        {
            var file = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            using var document = JsonDocument.Parse(ReadJsonText(file));
            var properties = document.RootElement.GetProperty("PropertiesList").GetProperty("value");
            var matchingAmounts = properties.EnumerateArray()
                .Where(property =>
                    TryGetNestedInt(property, "PropertyName", "value", out var propertyName) &&
                    propertyName == expected.PropertyType &&
                    TryGetNestedInt(property, "Subtype", "value", out var subType) &&
                    subType == expected.SubType)
                .Select(property => TryGetNestedInt(property, "CostValue", "value", out var amount) ? amount : -1)
                .ToList();

            if (matchingAmounts.Count != 1 ||
                matchingAmounts[0] != expected.Amount)
            {
                offenders.Add($"{resref}: expected {expected.Amount}, found {string.Join(", ", matchingAmounts)}");
            }
        }

        offenders.Should().BeEmpty("resistance values need to be strong enough to matter under the new curve without crowding out Defense");
    }

    [Test]
    public void BlueprintResistanceBonuses_UseRebalancedRanges()
    {
        var root = FindRepositoryRoot();
        var blueprintBonusesSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "CraftService",
            "BlueprintBonuses.cs"));

        blueprintBonusesSource.Should().Contain("new(14, EnhancementSubType.ResistanceElectrical, 3)");
        blueprintBonusesSource.Should().Contain("new(4, EnhancementSubType.ResistancePoison, 7)");
        blueprintBonusesSource.Should().Contain("new(2, EnhancementSubType.ResistanceDisruption, 9)");
        blueprintBonusesSource.Should().Contain("new(4, EnhancementSubType.FoodBonusElectricalResistance, 3)");
        blueprintBonusesSource.Should().Contain("new(1, EnhancementSubType.FoodBonusPoisonResistance, 7)");
        blueprintBonusesSource.Should().Contain("new(1, EnhancementSubType.FoodBonusDisruptionResistance, 9)");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.ResistanceMind, 1)");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.FoodBonusMindResistance, 1)");
    }

    [Test]
    public void LiveEnhancementDamageDefinitions_DoNotExposeLegacyTypedDamageSubtypes()
    {
        var root = FindRepositoryRoot();
        var enhancementSubTypeSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "CraftService",
            "EnhancementSubType.cs"));
        var enhanceWeapon2da = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "iprp_enhancewpn.2da"));
        var enhancementItemBuilderSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.CLI",
            "EnhancementItemBuilder.cs"));
        var enhancementInputSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.CLI",
            "InputFiles",
            "enhancement_list.tsv"));

        foreach (var legacyName in new[] { "DMGPhysical", "DMGForce", "DMGFire", "DMGPoison", "DMGElectrical", "DMGIce" })
        {
            enhancementSubTypeSource.Should().NotContain(legacyName);
            enhanceWeapon2da.Should().NotContain(legacyName);
            enhancementItemBuilderSource.Should().NotContain(legacyName);
        }

        foreach (var legacyLabel in new[] { "DMG - Physical", "DMG - Force", "DMG - Fire", "DMG - Poison", "DMG - Electrical", "DMG - Ice" })
        {
            enhancementItemBuilderSource.Should().NotContain(legacyLabel);
            enhancementInputSource.Should().NotContain(legacyLabel);
        }

        enhanceWeapon2da.Should().Contain("18    16859985   DMG");
        enhanceWeapon2da.Should().Contain("19    ****       ****");
        enhanceWeapon2da.Should().Contain("20    ****       ****");
        enhanceWeapon2da.Should().Contain("21    ****       ****");
        enhanceWeapon2da.Should().Contain("22    ****       ****");
        enhanceWeapon2da.Should().Contain("23    ****       ****");
    }

    [Test]
    public void WeaponDamageType2da_IsAvailableOnWeaponsAndEnhancementItems()
    {
        var root = FindRepositoryRoot();
        var itemPropsLines = File.ReadAllLines(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "itemprops.2da"));
        var header = Split2daColumns(itemPropsLines.First(x => x.Contains("0_Melee")));
        var row = Split2daColumns(itemPropsLines.First(x => x.StartsWith("134")));
        var valuesByColumn = new Dictionary<string, string>();

        for (var index = 0; index < header.Length; index++)
        {
            valuesByColumn[header[index]] = row[index + 1];
        }

        foreach (var column in new[] { "0_Melee", "1_Ranged", "2_Thrown", "3_Staves", "4_Rods", "5_Ammo", "14_Claw", "15_Misc_Uneq", "16_Misc", "21_Glove" })
        {
            valuesByColumn[column].Should().Be("1", $"WeaponDamageType must be assignable wherever weapon DMG or enhancement items use it ({column})");
        }

        valuesByColumn["StringRef"].Should().Be("16859986");

        var itemPropDefLines = File.ReadAllLines(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "itempropdef.2da"));
        Split2daColumns(itemPropDefLines.First(x => x.StartsWith("134")))
            .Should()
            .Contain(new[] { "16859986", "WeaponDamageType", "iprp_c_dmgtype", "16859986" });
    }

    [Test]
    public void BlueprintDamageBonuses_UseDmgAndSeparateDamageTypeProperty()
    {
        var root = FindRepositoryRoot();
        var blueprintBonusesSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "CraftService",
            "BlueprintBonuses.cs"));

        blueprintBonusesSource.Should().Contain("CombatDamageType.Fire");
        blueprintBonusesSource.Should().Contain("new(3, EnhancementSubType.DMG, 2)");
        blueprintBonusesSource.Should().Contain("new(3, EnhancementSubType.DMG, 1, CombatDamageType.Fire)");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.DMGPhysical");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.DMGForce");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.DMGFire");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.DMGPoison");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.DMGElectrical");
        blueprintBonusesSource.Should().NotContain("EnhancementSubType.DMGIce");
    }

    [Test]
    public void WeaponDamageTypeMigration_CoversLivePlayerInventoryAndSerializedItems()
    {
        var root = FindRepositoryRoot();
        var playerMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "PlayerMigration",
            "_14_MigrateResistanceItemProperties.cs"));
        var serverMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_31_MigrateResistanceItemProperties.cs"));
        var weaponMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));

        playerMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(player);");
        serverMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateSerializedObject");
        weaponMigrationSource.Should().Contain("ItemPropertyType.DMG");
        weaponMigrationSource.Should().Contain("ItemPropertyType.WeaponDamageType");
        weaponMigrationSource.Should().Contain("ItemPropertyType.WeaponEnhancement");
        weaponMigrationSource.Should().Contain("RawDamageEnhancementAmountsByResref");
        weaponMigrationSource.Should().Contain("LegacyEnhancementDamageTypesBySubType");
        weaponMigrationSource.Should().Contain("[19] = CombatDamageType.Force");
        weaponMigrationSource.Should().Contain("[20] = CombatDamageType.Fire");
        weaponMigrationSource.Should().Contain("[21] = CombatDamageType.Poison");
        weaponMigrationSource.Should().Contain("[22] = CombatDamageType.Electrical");
        weaponMigrationSource.Should().Contain("[23] = CombatDamageType.Ice");
        weaponMigrationSource.Should().Contain("GetHasInventory(obj)");
        weaponMigrationSource.Should().Contain("GetItemInSlot((InventorySlot)index, creature)");
    }

    [Test]
    public void ResistanceMigration_RebalancesStoredEnhancementItems()
    {
        var root = FindRepositoryRoot();
        var playerMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "PlayerMigration",
            "_14_MigrateResistanceItemProperties.cs"));
        var serverMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_31_MigrateResistanceItemProperties.cs"));
        var resistanceMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemResistanceMigration.cs"));

        playerMigrationSource.Should().Contain("SerializedItemResistanceMigration.MigrateObject(player);");
        serverMigrationSource.Should().Contain("SerializedItemResistanceMigration.MigrateSerializedObject");
        resistanceMigrationSource.Should().Contain("RebalanceResistanceEnhancementItem");
        resistanceMigrationSource.Should().Contain("ArmorAndFoodResistanceAmountByRank");
        resistanceMigrationSource.Should().Contain("[5] = 15");
        resistanceMigrationSource.Should().Contain("DroidResistanceAmountByRank");
        resistanceMigrationSource.Should().Contain("[2] = 15");
        resistanceMigrationSource.Should().Contain("aen_def_fir");
        resistanceMigrationSource.Should().Contain("cen_res_mnd");
        resistanceMigrationSource.Should().Contain("de_res_dis");
    }

    [Test]
    public void IsWeaponSkillType_UsesSkillCombatPointMetadata()
    {
        Combat.IsWeaponSkillType(SkillType.Lightsaber).Should().BeTrue();
        Combat.IsWeaponSkillType(SkillType.Rifle).Should().BeTrue();
        Combat.IsWeaponSkillType(SkillType.Force).Should().BeFalse();
        Combat.IsWeaponSkillType(SkillType.Devices).Should().BeFalse();
        Combat.IsWeaponSkillType(SkillType.Invalid).Should().BeFalse();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static string ReadJsonText(string file)
    {
        var bytes = File.ReadAllBytes(file);
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static bool TryGetNestedInt(JsonElement element, string property, string nestedProperty, out int value)
    {
        value = 0;
        return element.TryGetProperty(property, out var propertyElement) &&
               propertyElement.TryGetProperty(nestedProperty, out var nestedElement) &&
               nestedElement.TryGetInt32(out value);
    }

    private static string[] Split2daColumns(string line)
    {
        return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static void AddResistanceEnhancementExpectations(
        Dictionary<string, (int PropertyType, int SubType, int Amount)> expectedEnhancements,
        string resrefPrefix,
        ItemPropertyType propertyType,
        EnhancementSubType subType,
        IReadOnlyDictionary<int, int> amountsByRank)
    {
        foreach (var (rank, amount) in amountsByRank)
        {
            expectedEnhancements[$"{resrefPrefix}{rank}"] = ((int)propertyType, (int)subType, amount);
        }
    }
}
