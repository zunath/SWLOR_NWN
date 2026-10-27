using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The space NPC ship presets (RevampNpcShipCatalog): coverage, the status built from an entry, and where a
/// preset ship's shot math differs from a player's.
/// </summary>
public class NpcShipPresetTests
{
    private static ShipNpcEntry Entry(string tag) => RevampNpcShipCatalog.Ships.Single(x => x.CreatureTag == tag);

    private static ShipModuleStep Step(string tag) => RevampModuleCatalog.Steps.Single(x => x.Tag == tag);

    [Test]
    public void Catalog_CoversExactlyTheRegisteredNpcCreatureTags()
    {
        var registered = new NPCSpaceObjectDefinition().BuildSpaceObjects().Keys;

        RevampNpcShipCatalog.Ships.Select(x => x.CreatureTag).Should().OnlyHaveUniqueItems();
        RevampNpcShipCatalog.Ships.Select(x => x.CreatureTag).Should().BeEquivalentTo(registered);
    }

    [Test]
    public void Catalog_WeaponsAreRevampHighRowWeaponModules()
    {
        foreach (var entry in RevampNpcShipCatalog.Ships)
        {
            entry.Size.Should().NotBe(ShipSize.Invalid, entry.CreatureTag);
            entry.Weapons.Length.Should().BeLessThanOrEqualTo(10, entry.CreatureTag);
            foreach (var weapon in entry.Weapons)
            {
                var step = RevampModuleCatalog.Steps.SingleOrDefault(x => x.Tag == weapon.ModuleTag);
                step.Should().NotBeNull($"{entry.CreatureTag} fits {weapon.ModuleTag}");
                step!.Dmg.Should().BePositive(weapon.ModuleTag);
                step.Row.Should().Be(ShipModulePowerType.High, weapon.ModuleTag);
                weapon.Dmg.Should().BePositive($"{entry.CreatureTag} {weapon.ModuleTag}");
            }
        }
    }

    [Test]
    public void BuildStatus_CopiesTheEntryAndFitsWeaponsInHighSlotsInOrder()
    {
        var entry = Entry("t1bomber");
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var status = ShipNpcRules.BuildStatus(entry, "NPC_Bomber1", false, now);

        status.ItemTag.Should().Be("NPC_Bomber1");
        status.CapitalShip.Should().BeFalse();
        status.Size.Should().Be(entry.Size);
        (status.MaxHull, status.Hull).Should().Be((entry.Hull, entry.Hull));
        (status.MaxShield, status.Shield).Should().Be((entry.Shield, entry.Shield));
        (status.MaxCapacitor, status.Capacitor).Should().Be((ShipNpcRules.Capacitor, ShipNpcRules.Capacitor));
        status.CapacitorRegenPerSecond.Should().Be(ShipNpcRules.CapacitorRegenPerSecond);
        (status.Attack, status.Accuracy, status.Defense, status.Evasion)
            .Should().Be((entry.Attack, entry.Accuracy, entry.Defense, entry.Evasion));

        status.Resistances[ResistanceType.Thermal].Should().Be(entry.ResistAll);
        status.Resistances[ResistanceType.EM].Should().Be(entry.ResistAll);
        status.Resistances[ResistanceType.Explosive].Should().Be(entry.ResistAll);

        status.IsPresetNpc.Should().BeTrue();
        status.NpcPilot.Should().BeEquivalentTo(new ShipNpcPilot
        {
            Agility = entry.Agility,
            Perception = entry.Perception,
            Vitality = entry.Vitality,
            Willpower = entry.Willpower,
            Might = entry.Might,
            ShieldRegenPerSecond = entry.ShieldRegenPerSecond,
        });

        status.HighPowerModules.Keys.Should().Equal(1, 2);
        status.HighPowerModules[1].ItemTag.Should().Be(entry.Weapons[0].ModuleTag);
        status.HighPowerModules[2].ItemTag.Should().Be(entry.Weapons[1].ModuleTag);
        status.HighPowerModules[1].RecastTime.Should().Be(now);
        status.HighPowerModules[2].NpcShot.Should().BeEquivalentTo(entry.Weapons[1]);
        status.MidPowerModules.Should().BeEmpty();
        status.LowPowerModules.Should().BeEmpty();
    }

