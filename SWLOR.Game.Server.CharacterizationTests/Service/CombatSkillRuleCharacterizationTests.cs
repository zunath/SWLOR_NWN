using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks how <see cref="Combat"/> classifies skills (weapon, melee, ranged) and the rules that decide whether a
/// stat source applies to a skill. These feed hit, damage and cost calculations, so a change here moves combat.
/// </summary>
public class CombatSkillRuleCharacterizationTests
{
    private static readonly SkillType[] RepresentativeSkills =
    {
        SkillType.Invalid, SkillType.Force, SkillType.Armor, SkillType.Devices, SkillType.Vibroblade,
        SkillType.Lightsaber, SkillType.Pistol, SkillType.Rifle, SkillType.Unarmed, SkillType.BeastMastery
    };

    [Test]
    public void SkillClassification()
    {
        var table = new SnapshotTable(
            "Combat.IsWeaponSkillType / IsMeleeWeaponSkill / IsRangedWeaponSkill / IsRangedDamageSkill / GetWeaponEngagementRange / IncludesCombatImpactWeaponDamage",
            "How each skill is classified for combat: weapon skill, melee or ranged weapon, ranged damage source, engagement range, and whether an ability impact adds weapon damage.",
            "Every SkillType value, in numeric order. The last two columns are IncludesCombatImpactWeaponDamage without and with a queued natural weapon.",
            new[] { "skill", "skillId" },
            new[]
            {
                "isWeaponSkill", "isMeleeWeaponSkill", "isRangedWeaponSkill", "isRangedDamageSkill", "engagementRange",
                "impactWeaponDamage", "impactWeaponDamageNaturalWeapon"
            });

        foreach (var skill in Enum.GetValues<SkillType>().OrderBy(skill => (int)skill))
        {
            table.Add(
                skill, (int)skill,
                Combat.IsWeaponSkillType(skill),
                Combat.IsMeleeWeaponSkill(skill),
                Combat.IsRangedWeaponSkill(skill),
                Combat.IsRangedDamageSkill(skill),
                Combat.GetWeaponEngagementRange(skill),
                Combat.IncludesCombatImpactWeaponDamage(skill, false),
                Combat.IncludesCombatImpactWeaponDamage(skill, true));
        }

        Snapshot.Verify("Combat", "SkillClassification", table);
    }

    [Test]
    public void CanWeaponSkillTriggerAbility()
    {
        var table = new SnapshotTable(
            "Combat.CanWeaponSkillTriggerAbility",
            "Whether an item of one skill can deliver an ability of another: weapon abilities need a weapon, other abilities accept any item.",
            "Representative skills crossed with themselves: Invalid, Force, Armor, Devices, Vibroblade, Lightsaber, Pistol, Rifle, Unarmed, BeastMastery.",
            new[] { "weaponSkill", "abilitySkill" },
            new[] { "canTrigger" });

        foreach (var weaponSkill in RepresentativeSkills)
        {
            foreach (var abilitySkill in RepresentativeSkills)
                table.Add(weaponSkill, abilitySkill, Combat.CanWeaponSkillTriggerAbility(weaponSkill, abilitySkill));
        }

        Snapshot.Verify("Combat", "CanWeaponSkillTriggerAbility", table);
    }

    [Test]
    public void CanTriggerAutoAttackSplash()
    {
        var table = new SnapshotTable(
            "Combat.CanTriggerAutoAttackSplash",
            "Whether an auto-attack splash source applies to a skill: needs positive damage, chance and radius, more than one target, a weapon skill and a matching or global skill selector.",
            "Base source damage 10, chance 50, radius 5, max targets 3, no selector; then one field changed at a time (damage 0 or -1, chance 0, radius 0, " +
            "max targets 1 or 2, selector Pistol, Rifle or an undefined id) x skill {Pistol, Rifle, Force, Invalid}.",
            new[] { "damage", "chance", "radiusMeters", "maximumTargets", "skillSelector", "skill" },
            new[] { "canTrigger" });

        var variants = new (int Damage, int Chance, int Radius, int MaxTargets, int Selector)[]
        {
            (10, 50, 5, 3, 0), (0, 50, 5, 3, 0), (-1, 50, 5, 3, 0), (10, 0, 5, 3, 0), (10, 50, 0, 3, 0),
            (10, 50, 5, 1, 0), (10, 50, 5, 2, 0), (10, 50, 5, 3, (int)SkillType.Pistol),
            (10, 50, 5, 3, (int)SkillType.Rifle), (10, 50, 5, 3, 9999)
        };
        foreach (var v in variants)
        {
            var source = new StatAdjustmentSource("splash", new Dictionary<StatType, int>
            {
                [StatType.AutoAttackSplashDamage] = v.Damage,
                [StatType.AutoAttackSplashChance] = v.Chance,
                [StatType.AutoAttackSplashRadiusMeters] = v.Radius,
                [StatType.AutoAttackSplashMaximumTargets] = v.MaxTargets,
                [StatType.AutoAttackSplashSkillType] = v.Selector
            });
            foreach (var skill in new[] { SkillType.Pistol, SkillType.Rifle, SkillType.Force, SkillType.Invalid })
                table.Add(v.Damage, v.Chance, v.Radius, v.MaxTargets, v.Selector, skill, Combat.CanTriggerAutoAttackSplash(source, skill));
        }

        Snapshot.Verify("Combat", "CanTriggerAutoAttackSplash", table);
    }

