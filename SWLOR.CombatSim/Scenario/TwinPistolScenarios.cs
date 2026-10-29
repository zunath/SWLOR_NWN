using SWLOR.CombatSim.Data;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CombatSim.Scenario;

/// <summary>
/// Reproducible Twin line comparisons. These are effective-stat fixtures, not NWN equipment,
/// projectile or character-build verification. The production engine tests provide
/// the equal-investment Pistol baseline; the generic simulator does not model all Pistol procs.
/// </summary>
public static class TwinPistolScenarios
{
    public static ScenarioSpec Create(bool crossfire, bool matchedRhythm, float duration = 120,
        int rank = 50, int targets = 1, bool pvp = false, bool incomingPressure = false,
        int haste = 0, AbilityCatalog catalog = null)
    {
        // Each line owns one handling slot. Crossfire may swap Steady Cadence for Matched Rhythm; Steady Cadence
        // only adds to charges the Crossfire Cadence trait creates, so Barrage always keeps Matched Rhythm.
        if (!crossfire && !matchedRhythm)
            throw new ArgumentException("Steady Cadence needs the Crossfire Cadence trait; Barrage builds use Matched Rhythm.", nameof(matchedRhythm));
        catalog ??= AbilityCatalog.Load();
        var category = crossfire ? PerkCategoryType.TwinPistolCrossfire : PerkCategoryType.TwinPistolBarrage;
        var handling = matchedRhythm ? PerkType.MatchedRhythm : PerkType.SteadyCadence;
        var selected = catalog.Perks.InCategory(category).Where(perk =>
            perk.Type is not (PerkType.MatchedRhythm or PerkType.SteadyCadence)).Append(catalog.Perks.Get(handling));
        var perks = new List<string>();
        var abilities = new List<AbilityUseSpec>();
        foreach (var perk in selected)
        {
            var level = perk.PerkLevels.Where(level => level.Value.Requirements.OfType<PerkRequirementSkill>()
                    .All(requirement => requirement.RequiredRank <= rank))
                .OrderByDescending(level => level.Key).FirstOrDefault();
            if (level.Value == null) continue;
            perks.Add($"{perk.Type}:{level.Key}");
            foreach (var feat in level.Value.GrantedFeats.Where(catalog.Abilities.ContainsKey))
            {
                var info = catalog.Get(feat);
                var spender = info.TwinPistol != null && !info.KeepsWeaponAbilityCadence;
                abilities.Add(new AbilityUseSpec
                {
                    Feat = feat.ToString(),
                    Target = info.IsHostile ? AbilityTarget.Enemy : AbilityTarget.Self,
                    KeepUp = !info.IsHostile,
                    // Final Salvo waits for a full chain; Closing Shot fires on any Cadence.
                    MinimumCadence = !spender ? 0 : feat == SWLOR.NWN.API.NWScript.Enum.FeatType.FinalSalvo ? 3 : 1
                });
            }
        }
        var priority = crossfire
            ? new[] { "CrossfireStance", "TempoShot", "LeadShot", "FinalSalvo", "ClosingShot" }
            : new[] { "AkimboStance", "TwinSuns", "PairedVolley", "FocusedBurst", "ControlledBurst" };
        abilities = abilities.OrderBy(use => Array.FindIndex(priority, prefix => use.Feat.StartsWith(prefix, StringComparison.Ordinal))).ToList();
        return new ScenarioSpec
        {
            Name = $"Twin {(crossfire ? "Crossfire" : "Barrage")} {(matchedRhythm ? "Rhythm" : "Steady Cadence")} rank {rank}",
            Description = "Production Twin payloads, trait state and combat formulas; explicit effective ratings ATT150/ACC150, DMG22 per hand, Delay250 per hand, attributes26. " +
                "60 SP line at rank50 with mastery unlocks assumed; profile includes 30% offhand delay reduction (the common 9 SP Armor investment). " +
                "Finite61STM,20STM/30s; fixed targets HP30000/DEF94/EVA134/VIT26; no Guard, critical mitigation, item procs or terrain. " +
                "Impacts use logical timing, not native animation/projectile scheduling. Crossfire chains Tempo Shot, Lead Shot, then spends with " +
                "Final Salvo at three Cadence or Closing Shot at one or more; these thresholds are a chosen policy, not a gameplay requirement. " +
                "Other perk families fail closed. Equal-SP Pistol comparison requires WeaponRotationBalance native tests.",
            DurationSeconds = duration,
            TimeStepSeconds = 0.05f,
            IsPvp = pvp,
            Teams = new List<TeamSpec>
            {
                new() { Name = "Twin", Members = new List<CombatantSpec>
                {
                    new()
                    {
                        Name = "Twin", MaxHp = 30000, MaxStm = 61, StmRegenPer30Seconds = 20,
                        ReactionSeconds = 0.5f, Perks = perks, Abilities = abilities,
                        WeaponLoadout = new WeaponLoadoutSpec
                        {
                            MainDamage = 22, OffhandDamage = 22, MainDelay = 250, OffhandDelay = 250,
                            OffhandDelayReductionPercent = 30, Attack = 150, Accuracy = 150,
                            SkillRank = rank, HastePercent = haste
                        }
                    }
                } },
                new() { Name = "Targets", Members = Enumerable.Range(1, targets).Select(index => new CombatantSpec
                {
                    Name = $"Target {index}", IsNpc = !pvp, MaxHp = 30000,
                    Defense = 94, Evasion = 134, Vitality = 26,
                    WeaponLoadout = incomingPressure ? new WeaponLoadoutSpec { MainDamage = 8, Accuracy = 125 } : null
                }).ToList() }
            }
        };
    }
}