    [Test]
    public void BuildStatus_EveryEntryBuilds()
    {
        foreach (var entry in RevampNpcShipCatalog.Ships)
        {
            var status = ShipNpcRules.BuildStatus(entry, "tag", entry.Size == ShipSize.Capital, DateTime.UtcNow);

            status.HighPowerModules.Should().HaveCount(entry.Weapons.Length, entry.CreatureTag);
            status.MaxHull.Should().BePositive(entry.CreatureTag);
            status.IsPresetNpc.Should().BeTrue(entry.CreatureTag);
        }
    }

    [Test]
    public void FindNpcShot_ReturnsTheFittedWeaponsNumbersByModuleTag()
    {
        var entry = Entry("t1bomber");
        var status = ShipNpcRules.BuildStatus(entry, "NPC_Bomber1", false, DateTime.UtcNow);

        status.FindNpcShot("sm_missl_00").Should().BeEquivalentTo(entry.Weapons.Single(x => x.ModuleTag == "sm_missl_00"));
        status.FindNpcShot("sm_laser_00").Should().BeNull();
        new ShipStatus().FindNpcShot("sm_missl_00").Should().BeNull();
    }

    [Test]
    public void Shot_UsesTheFittedWeaponsNumbersWithoutSizeScaling()
    {
        var step = Step("sm_laser_00");
        var npcShot = new ShipNpcWeapon { ModuleTag = step.Tag, Dmg = 37, Attack = 61, Accuracy = 44, Rank = 22 };

        var shot = ShipShotBuilder.Build(step, ShipSize.Large, ShipSize.Large, 10, npcShot);

        shot.Dmg.Should().Be(37, "a preset weapon's Dmg is not scaled by the hull size");
        shot.Attack.Should().Be(61);
        shot.Accuracy.Should().Be(44);
        shot.Rank.Should().Be(22);
        shot.Family.Should().Be(step.WeaponFamily);
        shot.DamageType.Should().Be(step.DamageType);
        shot.Size.Should().Be(step.FixedSize);
        shot.ShieldBypass.Should().Be(step.ShieldBypass);
        shot.IgnoreSizePenalty.Should().Be(step.IgnoreSizePenalty);
        shot.ShieldDamageMultiplier.Should().Be(step.ShieldDamageMultiplier);
    }

    [Test]
    public void Shot_WithoutAnOverrideIsUnchanged()
    {
        var step = Step("sm_laser_00");

        var shot = ShipShotBuilder.Build(step, ShipSize.Large, ShipSize.Large, 10);

        shot.Dmg.Should().Be((int)Math.Round(step.Dmg * ShipStatCalculator.WeaponSizeMultiplier(ShipSize.Large), MidpointRounding.AwayFromZero));
        shot.Rank.Should().Be(step.Rank);
    }

    [Test]
    public void Shot_KeepsTheRidersOnThePresetDamage()
    {
        var missile = new ShipNpcWeapon { ModuleTag = "sm_missl_00", Dmg = 12, Attack = 16, Accuracy = 10, Rank = 5 };
        var pointDefense = new ShipNpcWeapon { ModuleTag = "sm_pdef_00", Dmg = 21, Attack = 43, Accuracy = 30, Rank = 15 };

        ShipShotBuilder.Build(Step("sm_missl_00"), ShipSize.Small, ShipSize.Medium, 0, missile).Dmg.Should().Be(14, "+20% against no shield");
        ShipShotBuilder.Build(Step("sm_missl_00"), ShipSize.Small, ShipSize.Medium, 5, missile).Dmg.Should().Be(12);
        ShipShotBuilder.Build(Step("sm_pdef_00"), ShipSize.Capital, ShipSize.Small, 5, pointDefense).Dmg.Should().Be(25, "+20% against Small");
        ShipShotBuilder.Build(Step("sm_pdef_00"), ShipSize.Capital, ShipSize.Large, 5, pointDefense).Dmg.Should().Be(21);
    }