    [Test]
    public void CanTriggerBleedingTargetAbilitySplash()
    {
        var table = new SnapshotTable(
            "Combat.CanTriggerBleedingTargetAbilitySplash",
            "Whether a bleeding-target ability splash source procs: the target must bleed, the d100 roll must be within the chance (capped at 100), " +
            "the source needs damage, radius and targets, and its skill selector must match.",
            "Base source chance 50, damage 10, radius 5, max targets 2, selector Throwing. Family A: base x roll {0,1,49,50,51,100,101} x bleeding {true,false} x skill {Throwing, Pistol}. " +
            "Family B: chance {0,150,-5} x roll {1,100,101}. Family C: one field removed (damage, radius, targets, selector) or changed (Pistol) at roll 1.",
            new[] { "chance", "damage", "radiusMeters", "maximumTargets", "skillSelector", "skill", "isBleeding", "roll" },
            new[] { "canTrigger" });

        void AddRow(int chance, int damage, int radius, int maxTargets, int selector, SkillType skill, bool bleeding, int roll)
        {
            var source = new StatAdjustmentSource("bleed", new Dictionary<StatType, int>
            {
                [StatType.BleedingTargetAbilitySplashChance] = chance,
                [StatType.BleedingTargetAbilitySplashDamage] = damage,
                [StatType.BleedingTargetAbilitySplashRadiusMeters] = radius,
                [StatType.BleedingTargetAbilitySplashMaximumTargets] = maxTargets,
                [StatType.AbilityDamageToBleedingTargetSkillType] = selector
            });
            table.Add(chance, damage, radius, maxTargets, selector, skill, bleeding,
                roll, Combat.CanTriggerBleedingTargetAbilitySplash(source, skill, bleeding, roll));
        }

        const int throwing = (int)SkillType.Throwing;
        foreach (var roll in new[] { 0, 1, 49, 50, 51, 100, 101 })
        {
            foreach (var bleeding in new[] { true, false })
            {
                foreach (var skill in new[] { SkillType.Throwing, SkillType.Pistol })
                    AddRow(50, 10, 5, 2, throwing, skill, bleeding, roll);
            }
        }

        foreach (var chance in new[] { 0, 150, -5 })
        {
            foreach (var roll in new[] { 1, 100, 101 })
                AddRow(chance, 10, 5, 2, throwing, SkillType.Throwing, true, roll);
        }

        AddRow(50, 0, 5, 2, throwing, SkillType.Throwing, true, 1);
        AddRow(50, 10, 0, 2, throwing, SkillType.Throwing, true, 1);
        AddRow(50, 10, 5, 0, throwing, SkillType.Throwing, true, 1);
        AddRow(50, 10, 5, 2, 0, SkillType.Throwing, true, 1);
        AddRow(50, 10, 5, 2, (int)SkillType.Pistol, SkillType.Throwing, true, 1);

        Snapshot.Verify("Combat", "CanTriggerBleedingTargetAbilitySplash", table);
    }

    [Test]
    public void MatchesStaminaDiscount()
    {
        var table = new SnapshotTable(
            "Combat.MatchesStaminaDiscount",
            "Whether a next-ability stamina discount source applies to a skill: hostile-only and ranged-only flags, and a matching or global skill selector.",
            "hostile-only flag {0,1} x ranged-only flag {0,1} x skill selector {0, Pistol, Force} x skill {Invalid, Pistol, Force, Rifle} x ability is hostile {false,true}.",
            new[] { "hostileOnly", "rangedOnly", "skillSelector", "skill", "isHostileAbility" },
            new[] { "matches" });

        foreach (var hostileOnly in new[] { 0, 1 })
        {
            foreach (var rangedOnly in new[] { 0, 1 })
            {
                foreach (var selector in new[] { 0, (int)SkillType.Pistol, (int)SkillType.Force })
                {
                    var source = new StatAdjustmentSource("discount", new Dictionary<StatType, int>
                    {
                        [StatType.NextSkillAbilityStaminaCostAdjustmentHostileOnly] = hostileOnly,
                        [StatType.NextSkillAbilityStaminaCostAdjustmentRangedOnly] = rangedOnly,
                        [StatType.NextSkillAbilityStaminaCostAdjustmentSkillType] = selector
                    });
                    foreach (var skill in new[] { SkillType.Invalid, SkillType.Pistol, SkillType.Force, SkillType.Rifle })
                    {
                        foreach (var hostile in new[] { false, true })
                        {
                            table.Add(hostileOnly, rangedOnly, selector, skill, hostile,
                                Combat.MatchesStaminaDiscount(source, skill, hostile));
                        }
                    }
                }
            }
        }

        Snapshot.Verify("Combat", "MatchesStaminaDiscount", table);
    }