    [Test]
    public void PilotScore_ComesFromThePresetOnlyForPresetShips()
    {
        var entry = Entry("t1interceptor");
        var preset = ShipNpcRules.BuildStatus(entry, "tag", false, DateTime.UtcNow);
        var creature = new ShipStatus();

        int Ten(AbilityType _) => 10;

        ShipNpcRules.PilotScore(preset, AbilityType.Agility, Ten).Should().Be(entry.Agility);
        ShipNpcRules.PilotScore(preset, AbilityType.Perception, Ten).Should().Be(entry.Perception);
        ShipNpcRules.PilotScore(preset, AbilityType.Vitality, Ten).Should().Be(entry.Vitality);
        ShipNpcRules.PilotScore(preset, AbilityType.Willpower, Ten).Should().Be(entry.Willpower);
        ShipNpcRules.PilotScore(preset, AbilityType.Might, Ten).Should().Be(entry.Might);
        ShipNpcRules.PilotScore(preset, AbilityType.Social, Ten).Should().Be(10);
        ShipNpcRules.PilotScore(creature, AbilityType.Agility, _ => 33).Should().Be(33);
        ShipNpcRules.PilotScore(null, AbilityType.Agility, _ => 34).Should().Be(34);
    }

    [Test]
    public void PresetShotInputs_UsePilotAttributesAndNoLevelTerm()
    {
        var entry = Entry("t1interceptor");
        var preset = ShipNpcRules.BuildStatus(entry, "tag", false, DateTime.UtcNow);
        var shot = ShipShotBuilder.Build(Step("sm_laser_00"), preset.Size, ShipSize.Small, 10, preset.FindNpcShot("sm_laser_00"));
        int Ten(AbilityType _) => 10;

        var accuracy = SpaceCombatMath.Accuracy(
            preset.Accuracy,
            shot.Accuracy,
            ShipNpcRules.PilotScore(preset, AbilityType.Agility, Ten),
            ShipNpcRules.PilotScore(preset, AbilityType.Perception, Ten),
            shot.Family);
        var attack = SpaceCombatMath.Attack(
            preset.Attack,
            shot.Attack,
            SpaceCombatMath.DamageAttribute(shot.Family, ShipNpcRules.PilotScore(preset, AbilityType.Perception, Ten), ShipNpcRules.PilotScore(preset, AbilityType.Might, Ten)));

        accuracy.Should().Be(entry.Accuracy + entry.Weapons[0].Accuracy + entry.Agility * 3);
        attack.Should().Be(entry.Attack + entry.Weapons[0].Attack + entry.Perception);
        SpaceCombatMath.Evasion(preset.Evasion, ShipNpcRules.PilotScore(preset, AbilityType.Agility, Ten)).Should().Be(entry.Evasion + entry.Agility * 3);
        SpaceCombatMath.Defense(preset.Defense, ShipNpcRules.PilotScore(preset, AbilityType.Vitality, Ten)).Should().Be(entry.Defense + entry.Vitality);

        ShipNpcRules.UsesLevelTerm(false, preset).Should().BeFalse("a preset ship's values already include its level");
    }

    [Test]
    public void LevelTerm_AppliesOnlyToLegacyNpcShips()
    {
        var preset = ShipNpcRules.BuildStatus(Entry("t1fighter"), "tag", false, DateTime.UtcNow);

        ShipNpcRules.UsesLevelTerm(true, new ShipStatus()).Should().BeFalse("players have none");
        ShipNpcRules.UsesLevelTerm(false, new ShipStatus()).Should().BeTrue("a legacy NPC ship keeps it");
        ShipNpcRules.UsesLevelTerm(false, null).Should().BeTrue();
        ShipNpcRules.UsesLevelTerm(false, preset).Should().BeFalse();
    }

    [Test]
    public void ShieldRegen_PresetShipsUseTheirOwnRate()
    {
        var entry = Entry("t1gunship");
        var preset = ShipNpcRules.BuildStatus(entry, "tag", false, DateTime.UtcNow);
        var player = new ShipStatus { Size = ShipSize.Medium, ShieldRegenBonusPercent = 10 };

        ShipNpcRules.ShieldRegenPerSecond(preset, preset.MaxShield, 99).Should().Be(entry.ShieldRegenPerSecond);
        ShipNpcRules.ShieldRegenPerSecond(player, 1000, 14).Should().Be(SpaceCombatMath.ShieldRegenPerSecond(1000, ShipSize.Medium, 14, 10));
    }

    [TestCase(ShipSize.Small, false)]
    [TestCase(ShipSize.Medium, false)]
    [TestCase(ShipSize.Large, true)]
    [TestCase(ShipSize.Capital, true)]
    public void Torpedo_FromAPresetShipTargetsOnlyLargeOrCapital(ShipSize targetSize, bool allowed)
    {
        var preset = ShipNpcRules.BuildStatus(Entry("t1bomber"), "tag", false, DateTime.UtcNow);

        var error = ShipNpcRules.TargetError(ShipModuleFamily.TorpedoLauncher, preset, targetSize);

        error.Should().Be(allowed ? string.Empty : ShipNpcRules.TorpedoTargetError);
    }

    [Test]
    public void TargetError_LeavesOtherWeaponsAndPlayerTorpedoesAlone()
    {
        var preset = ShipNpcRules.BuildStatus(Entry("t1bomber"), "tag", false, DateTime.UtcNow);

        ShipNpcRules.TargetError(ShipModuleFamily.MissileLauncher, preset, ShipSize.Small).Should().BeEmpty();
        ShipNpcRules.TargetError(ShipModuleFamily.TorpedoLauncher, new ShipStatus(), ShipSize.Small).Should().BeEmpty();
    }

    [TestCase(ShipModuleType.TurboLaser)]
    [TestCase(ShipModuleType.CombatLaser)]
    [TestCase(ShipModuleType.IonCannon)]
    [TestCase(ShipModuleType.Missile)]
    [TestCase(ShipModuleType.ProtonBomb)]
    public void NpcWeaponModuleTypes_AreOffensive(ShipModuleType type)
    {
        ShipToggleRules.IsOffensive(type).Should().BeTrue();
    }

    [Test]
    public void RevampWeaponFamilies_MapToOffensiveModuleTypes()
    {
        var presetWeaponFamilies = RevampNpcShipCatalog.Ships
            .SelectMany(x => x.Weapons)
            .Select(x => RevampModuleCatalog.Steps.Single(step => step.Tag == x.ModuleTag).Family)
            .Distinct();

        presetWeaponFamilies.Should().BeEquivalentTo(new[]
        {
            ShipModuleFamily.LaserCannon,
            ShipModuleFamily.IonCannon,
            ShipModuleFamily.MissileLauncher,
            ShipModuleFamily.PointDefenseTurret,
            ShipModuleFamily.TorpedoLauncher,
            ShipModuleFamily.Turbolaser,
        });
    }

    [TestCase(ShipModuleType.HullRepairer)]
    [TestCase(ShipModuleType.ShieldRepairer)]
    [TestCase(ShipModuleType.MiningLaser)]
    public void SupportModuleTypes_AreNotOffensive(ShipModuleType type)
    {
        ShipToggleRules.IsOffensive(type).Should().BeFalse();
    }

    [Test]
    public void ShipStatus_PresetFieldsSurviveTheSaveRoundTrip()
    {
        var status = ShipNpcRules.BuildStatus(Entry("t1bomber"), "tag", false, DateTime.UtcNow);

        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<ShipStatus>(Newtonsoft.Json.JsonConvert.SerializeObject(status))!;

        loaded.IsPresetNpc.Should().BeTrue();
        loaded.NpcPilot.Should().BeEquivalentTo(status.NpcPilot);
        loaded.HighPowerModules[2].NpcShot.Should().BeEquivalentTo(status.HighPowerModules[2].NpcShot);
        Newtonsoft.Json.JsonConvert.SerializeObject(new ShipStatus()).Should().NotContain("IsPresetNpc");
    }
}