    [Test]
    public void SumSkillSelectedStatAdjustment()
    {
        var table = new SnapshotTable(
            "Combat.SumSkillSelectedStatAdjustment",
            "Adds up the adjustments of the sources whose skill selector matches the skill. A source with no selector counts for every skill only when asked to.",
            "Three sources: A selects Force and adds 10, B has no selector and adds 7, C selects Pistol and adds 3. Every subset of the sources (bitmask, A=1 B=2 C=4) " +
            "x skill {Force, Pistol, Armor, Invalid} x unselectedSourcesApplyToEverySkill {false,true}.",
            new[] { "sourceMask", "skill", "unselectedSourcesApplyToEverySkill" },
            new[] { "sum" });

        var all = new[]
        {
            BuildSelectedSource("A", (int)SkillType.Force, 10),
            BuildSelectedSource("B", 0, 7),
            BuildSelectedSource("C", (int)SkillType.Pistol, 3)
        };
        for (var mask = 0; mask < 8; mask++)
        {
            var sources = all.Where((_, index) => (mask & (1 << index)) != 0).ToList();
            foreach (var skill in new[] { SkillType.Force, SkillType.Pistol, SkillType.Armor, SkillType.Invalid })
            {
                foreach (var global in new[] { false, true })
                {
                    table.Add(mask, skill, global, Combat.SumSkillSelectedStatAdjustment(
                        sources, skill,
                        StatType.AbilityHitChancePercentAdjustmentSkillType,
                        StatType.AbilityHitChancePercentAdjustment,
                        global));
                }
            }
        }

        Snapshot.Verify("Combat", "SumSkillSelectedStatAdjustment", table);
    }

    private static StatAdjustmentSource BuildSelectedSource(string key, int selector, int adjustment)
    {
        return new StatAdjustmentSource(key, new Dictionary<StatType, int>
        {
            [StatType.AbilityHitChancePercentAdjustmentSkillType] = selector,
            [StatType.AbilityHitChancePercentAdjustment] = adjustment
        });
    }

    private static readonly uint[][] TargetCandidateLists =
    {
        Array.Empty<uint>(), new uint[] { 101, 102, 103 }, new uint[] { 101, 101, 102, 103, 103 }, new uint[] { 105, 101, 105, 102 }
    };

    [Test]
    public void SelectSecondaryDamageTargets()
    {
        var table = new SnapshotTable(
            "Combat.SelectSecondaryDamageTargets",
            "The extra targets of a secondary hit: candidates in order, without the primary target and without duplicates, limited to the budget.",
            "Candidate lists x primary target {0, 101, 999} x secondary budget {-1,0,1,2,10}. Object ids are small stand-in numbers.",
            new[] { "candidates", "primaryTarget", "maximumSecondaryTargets" },
            new[] { "selected" });

        foreach (var candidates in TargetCandidateLists)
        {
            foreach (var primary in new uint[] { 0, 101, 999 })
            {
                foreach (var budget in new[] { -1, 0, 1, 2, 10 })
                    table.Add(candidates, primary, budget, Combat.SelectSecondaryDamageTargets(candidates, primary, budget));
            }
        }

        Snapshot.Verify("Combat", "SelectSecondaryDamageTargets", table);
    }

    [Test]
    public void SelectAutoAttackSplashSecondaryTargets()
    {
        var table = new SnapshotTable(
            "Combat.SelectAutoAttackSplashSecondaryTargets",
            "The extra targets of an auto-attack splash: the same selection, with a budget that counts the primary target.",
            "Candidate lists x primary target {0, 101, 999} x maximum total targets {-1,0,1,2,3,10}. Object ids are small stand-in numbers.",
            new[] { "candidates", "primaryTarget", "maximumTotalTargets" },
            new[] { "selected" });

        foreach (var candidates in TargetCandidateLists)
        {
            foreach (var primary in new uint[] { 0, 101, 999 })
            {
                foreach (var total in new[] { -1, 0, 1, 2, 3, 10 })
                {
                    table.Add(candidates, primary, total,
                        Combat.SelectAutoAttackSplashSecondaryTargets(candidates, primary, total));
                }
            }
        }

        Snapshot.Verify("Combat", "SelectAutoAttackSplashSecondaryTargets", table);
    }
}
