using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.Game.Server.Service.PvpCombatService;
using SWLOR.Game.Server.Service.CombatService;
using Combat = SWLOR.Game.Server.Service.Combat;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using DamageType = SWLOR.CombatSim.Scenario.DamageType;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// Runs one fight on a fixed time step. Damage comes from scenario profiles (calibrated against combat logs);
    /// heals, shields, costs, recasts, regeneration and refunds follow the game's rules under the chosen <see cref="RuleSet"/>.
    /// </summary>
    public sealed partial class Simulation
    {
        internal const double HeartbeatSeconds = 6.0;
        private const int HeartbeatsPerRegenCycle = 5;
        private const float ConvergencePercentPerTick = 4f;
        internal const double ConvergenceTickSeconds = 2.0;
        private const double ConvergenceCooldownSeconds = 45.0;
        private const double ConvergenceAccuracySeconds = 30.0;
        private const float ConvergenceHitChanceBonus = 0.05f;
        private const double ResumeAttackDelaySeconds = 0.1;
        private const double ReviveHealDelaySeconds = 0.1;
        private const double QueuedWeaponAbilitySeconds = 30.0;

        /// <summary>Well above any real attack rate, and small enough for each swing to move the clock.</summary>
        public const float MaximumAttacksPerSecond = 20f;

        public const float MinimumTimeStepSeconds = 0.001f;

        /// <summary>
        /// Each step's events resolve against the state at the step's end, so the step bounds how far any event lands
        /// from its true time; the shipped scenarios all use this resolution.
        /// </summary>
        public const float MaximumTimeStepSeconds = 0.1f;

        public const int MaximumMaxHp = 10_000_000;

        /// <summary>One day of simulated fighting.</summary>
        public const float MaximumDurationSeconds = 86_400f;

        /// <summary>Copies one combatant entry may spawn; far beyond any fight on the server.</summary>
        public const int MaximumCount = 200;
        private const int CoordinatedSampleSize = 3;

        /// <summary>Perk stats the simulator models; listed perks contribute only these.</summary>
        private static readonly StatType[] PerkStatsRead = new[]
        {
            StatType.ForceConvergence,
            StatType.HealingReceivedPercentAdjustment,
            StatType.OutgoingAbilityHealingPercentAdjustment,
            StatType.HealingReceivedStaminaRestore,
            StatType.HealingReceivedStaminaRestoreChance,
            StatType.HealingReceivedStaminaRestoreChanceMaximum,
            StatType.HealingReceivedStaminaRestoreChanceScalingAbility,
            StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent,
            StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds,
            StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility,
            StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent,
            StatType.AbilityStaminaCostFPRestorePercentSkillType,
            StatType.AbilityStaminaCostFPRestorePercent,
            StatType.AbilityFPCostStaminaRestorePercentSkillType,
            StatType.AbilityFPCostStaminaRestorePercent
        }.Concat(Enum.GetValues<StatType>().Where(stat => stat.ToString().StartsWith("PairedRanged", StringComparison.Ordinal)))
         .Concat(new[] { StatType.AttackDelayReductionPercent, StatType.DualWieldAttackDelayReductionPercent, StatType.OrdinaryAttackDamagePercentAdjustment,
             StatType.OrdinaryAttackHasteRampPerStack, StatType.OrdinaryAttackHasteRampMaximumStacks,
             StatType.OrdinaryAttackHasteRampGrantCooldownSeconds, StatType.OrdinaryAttackHasteRampDurationSeconds,
             StatType.Accuracy, StatType.MovementHitWeaponAbilityDamagePerCharge })
         .Distinct().ToArray();

        internal static bool SupportsPerkStat(StatType stat) => PerkStatsRead.Contains(stat);

        // Combat's MaximumCrossResourceRestorePercent: Conduit never returns more than this share of a cost.
        private const int MaximumCrossResourceRestorePercent = 95;

        private readonly ScenarioSpec _scenario;
        private readonly RuleSet _rules;
        private readonly HealFormula _healFormula;
        private readonly AbilityCatalog _catalog;
        private readonly Random _random;
        private readonly List<Combatant> _combatants = new();
        private readonly List<DeathRecord> _deaths = new();
        private readonly Dictionary<AbilityUseSpec, (AbilityInfo Info, AbilityEffect Effect)> _resolvedUses = new();
        private readonly Dictionary<int, SharedTarget> _sharedTargets = new();
        private readonly List<PendingHeal> _pendingHeals = new();
        private readonly List<(Combatant Combatant, double Seconds)> _starvationThisStep = new();
        private int _revives;
        private double _now;

        public Simulation(ScenarioSpec scenario, RuleSet rules, AbilityCatalog catalog, int seed)
        {
            _scenario = scenario;
            _rules = rules;
            _healFormula = HealFormula.From(rules);
            _catalog = catalog;
            _random = new Random(seed);
        }

        public double Now
        {
            get => _now;
            internal set => _now = value;
        }
        public IReadOnlyList<Combatant> Combatants => _combatants;

        public SimulationResult Run()
        {
            Spawn();
            var step = _scenario.TimeStepSeconds;
            int? winner = null;

            _now = 0;
            while (true)
            {
                Step(step);

                winner = GetLastTeamStanding();
                if (winner != null)
                {
                    // The fight ends at this step, so the starved interval charged after it never happened.
                    foreach (var (combatant, seconds) in _starvationThisStep)
                        combatant.Stats.StarvedSeconds -= seconds;
                    break;
                }

                if (_now >= _scenario.DurationSeconds)
                    break;

                // The last step lands on the duration itself, so events due before it are still processed.
                _now = Math.Min(_now + step, _scenario.DurationSeconds);
            }

            return new SimulationResult
            {
                EndedAt = Math.Min(_now, _scenario.DurationSeconds),
                WinningTeam = winner,
                Deaths = _deaths,
                Revives = _revives,
                Combatants = _combatants
            };
        }

        public void Spawn()
        {
            // Bounds keep every step moving the clock, so a run always ends.
            if (!(_scenario.TimeStepSeconds >= MinimumTimeStepSeconds) || !(_scenario.TimeStepSeconds <= MaximumTimeStepSeconds))
                throw new InvalidDataException(
                    $"The scenario's TimeStepSeconds must be from {MinimumTimeStepSeconds} to {MaximumTimeStepSeconds}; " +
                    "each step's events resolve against the state at its end, so a longer step would misplace them.");
            if (!(_scenario.DurationSeconds > 0) || !(_scenario.DurationSeconds <= MaximumDurationSeconds))
                throw new InvalidDataException($"The scenario's DurationSeconds must be greater than zero and at most {MaximumDurationSeconds}.");

            for (var teamIndex = 0; teamIndex < _scenario.Teams.Count; teamIndex++)
            {
                // Hand-edited JSON can null out any list; name the gap instead of failing on a null reference.
                var team = _scenario.Teams[teamIndex];
                if (team?.Members == null || team.Members.Any(member => member == null))
                    throw new InvalidDataException($"Team {teamIndex + 1} needs a list of members without empty entries.");
                // An empty side would hand the other a win at the first step.
                if (team.Members.Count == 0)
                    throw new InvalidDataException($"Team '{team.Name}' needs at least one member.");
                foreach (var member in team.Members)
                {
                    if (member.Abilities == null || member.Abilities.Any(use => use == null))
                        throw new InvalidDataException($"'{member.Name}' needs a list of Abilities without empty entries; use [] for none.");
                    if (member.Perks == null)
                        throw new InvalidDataException($"'{member.Name}' needs a list of Perks; use [] for none.");
                }

                var names = new HashSet<string>();
                if (!(team.RetargetSeconds >= 0) || !float.IsFinite(team.RetargetSeconds))
                    throw new InvalidDataException($"Team '{team.Name}' needs a finite RetargetSeconds of at least 0.");
                if (!Enum.IsDefined(_scenario.Teams[teamIndex].TargetPolicy))
                    throw new InvalidDataException(
                        $"Team '{_scenario.Teams[teamIndex].Name}' has an unknown TargetPolicy; use one of {string.Join(", ", Enum.GetNames<TargetPolicy>())}.");

                foreach (var spec in _scenario.Teams[teamIndex].Members)
                {
                    if (!(spec.JoinAtSeconds >= 0) || !float.IsFinite(spec.JoinAtSeconds))
                        throw new InvalidDataException($"'{spec.Name}' needs a finite JoinAtSeconds of at least 0.");

                    var attack = spec.AutoAttack;
                    spec.WeaponLoadout?.Validate();
                    if (spec.WeaponLoadout != null && attack != null)
                        throw new InvalidDataException($"'{spec.Name}' must choose WeaponLoadout or calibrated AutoAttack, not both.");
                    if (attack != null)
                    {
                        if (!float.IsFinite(attack.AttacksPerSecond) || attack.AttacksPerSecond < 0 ||
                            attack.AttacksPerSecond > MaximumAttacksPerSecond)
                            throw new InvalidDataException(
                                $"'{spec.Name}' needs an autoattack AttacksPerSecond from 0 to {MaximumAttacksPerSecond}.");
                        if (attack.AttacksPerSecond > 0)
                            RequireValidDamage($"'{spec.Name}' autoattack", attack.Damage);
                        if (attack.Damage?.WeaponPower != null)
                            throw new InvalidDataException("WeaponPower is an activated weapon damage profile; use WeaponLoadout for native autoattacks.");
                    }

                    // Each step makes at most one decision per combatant, so a step longer than the
                    // reaction time would skip decisions and make results depend on the step size.
                    // The upper bound keeps every healing formula's whole-number result representable.
                    if (!(spec.MaxHp > 0) || spec.MaxHp > MaximumMaxHp)
                        throw new InvalidDataException($"'{spec.Name}' needs a MaxHp from 1 to {MaximumMaxHp}.");
                    var specProblems = spec.GetProblems();
                    if (specProblems.Count > 0)
                        throw new InvalidDataException($"'{spec.Name}': {string.Join("; ", specProblems)}.");

                    if (spec.Abilities.Count > 0 && !float.IsFinite(spec.ReactionSeconds))
                        throw new InvalidDataException($"'{spec.Name}' needs a finite ReactionSeconds.");
                    if (spec.Abilities.Count > 0 && !(spec.ReactionSeconds >= _scenario.TimeStepSeconds))
                        throw new InvalidDataException(
                            $"'{spec.Name}' has a ReactionSeconds of {spec.ReactionSeconds}, below the {_scenario.TimeStepSeconds}s time step; " +
                            "use a TimeStepSeconds no longer than every ability user's ReactionSeconds.");

                    if (spec.Count < 1 || spec.Count > MaximumCount)
                        throw new InvalidDataException($"'{spec.Name}' needs a Count from 1 to {MaximumCount}.");

                    var perkStats = _catalog.Perks.GetStats(spec.Perks, PerkStatsRead);
                    for (var copy = 1; copy <= spec.Count; copy++)
                    {
                        if (string.IsNullOrWhiteSpace(spec.Name))
                            throw new InvalidDataException($"Every member of team '{_scenario.Teams[teamIndex].Name}' needs a Name.");
                        var name = spec.Count > 1 ? $"{spec.Name} {copy}" : spec.Name;
                        if (!names.Add(name))
                            throw new InvalidDataException($"Team '{_scenario.Teams[teamIndex].Name}' has more than one combatant named '{name}'.");
                        var combatant = new Combatant(name, teamIndex, spec, perkStats)
                        {
                            NextHeartbeatAt = _random.NextDouble() * HeartbeatSeconds,
                            // Players arrive partway through the game's five-heartbeat regeneration cycle.
                            HeartbeatCount = _random.Next(HeartbeatsPerRegenCycle),
                            // The game also carries a stamina remainder from 0 to 4 points across heartbeats.
                            StmRegenCarry = _random.Next(HeartbeatsPerRegenCycle) / (double)HeartbeatsPerRegenCycle,
                            // The first swing and decision fall anywhere within their first interval after joining.
                            NextAttackAt = spec.JoinAtSeconds + _random.NextDouble() * AttackInterval(spec),
                            NextActionAt = spec.JoinAtSeconds + _random.NextDouble() * ReactionInterval(spec),
                            AvailableSince = spec.JoinAtSeconds,
                            SimId = (uint)(_combatants.Count + 1)
                        };
                        if (spec.WeaponCombat != null)
                        {
                            combatant.Weapon = new WeaponCombatState(combatant, _catalog.Perks.GetSources(spec.Perks));
                            combatant.NextAttackAt = spec.JoinAtSeconds + (combatant.NextAttackAt - spec.JoinAtSeconds) * combatant.Weapon.SwingSeconds(0) / AttackInterval(spec);
                        }
                        _combatants.Add(combatant);
                        combatant.WeaponTraits.Movement.Observe(DateTime.UnixEpoch.AddSeconds(spec.JoinAtSeconds), 1,
                            combatant.WeaponTraits.Position, false);
                    }

                    foreach (var use in spec.Abilities)
                    {
                        ResolveUse(use);
                        if (use.WeaponPower.HasValue && spec.WeaponCombat == null)
                            throw new InvalidDataException("WeaponPower requires WeaponCombat calibration.");
                        if (spec.WeaponCombat != null && _resolvedUses[use].Info.UnmodeledRiders.Count > 0)
                            throw new InvalidDataException($"'{use.Feat}' has source riders the weapon simulation does not model: {string.Join(", ", _resolvedUses[use].Info.UnmodeledRiders)}.");
                        if (spec.WeaponCombat != null && _resolvedUses[use].Effect is { Kind: EffectKind.SelfStatus } selfStatus)
                            WeaponCombatState.RequireSupportedStatus(selfStatus.StatusType);
                        if (_resolvedUses[use].Info.Technique != null && spec.WeaponCombat?.AllowTechniqueAbilities != true)
                            throw new InvalidDataException("Technique abilities require an eligible weapon calibration.");
                        if (use.Damage?.WeaponPower != null &&
                            (spec.WeaponLoadout == null || !Combat.IsWeaponSkillType(_resolvedUses[use].Info.SkillType)))
                            throw new InvalidDataException($"'{use.Feat}' WeaponPower needs a WeaponLoadout and a weapon skill.");
                        if (use.Damage is { WeaponPower: null } &&
                            Combat.IsWeaponSkillType(_resolvedUses[use].Info.SkillType) &&
                            perkStats.GetValueOrDefault(StatType.MovementHitWeaponAbilityDamagePerCharge) > 0)
                            throw new InvalidDataException($"'{use.Feat}' cannot apply Flow to calibrated final damage; supply explicit authored WeaponPower or omit Flow.");
                        if (_resolvedUses[use].Info.TwinPistol != null && spec.WeaponLoadout == null)
                            throw new InvalidDataException($"'{use.Feat}' needs a WeaponLoadout to resolve its captured hands and traits.");
                        // The loadout autoattack path carries only calibrated queues; a formula queue rides the formula swing.
                        if (_resolvedUses[use].Info.IsQueuedWeapon && use.WeaponPower.HasValue && spec.WeaponLoadout != null)
                            throw new InvalidDataException(
                                $"'{spec.Name}' queues the formula weapon ability '{use.Feat}', which a WeaponLoadout cannot carry.");
                        // Only a landed autoattack can carry a queued weapon ability.
                        if (_resolvedUses[use].Info.IsQueuedWeapon && spec.AutoAttack is not { AttacksPerSecond: > 0 } && spec.WeaponLoadout == null)
                            throw new InvalidDataException(
                                $"'{spec.Name}' queues the weapon ability '{use.Feat}' but has no autoattack to carry it.");
                    }
                    if (spec.WeaponLoadout != null)
                    {
                        var modeledActivePerks = spec.Abilities
                            .Where(use => use.Damage != null || EffectCatalog.TryGet(_resolvedUses[use].Info.Feat, out _))
                            .Select(use => _catalog.Perks.TryGetGrantingPerk(_resolvedUses[use].Info.Feat, out var perk, out _) ? perk.Type : PerkType.Invalid)
                            .Where(perk => perk != PerkType.Invalid).ToArray();
                        _catalog.Perks.ValidateWeaponBuild(spec.Perks, spec.Abilities.Select(use => _resolvedUses[use].Info.Feat),
                            spec.WeaponLoadout.SkillRank, modeledActivePerks);
                    }
                }
            }

            for (var teamIndex = 0; teamIndex < _scenario.Teams.Count; teamIndex++)
                RequireFocusTarget(teamIndex);
            if (_combatants.Any(c => c.Weapon != null) && _combatants.Any(c => c.Weapon == null))
                throw new InvalidDataException("Every combatant in a weapon-calibrated fight needs WeaponCombat ratings.");
            RequireNamedAllies();
            RequireKnownHitChanceTargets();
        }

        // A per-target hit chance that names nobody would silently fall back to the default.
        private void RequireKnownHitChanceTargets()
        {
            foreach (var combatant in _combatants)
            {
                var profiles = combatant.Spec.Abilities.Select(use => (Owner: use.Feat, use.Damage)).ToList();
                if (combatant.Spec.AutoAttack != null)
                    profiles.Add(("autoattack", combatant.Spec.AutoAttack.Damage));

                foreach (var (owner, damage) in profiles.Where(profile => profile.Damage?.HitChanceVs != null))
                {
                    foreach (var name in damage.HitChanceVs.Keys)
                    {
                        var matches = _combatants.Count(c => c.TeamIndex != combatant.TeamIndex && c.Name == name);
                        if (matches == 0)
                            throw new InvalidDataException(
                                $"'{combatant.Name}' {owner} has a HitChanceVs entry for '{name}', which is not an opposing combatant; " +
                                "copies of a combatant with a Count are named '<name> 1', '<name> 2' and so on.");
                        // Opposing teams may share a name, but an override must say which combatant it means.
                        if (matches > 1)
                            throw new InvalidDataException(
                                $"'{combatant.Name}' {owner} has a HitChanceVs entry for '{name}', which names a combatant on more " +
                                "than one opposing team; rename one of them.");
                    }
                }
            }
        }

        // A named ally may join the fight late, but it has to be a combatant on the caster's team.
        private void RequireNamedAllies()
        {
            foreach (var combatant in _combatants)
            {
                foreach (var use in combatant.Spec.Abilities.Where(use => use.Target == AbilityTarget.NamedAlly))
                {
                    if (!_combatants.Any(c => c.TeamIndex == combatant.TeamIndex && c.Name == use.TargetName))
                        throw new InvalidDataException(
                            $"'{combatant.Name}' casts '{use.Feat}' on a NamedAlly '{use.TargetName}' that is not on its team; " +
                            "copies of a combatant with a Count are named '<name> 1', '<name> 2' and so on.");
                }
            }
        }

        // A focus target may join the fight late, but it has to exist on an opposing team.
        private void RequireFocusTarget(int teamIndex)
        {
            var team = _scenario.Teams[teamIndex];
            if (team.TargetPolicy != TargetPolicy.Focus)
                return;

            if (string.IsNullOrWhiteSpace(team.FocusTarget))
                throw new InvalidDataException($"Team '{team.Name}' uses the Focus TargetPolicy but names no FocusTarget.");
            var targetTeams = _combatants.Where(c => c.TeamIndex != teamIndex && IsFocusTarget(c, team)).Select(c => c.TeamIndex).Distinct().Count();
            if (targetTeams == 0)
                throw new InvalidDataException($"Team '{team.Name}' has a FocusTarget of '{team.FocusTarget}', which names no opposing combatant.");
            if (targetTeams > 1)
                throw new InvalidDataException(
                    $"Team '{team.Name}' has a FocusTarget of '{team.FocusTarget}', which names combatants on more than one opposing team; rename one of them.");
        }

        public void Step(double step)
        {
            _starvationThisStep.Clear();
            foreach (var heal in _pendingHeals.Where(heal => _now >= heal.At).ToList())
            {
                _pendingHeals.Remove(heal);
                ApplyHealing(heal.Source, heal.Target, heal.Amount, heal.Kind);
            }

            // Shields ending now are gone before regeneration, heals over time and zone pulses read anyone's HP.
            foreach (var combatant in _combatants)
            {
                combatant.TemporaryHp.RemoveAll(p => p.ExpiresAt <= _now || p.Amount <= 0);
                foreach (var key in combatant.Statuses.Where(s => s.Value.ExpiresAt <= _now).Select(s => s.Key).ToList())
                    combatant.Statuses.Remove(key);
            }

            foreach (var combatant in _combatants.ToList())
            {
                if (combatant.IsDead)
                {
                    // The game's regeneration heartbeat has no dead-player check, so FP and STM keep coming back.
                    // A dead NPC restores nothing but keeps its schedule, so a revive does not replay missed ticks.
                    if (combatant.Spec.IsNpc)
                        SkipHeartbeats(combatant);
                    else
                        ProcessHeartbeat(combatant);
                    // A dead caster's zones keep their schedule; only those that outlive the caster land.
                    ProcessPartyPulses(combatant);
                    continue;
                }

                ProcessHeartbeat(combatant);
                ProcessConvergence(combatant);
                ProcessHealsOverTime(combatant);
                ProcessPartyPulses(combatant);
            }

            var order = _combatants.OrderBy(_ => _random.Next()).ToList();
            foreach (var combatant in order)
            {
                if (!IsEngaged(combatant))
                    continue;

                combatant.Weapon?.Integrate(_now, Math.Min(step, Math.Max(0, _scenario.DurationSeconds - _now)));
                ProcessCast(combatant);
                Decide(combatant);
                ProcessAutoAttack(combatant, step);

                if (combatant.IsStarved)
                {
                    // The last step can reach past the fight's end; only the time inside it counts.
                    var starved = Math.Min(step, Math.Max(0, _scenario.DurationSeconds - _now));
                    combatant.Stats.StarvedSeconds += starved;
                    _starvationThisStep.Add((combatant, starved));
                    combatant.Stats.FirstStarvedAt ??= _now;
                }
            }
        }

        private void ResolveUse(AbilityUseSpec use)
        {
            if (_resolvedUses.ContainsKey(use))
                return;

            if (!Enum.TryParse<FeatType>(use.Feat, out var feat))
                throw new InvalidDataException($"Unknown ability feat '{use.Feat}'.");

            var info = _catalog.Get(feat);
            if (info.UnreadableTiming != null)
                throw new InvalidDataException($"'{use.Feat}' computes its {info.UnreadableTiming} from the game engine, which the simulator cannot read.");

            if (info.SourceWeaponPower.HasValue && (info.Technique != null || use.Damage == null))
            {
                if (use.WeaponPower.HasValue && use.WeaponPower != info.SourceWeaponPower)
                    throw new InvalidDataException($"{use.Feat} power must match the source definition.");
                use.WeaponPower = info.SourceWeaponPower;
                use.IsControl = info.ImpactStatusTypes.Any(type =>
                    ((SWLOR.Game.Server.Service.StatusEffectService.IStatusEffect)Activator.CreateInstance(type)).Categories.HasFlag(
                        SWLOR.Game.Server.Service.StatusEffectService.StatusEffectCategory.Control));
            }
            if (use.MinimumStaminaBeforeUse < 0)
                throw new InvalidDataException("MinimumStaminaBeforeUse must be nonnegative.");
            if (use.RequiredSetup is not ("Any" or "None" or "Opening" or "Finish" or "Distraction") || use.WeaponPower is < 0 or > 10000 || use.WeaponHits is < 1 or > 10 || use.WeaponDamagePercent is < 0 or > 100)
                throw new InvalidDataException("Unsupported weapon ability calibration or rotation condition.");
            if (use.WeaponPower.HasValue && (use.Damage != null || !info.IsHostile ||
                !SWLOR.Game.Server.Service.Combat.IsWeaponSkillType(info.WeaponImpactSkillType != SkillType.Invalid ? info.WeaponImpactSkillType : info.SkillType)))
                throw new InvalidDataException("WeaponPower supports hostile weapon abilities with no flat Damage profile.");
            EffectCatalog.TryGet(feat, out var effect);
            if (effect == null && use.Damage == null && !use.WeaponPower.HasValue && info.TwinPistol == null)
                throw new InvalidDataException($"'{use.Feat}' has no modeled effect; give it a damage profile or add it to the effect catalog.");
            if (info.TwinPistol != null)
            {
                if (use.Damage != null) throw new InvalidDataException($"'{use.Feat}' uses its production payload; do not supply a calibrated damage profile.");
                if (use.Target != AbilityTarget.Enemy || use.LineTargets < 0 || use.MinimumFlow is < 0 or > 3 ||
                    !float.IsFinite(use.MovementSeconds) || use.MovementSeconds < 0 ||
                    use.MovementMeters.HasValue && (!float.IsFinite(use.MovementMeters.Value) || use.MovementMeters < 0 || use.MovementMeters > info.TwinPistol.MaxMovementMeters))
                    throw new InvalidDataException($"'{use.Feat}' has invalid movement, target, or Flow assumptions.");
                if (use.ChannelInterruptAfterSeconds is { } interruptAfter &&
                    (!info.IsChanneled || !float.IsFinite(interruptAfter) || interruptAfter <= 0 || interruptAfter >= info.CastSeconds))
                    throw new InvalidDataException($"'{use.Feat}' has an invalid channel interruption assumption.");
            }

            // A damage profile picks an enemy target, so only party-wide effects can resolve alongside it.
            if (effect != null && use.Damage != null && !IsPartyEffect(effect))
                throw new InvalidDataException($"'{use.Feat}' has a damage profile and a single-target effect; only party heals and shields can be paired with damage.");

            // Only a hostile ability hurts enemies; hostile hybrids such as Purifying Wave may also heal or shield.
            if (use.Damage != null && !info.IsHostile)
                throw new InvalidDataException($"'{use.Feat}' is not a hostile ability, so it cannot carry a damage profile.");

            if (!Enum.IsDefined(use.Target))
                throw new InvalidDataException($"'{use.Feat}' has an unknown Target; use one of {string.Join(", ", Enum.GetNames<AbilityTarget>())}.");
            RequireTargetFitsEffect(use, effect, info);

            if (use.BelowHpPercent.HasValue && (!float.IsFinite(use.BelowHpPercent.Value) || use.BelowHpPercent.Value < 0f))
                throw new InvalidDataException($"'{use.Feat}' needs a finite BelowHpPercent of at least 0.");

            if (!(use.MinIntervalSeconds >= 0) || !float.IsFinite(use.MinIntervalSeconds))
                throw new InvalidDataException($"'{use.Feat}' needs a finite MinIntervalSeconds of at least 0.");

            if (use.Damage != null)
                RequireValidDamage($"'{use.Feat}'", use.Damage);

            if (use.MaxTargets.HasValue)
            {
                if (use.MaxTargets.Value < 1)
                    throw new InvalidDataException($"'{use.Feat}' needs MaxTargets of at least 1.");

                if (effect == null || !IsPartyEffect(effect))
                    throw new InvalidDataException($"'{use.Feat}' sets MaxTargets but is not a zone or party heal or shield.");
            }

            _resolvedUses[use] = (info, effect);
        }

        // The game refuses these pairings, so a scenario that asks for them would heal or shield the wrong side.
        private static void RequireTargetFitsEffect(AbilityUseSpec use, AbilityEffect effect, AbilityInfo info)
        {
            var isRevive = effect?.Kind == EffectKind.Revive;
            if (isRevive != (use.Target == AbilityTarget.DeadAlly))
                throw new InvalidDataException(isRevive
                    ? $"'{use.Feat}' revives, so its Target must be DeadAlly."
                    : $"'{use.Feat}' does not revive, so it cannot target a DeadAlly.");

            if (effect?.Kind == EffectKind.SelfStatus && use.Target != AbilityTarget.Self)
                throw new InvalidDataException($"'{use.Feat}' puts a status on its caster, so its Target must be Self.");

            var isFriendlySingleTarget = effect != null && !isRevive && !IsPartyEffect(effect);
            if (isFriendlySingleTarget && use.Target == AbilityTarget.Enemy)
                throw new InvalidDataException(
                    $"'{use.Feat}' heals or shields one creature, so its Target must be Self, LowestAlly or NamedAlly.");

            // A friendly zone or party shield centers on the caster; only hostile hybrids (Purifying Wave) aim at an enemy.
            if (effect != null && IsPartyEffect(effect) && !info.IsHostile && use.Target == AbilityTarget.Enemy)
                throw new InvalidDataException(
                    $"'{use.Feat}' is a friendly zone or party effect, so its Target must be Self, LowestAlly or NamedAlly.");

            // A self-centered hostile cast (TARGETSELF in the game, as Purifying Wave and Sundering Sweep) is cast on the
            // caster whatever it hits, so its conditions read the caster; a queued weapon ability rides the next swing instead.
            if (info.IsHostile && (use.Damage != null || use.WeaponPower.HasValue || effect != null) && !info.IsAimed && !info.IsQueuedWeapon &&
                use.Target != AbilityTarget.Self)
                throw new InvalidDataException($"'{use.Feat}' is a self-centered hostile cast, so its Target must be Self.");

            // A hostile cast the game aims at a creature or spot needs an enemy.
            if (info.IsHostile && effect == null && (use.Damage != null || use.WeaponPower.HasValue) &&
                (use.Target is AbilityTarget.LowestAlly or AbilityTarget.NamedAlly || use.Target == AbilityTarget.Self && info.IsAimed))
                throw new InvalidDataException(
                    $"'{use.Feat}' is a hostile cast, so its Target must be Enemy{(info.IsAimed ? "" : " or Self")}.");
        }

        private static void RequireValidDamage(string owner, DamageSpec damage)
        {
            if (damage == null)
                throw new InvalidDataException($"{owner} needs a damage profile.");

            var problems = damage.GetProblems();
            if (problems.Count > 0)
                throw new InvalidDataException($"{owner} has a bad damage profile: {string.Join("; ", problems)}.");
        }

        private static bool IsPartyEffect(AbilityEffect effect) =>
            effect.Kind is EffectKind.PartyHealOverTime or EffectKind.PartyTemporaryHp;

        private int? GetLastTeamStanding()
        {
            var alive = _combatants.Where(c => !c.IsDead).Select(c => c.TeamIndex).Distinct().ToList();
            return alive.Count == 1 ? alive[0] : alive.Count == 0 ? -1 : null;
        }

        // Recurring events loop until caught up, so a time step longer than their interval loses none of them.
        private void ProcessHeartbeat(Combatant combatant)
        {
            while (_now >= combatant.NextHeartbeatAt)
            {
                combatant.NextHeartbeatAt += HeartbeatSeconds;
                combatant.HeartbeatCount++;

                combatant.StmRegenCarry += combatant.Spec.StmRegenPer30Seconds / HeartbeatsPerRegenCycle;
                var stamina = Math.Floor(combatant.StmRegenCarry);
                combatant.StmRegenCarry -= stamina;
                RestoreStamina(combatant, stamina, false);

                if (combatant.HeartbeatCount % HeartbeatsPerRegenCycle != 0)
                    continue;

                RestoreFp(combatant, Math.Floor(combatant.Spec.FpRegenPer30Seconds), false);
                // The game's tick needs current HP, which counts temporary HP, below max HP.
                // A heal does nothing to the dead.
                if (!combatant.IsDead && combatant.Spec.HpRegenPer30Seconds > 0 &&
                    combatant.Hp + combatant.TemporaryHpTotal < combatant.Spec.MaxHp)
                    ApplyHealing(null, combatant, (int)combatant.Spec.HpRegenPer30Seconds, HealKind.NaturalRegeneration);
            }
        }

        private void SkipHeartbeats(Combatant combatant)
        {
            while (_now >= combatant.NextHeartbeatAt)
            {
                combatant.NextHeartbeatAt += HeartbeatSeconds;
                combatant.HeartbeatCount++;
            }
        }

        private void ProcessConvergence(Combatant combatant)
        {
            while (combatant.ConvergenceUntil >= 0 && _now >= combatant.ConvergenceNextTickAt)
            {
                if (combatant.ConvergenceNextTickAt > combatant.ConvergenceUntil + 1e-6)
                {
                    combatant.ConvergenceUntil = -1;
                    return;
                }

                RestoreFp(combatant, Math.Max(1, Math.Ceiling(combatant.Spec.MaxFp * ConvergencePercentPerTick / 100f)), true);
                combatant.ConvergenceNextTickAt += ConvergenceTickSeconds;
            }
        }

        private void ProcessHealsOverTime(Combatant target)
        {
            foreach (var (key, instance) in target.HealsOverTime.ToList())
            {
                while (instance.TicksLeft > 0 && _now >= instance.NextTickAt)
                {
                    var source = instance.Source;
                    var amount = HealingMath.HealOverTimeTick(
                        instance.Effect,
                        target.Spec.MaxHp,
                        source.Spec.Willpower,
                        instance.AffinityMultiplier,
                        source.OutgoingHealingPercent,
                        _healFormula);
                    ApplyHealing(source, target, amount, HealKind.OverTime);

                    instance.TicksLeft--;
                    instance.NextTickAt += instance.Effect.TickSeconds;
                }

                if (instance.TicksLeft <= 0)
                    target.HealsOverTime.Remove(key);
            }
        }

        private void ProcessPartyPulses(Combatant caster)
        {
            foreach (var pulse in caster.PartyPulses.ToList())
            {
                while (pulse.TicksLeft > 0 && _now >= pulse.NextTickAt)
                {
                    // Kolto Mist's pulses each check that the caster is alive: pulses while it is dead are spent,
                    // and a revived caster's zone resumes.
                    if (!caster.IsDead || pulse.Effect.ContinuesAfterCasterDeath)
                    {
                        // A pulse picks its allies once. A zone with a damage cut refreshes it on them ahead of the
                        // heal, except on the last pulse: the status refreshes at 0, 3 ... 27 seconds and heals land at 3 ... 30.
                        // A pulse caught up from earlier in the step reaches only allies who were there by then.
                        var allies = PartyTargets(caster, pulse.MaxTargets, pulse.NextTickAt).ToList();
                        if (pulse.Effect.DamageReductionPercent > 0 && pulse.TicksLeft > 1)
                            RefreshPartyDamageReduction(caster, pulse.Effect, allies);
                        PulseParty(caster, pulse.Effect, allies, HealKind.OverTime);
                    }
                    pulse.TicksLeft--;
                    pulse.NextTickAt += pulse.Effect.TickSeconds;
                }

                if (pulse.TicksLeft <= 0)
                    caster.PartyPulses.Remove(pulse);
            }
        }

        private void PulseParty(Combatant caster, AbilityEffect effect, IEnumerable<Combatant> allies, HealKind kind)
        {
            foreach (var ally in allies)
            {
                var amount = HealingMath.PartyPulse(
                    effect,
                    ally.Spec.MaxHp,
                    caster.Spec.Willpower,
                    caster.Spec.ForceAffinity,
                    _rules.ForceAffinityHealingCap,
                    caster.OutgoingHealingPercent,
                    caster.Spec.CombatReadinessPercent,
                    _healFormula);
                ApplyHealing(caster, ally, amount, kind);
            }
        }

        private void RefreshPartyDamageReduction(Combatant caster, AbilityEffect effect, IEnumerable<Combatant> allies)
        {
            foreach (var ally in allies)
                ApplyDamageReduction(caster, ally, effect, ally.HpPercentWithTemporaryHp);
        }

        private void ProcessCast(Combatant caster)
        {
            var cast = caster.Casting;
            if (cast?.Ability is { IsChanneled: true, TwinPistol: not null } &&
                cast.Use.ChannelInterruptAfterSeconds is { } interruptAfter &&
                _now + 1e-9 >= cast.ChannelStartedAt + interruptAfter)
            {
                caster.Casting = null;
                CompleteTwin(caster, cast);
                caster.Stats.InterruptedCasts++;
                caster.NextActionAt = _now + caster.Spec.ReactionSeconds;
                caster.NextAttackAt = Math.Max(cast.AttackReadyBeforeChannel, _now);
                return;
            }
            if (cast == null || _now < cast.ImpactAt)
                return;

            caster.Casting = null;
            Resolve(caster, cast);
        }

        private void Decide(Combatant caster)
        {
            ExpireQueuedWeaponAbility(caster);
            if (caster.Casting != null || _now < caster.NextActionAt || caster.Weapon?.Control.IsIncapacitated(_now) == true)
                return;

            var starved = false;
            foreach (var use in caster.Spec.Abilities)
            {
                var (info, effect) = _resolvedUses[use];
                if (caster.RecastReadyAt.TryGetValue(info.RecastGroup, out var readyAt) && _now < readyAt)
                    continue;

                if (use.MinIntervalSeconds > 0 && caster.LastUsedAt.TryGetValue(use.Feat, out var lastUsedAt) && _now < lastUsedAt + use.MinIntervalSeconds)
                    continue;

                if (info.IsHostile && _now + 1e-9 < caster.HostileAbilityReadyAt)
                    continue;

                // Queueing another weapon ability would replace the one waiting for a hit.
                if (info.IsQueuedWeapon && caster.QueuedWeaponAbility != null)
                    continue;

                if (_rules.RefuseHitPointCostsAtOneHitPoint &&
                    HitPointCostCatalog.TryGet(info.Feat, out _) &&
                    caster.Hp + caster.TemporaryHpTotal <= 1)
                    continue;

                var target = ResolveTarget(caster, use, effect);
                if (target == null || !ConditionHolds(caster, use, effect, target) || caster.Stm < use.MinimumStaminaBeforeUse || caster.Weapon != null && !caster.Weapon.CanUse(info, use, target, _now))
                    continue;

                var fp = _rules.GetFpCost(info);
                var stm = _rules.GetStmCost(info);
                if (WeaponAbilityTraitPolicy.IsEligibleCostReduction(info.SkillType, stm))
                {
                    stm = Math.Max(0, stm + caster.WeaponTraits.Paired.GetAbilityStaminaCostAdjustment(WeaponNow, WeaponTraitState.Configuration(caster)));
                }
                stm = DiscountedStamina(caster, stm, false, info);
                if (info.TwinPistol != null && !CanUseTwin(caster, use, info.TwinPistol)) continue;
                if (caster.Fp < fp || caster.Stm < stm)
                {
                    starved = true;
                    continue;
                }

                if (effect?.Kind == EffectKind.Revive && !CanRevive(caster, target))
                {
                    // Counted once per reaction window (at least a second), so the count doesn't depend on the time step.
                    if (_now + 1e-9 >= caster.NextBlockedReviveCountAt)
                    {
                        caster.Stats.BlockedRevives++;
                        var window = Math.Max(1.0, caster.Spec.ReactionSeconds);
                        caster.NextBlockedReviveCountAt = caster.NextBlockedReviveCountAt + window > _now
                            ? caster.NextBlockedReviveCountAt + window
                            : _now + window;
                    }
                    continue;
                }

                // Stances and self buffs toggle (ToggleSelfStatus): using one that is active turns it off at once,
                // spending nothing and starting no recast. The game checks the costs first, so an unaffordable use
                // leaves it on.
                if (effect?.Kind == EffectKind.SelfStatus && caster.Statuses.Remove(effect.Key))
                {
                    caster.WeaponTraits.Haste.Clear();
                    caster.WeaponTraits.Paired.Reconcile(WeaponNow, WeaponTraitState.Configuration(caster));
                    // Still a use, for the scenario's MinIntervalSeconds.
                    caster.LastUsedAt[use.Feat] = _now;
                    caster.NextActionAt = _now + caster.Spec.ReactionSeconds;
                    caster.IsStarved = starved;
                    return;
                }

                StartCast(caster, use, info, effect, target, fp, stm);
                // Settling for a cheaper ability still counts as not affording the one wanted first.
                caster.IsStarved = starved;
                return;
            }

            caster.IsStarved = starved;
        }

        private void StartCast(Combatant caster, AbilityUseSpec use, AbilityInfo info, AbilityEffect effect, Combatant target, int fp, int stm)
        {
            var cast = new PendingCast
            {
                Use = use,
                Ability = info,
                Effect = effect,
                Target = target,
                ImpactAt = _now + info.CastSeconds,
                ChannelStartedAt = _now,
                ChannelEndsAt = _now + info.CastSeconds,
                AttackReadyBeforeChannel = caster.NextAttackAt,
                InterruptedByDamage = effect?.Kind == EffectKind.Revive
                                      && _rules.Revives == ReviveRule.BlockedInPvpLimitedInPve,
                FpCost = fp,
                StmCost = stm,
                WeaponTargets = info.TwinPistol == null ? null : CaptureTwinTargets(caster, use, info.TwinPistol, target),
                WeaponDamages = info.TwinPistol == null ? null : CaptureTwinWeaponDamage(caster.Spec.WeaponLoadout),
                WeaponTraits = caster.Spec.WeaponLoadout != null && info.IsHostile && Combat.IsWeaponSkillType(info.SkillType)
                    ? caster.WeaponTraits.Paired.CaptureAbility(WeaponNow, WeaponTraitState.Configuration(caster), true,
                        // Twin Pistol builders keep Flow for their finishers, as on the server.
                        WeaponAbilityTraitPolicy.IsEligibleSpender(info.IsHostile, info.SkillType, info.STM) &&
                        info.TwinPistol?.SpendsMomentum != false)
                    : null
            };
            if (cast.WeaponTraits != null)
                cast.WeaponActivation = new WeaponAbilityTraitActivation(cast.WeaponTraits,
                    info.TwinPistol?.MomentumPowerPerCharge is > 0 ? info.TwinPistol.MomentumPowerPerCharge
                        : WeaponTraitState.Configuration(caster).MomentumDamagePerCharge);

            caster.Weapon?.HoldSetupForCast(cast, _now);
            caster.NextActionAt = _now + info.CastSeconds + info.ImpactDelaySeconds + caster.Spec.ReactionSeconds;
            if (info.IsHostile)
                caster.HostileAbilityReadyAt = _now + _rules.HostileAbilityDelaySeconds;
            // The game stops the attack for an ability and resumes it 0.1s after the impact, or after queueing a weapon
            // ability (QueueWeaponAbility), so a swing due now cannot carry the ability queued with it.
            caster.NextAttackAt = Math.Max(caster.NextAttackAt, _now + info.CastSeconds + info.ImpactDelaySeconds + ResumeAttackDelaySeconds);
            if (info.IsChanneled && info.TwinPistol != null)
            {
                // Channels resume ActionAttack directly at completion, unlike the ordinary
                // impact approximation above. The native last-swing timer is retained.
                caster.NextAttackAt = Math.Max(cast.AttackReadyBeforeChannel, cast.ChannelEndsAt);
                cast.ImpactAt = _now;
                Resolve(caster, cast);
                return;
            }
            if (info.CastSeconds <= 0)
            {
                Resolve(caster, cast);
                return;
            }

            caster.Casting = cast;
            caster.NextAttackAt = Math.Max(caster.NextAttackAt, cast.ImpactAt);
        }

        private void Resolve(Combatant caster, PendingCast cast)
        {
            var effect = cast.Effect;
            var target = cast.Target;
            var isRevive = effect?.Kind == EffectKind.Revive;
            var targetLost = false;
            var twin = cast.Ability.TwinPistol;
            if (!cast.Activated && twin != null && twin.Movement != TwinPistolMovementType.FreeDestination && target.IsDead)
                return;
            // Paid Twin travel resolves its captured legal targets, even when none survive.
            if (twin == null && (isRevive ? !target.IsDead : target.IsDead))
            {
                // Area damage and party heals or shields centered on the caster (Purifying Wave) need no living
                // target: they still land, and their damage goes to enemies still in the fight. A cast spread over
                // several targets (Force Lightning's arcs) still resolves at the fallen one, so only the others take
                // their share.
                var needsNoTarget = effect == null ? cast.Use.Damage?.Area == true : IsPartyEffect(effect);
                if (isRevive)
                    return;
                if (needsNoTarget)
                    target = EnsureAttackTarget(caster);
                else if (effect == null && cast.Use.Damage is { Targets: > 1 })
                {
                    // The fallen primary stays the cast's target.
                }
                else if (effect == null && (cast.Use.Damage != null || cast.Use.WeaponPower.HasValue))
                    // The game completes a hostile cast at a body: it is paid for and goes on recast, but hits nothing.
                    targetLost = true;
                else
                    return;
            }

            // Convergence's accuracy comes from an earlier trigger; the cast that triggers it rolls first.
            var hitBonus = ConvergenceHitBonus(caster, cast.Ability) + FlatProfileSetupBonus(caster, target);
            if (!cast.Activated)
            {
                // The game checks the revive rules again when the cast completes; a failed check spends nothing.
                if (isRevive && !CanRevive(caster, target))
                {
                    caster.Stats.BlockedRevives++;
                    return;
                }

                if (WeaponAbilityTraitPolicy.IsEligibleCostReduction(cast.Ability.SkillType, _rules.GetStmCost(cast.Ability)))
                    cast.StmCost = Math.Max(0, _rules.GetStmCost(cast.Ability) + caster.WeaponTraits.Paired.GetAbilityStaminaCostAdjustment(WeaponNow, WeaponTraitState.Configuration(caster)));
                if (caster.Weapon != null) cast.StmCost = DiscountedStamina(caster, cast.StmCost, false, cast.Ability);
                if (caster.Fp < cast.FpCost || caster.Stm < cast.StmCost)
                {
                    caster.IsStarved = true;
                    return;
                }

                if (caster.Weapon != null)
                {
                    DiscountedStamina(caster, _rules.GetStmCost(cast.Ability), true, cast.Ability);
                    cast.Technique = caster.Weapon.Begin(cast.Ability, cast.Use, target, cast.StmCost, _now, cast);
                    CaptureTechniqueSweep(caster, target, cast);
                }
                PayCosts(caster, cast);
                caster.Stats.AbilitiesUsed++;
                caster.Stats.AbilityUses[cast.Use.Feat] = caster.Stats.AbilityUses.GetValueOrDefault(cast.Use.Feat) + 1;
                if (WeaponAbilityTraitPolicy.IsEligibleCostReduction(cast.Ability.SkillType, _rules.GetStmCost(cast.Ability)))
                    caster.WeaponTraits.Paired.ConsumeAbilityStaminaDiscount(WeaponNow, WeaponTraitState.Configuration(caster));
                if (cast.WeaponActivation != null)
                {
                    cast.WeaponActivation.SetPaidStamina(cast.StmCost);
                    cast.WeaponTraits = cast.WeaponActivation.Snapshot;
                    cast.WeaponActivation.IsCommitted = caster.WeaponTraits.Paired.CommitAbility(WeaponNow,
                        WeaponTraitState.Configuration(caster), cast.WeaponTraits);
                    if (cast.WeaponActivation.IsCommitted) caster.Stats.FlowSpent += cast.WeaponTraits.MomentumSpent;
                }
                caster.RecastReadyAt[cast.Ability.RecastGroup] = _now + _rules.GetRecastSeconds(cast.Ability) *
                    (1 + (cast.Ability.IsHostile ? caster.Weapon?.Stat(StatType.HostileAbilityRecastDelayPercentAdjustment) ?? 0 : 0) / 100d);
                caster.LastUsedAt[cast.Use.Feat] = _now;
                if (cast.Ability.IsChanneled && twin != null)
                    cast.Activated = true;

                // A delayed impact is paid for and on recast now; the caster stays busy until it lands.
                if (cast.Ability.ImpactDelaySeconds > 0)
                {
                    cast.Activated = true;
                    cast.ImpactAt = _now + cast.Ability.ImpactDelaySeconds;
                    caster.Casting = cast;
                    caster.NextAttackAt = Math.Max(caster.NextAttackAt, cast.ImpactAt);
                    return;
                }
            }

            // The game opens Convergence with the impact's activated effects, after its attack roll, and checks the
            // ability's declared FP requirement, not the adjusted amount charged. A delayed impact whose target is gone
            // is cancelled before then.
            if (cast.Ability.FP > 0 && cast.Ability.SkillType == SkillType.Force && !(cast.Activated && targetLost))
                TriggerConvergence(caster);
            if (targetLost)
                return;

            if (cast.Ability.TwinPistol != null)
            {
                ResolveTwin(caster, cast);
                return;
            }

            if (cast.Ability.IsQueuedWeapon && (cast.Use.Damage != null || cast.Use.WeaponPower.HasValue))
            {
                // The game holds a paid weapon ability for the next landed attack, for up to 30 seconds.
                caster.QueuedWeaponAbility = new QueuedWeaponAbility
                {
                    Use = cast.Use,
                    Ability = cast.Ability,
                    ExpiresAt = _now + QueuedWeaponAbilitySeconds,
                    Cast = cast
                };
                return;
            }

            if (HitPointCostCatalog.TryGet(cast.Ability.Feat, out var hitPointCost))
                PayHitPoints(caster, hitPointCost);

            if (cast.Use.WeaponPower.HasValue)
            {
                var victim = target.TeamIndex != caster.TeamIndex ? target : EnsureAttackTarget(caster);
                if (victim != null)
                {
                    var landed = ApplyWeaponAbility(caster, victim, cast);
                    caster.Weapon.Complete(cast.Ability, cast.Use, cast.Technique, victim, landed, _now);
                    caster.Weapon.CompleteSweep(cast.Technique, _now);
                }
            }
            if (cast.Use.Damage != null)
            {
                // Damage lands on enemies even when the use targets the caster or an ally (self-centered areas).
                var victim = target != null && target.TeamIndex != caster.TeamIndex ? target : EnsureAttackTarget(caster);
                DamageHealingCatalog.TryGet(cast.Ability.Feat, out var damageHealing);
                if (victim != null || cast.Use.Damage.Area)
                {
                    var hitTargets = new HashSet<uint>();
                    Action<Combatant> beforeDamage = null;
                    if (caster.Weapon != null)
                    {
                        BeginWeaponAreaImpact(caster, cast);
                        beforeDamage = hitTarget =>
                        {
                            hitTargets.Add(hitTarget.SimId);
                            if (cast.Ability.SourceWeaponPower.HasValue)
                                ApplyWeaponStatusRiders(caster, hitTarget, cast.Ability);
                            caster.Weapon.CostlyAbilityHit(cast.Technique);
                        };
                    }
                    var damageBefore = caster.Stats.DamageDealt;
                    var landed = ApplyDamageProfile(caster, victim, cast.Use.Damage, hitBonus, damageHealing, beforeDamage: beforeDamage, cast: cast);
                    cast.AnyWeaponHit = landed;
                    if (caster.Weapon != null)
                    {
                        cast.Technique.ImpactedTargets = hitTargets.Count;
                        caster.Weapon.Complete(cast.Ability, cast.Use, cast.Technique, victim ?? target, landed, _now);
                        caster.Weapon.CompleteSweep(cast.Technique, _now);
                    }
                    if (cast.WeaponActivation != null)
                    {
                        caster.Stats.AbilityDamage += caster.Stats.DamageDealt - damageBefore;
                        CompleteWeaponTraits(caster, cast);
                    }
                }
            }

            if (effect == null)
                return;

            switch (effect.Kind)
            {
                case EffectKind.DirectHeal:
                    var heal = HealingMath.DirectHeal(
                        effect,
                        target.Spec.MaxHp,
                        // The game's low-HP check reads current HP, which counts temporary HP.
                        target.HpPercentWithTemporaryHp,
                        target == caster,
                        caster.Spec.Willpower,
                        caster.Spec.ForceAffinity,
                        _rules.ForceAffinityHealingCap,
                        caster.OutgoingHealingPercent,
                        caster.Spec.CombatReadinessPercent,
                        _healFormula);
                    ApplyHealing(caster, target, heal, HealKind.InstantAbility);
                    break;

                case EffectKind.HealOverTime:
                    target.HealsOverTime[effect.Key] = new HealOverTimeInstance
                    {
                        Source = caster,
                        Effect = effect,
                        AffinityMultiplier = effect.UsesForceAffinity
                            ? HealingMath.AffinityMultiplier(caster.Spec.ForceAffinity, _rules.ForceAffinityHealingCap)
                            : 1f,
                        TicksLeft = effect.Ticks,
                        NextTickAt = _now + effect.TickSeconds
                    };
                    break;

                case EffectKind.PartyHealOverTime:
                    if (effect.TickSeconds <= 0)
                    {
                        // An immediate pulse (Purifying Wave) is an instant ability heal.
                        PulseParty(caster, effect, PartyTargets(caster, cast.Use.MaxTargets).ToList(), HealKind.InstantAbility);
                        break;
                    }

                    // Each cast keeps its own schedule, so a zone recast before the last one ends doesn't cut it short.
                    caster.PartyPulses.Add(new PartyPulseInstance
                    {
                        Effect = effect,
                        MaxTargets = cast.Use.MaxTargets,
                        TicksLeft = effect.Ticks,
                        NextTickAt = _now + effect.TickSeconds
                    });

                    // The zone's status lands at once; each pulse refreshes it (see ProcessPartyPulses).
                    if (effect.DamageReductionPercent > 0)
                        RefreshPartyDamageReduction(caster, effect, PartyTargets(caster, cast.Use.MaxTargets).ToList());
                    break;

                case EffectKind.TemporaryHp:
                {
                    // Rousing Shout's rescue is judged before the shield, on current HP including temporary HP.
                    var hpPercentBeforeShield = target.HpPercentWithTemporaryHp;
                    GrantTemporaryHp(caster, target, effect);
                    ApplyDamageReduction(caster, target, effect, hpPercentBeforeShield);
                    break;
                }

                case EffectKind.PartyTemporaryHp:
                    foreach (var ally in PartyTargets(caster, cast.Use.MaxTargets))
                    {
                        var hpPercentBeforeShield = ally.HpPercentWithTemporaryHp;
                        GrantTemporaryHp(caster, ally, effect);
                        ApplyDamageReduction(caster, ally, effect, hpPercentBeforeShield);
                    }
                    break;

                case EffectKind.Revive:
                    Revive(caster, target, effect);
                    break;
                case EffectKind.SelfStatus:
                    var isStance = StatusStats.IsStance(effect.StatusType);
                    // One stance at a time, as the game's RemoveOtherStanceStatuses does.
                    if (isStance)
                    {
                        foreach (var key in caster.Statuses.Where(s => s.Value.IsStance).Select(s => s.Key).ToList())
                            caster.Statuses.Remove(key);
                        caster.WeaponTraits.Haste.Clear();
                    }

                    caster.Statuses[effect.Key] = new ActiveStatus
                    {
                        Stats = StatusStats.Get(effect.StatusType),
                        ExpiresAt = effect.DurationSeconds > 0 ? _now + effect.DurationSeconds : double.PositiveInfinity,
                        IsStance = isStance
                    };
                    break;
            }
        }

        /// <summary>
        /// Pays a cast's costs in the order the ability lists them, each followed by Conduit's restore of the other
        /// resource, as the game's requirements do: an FP-first ability returns STM before its STM cost makes room for it.
        /// </summary>
        private void PayCosts(Combatant caster, PendingCast cast)
        {
            if (cast.Ability.PaysFpFirst)
            {
                PayFp(caster, cast);
                PayStamina(caster, cast);
            }
            else
            {
                PayStamina(caster, cast);
                PayFp(caster, cast);
            }
        }

        // Conduit, as Combat's FP-cost stamina restore and stamina-cost FP restore do it, rounded down.
        private void PayFp(Combatant caster, PendingCast cast)
        {
            caster.Fp -= cast.FpCost;
            caster.Stats.FpSpent += cast.FpCost;
            if (cast.FpCost > 0 && IsRestoreSkill(caster, StatType.AbilityFPCostStaminaRestorePercentSkillType, cast.Ability.SkillType))
                RestoreStamina(caster, CrossResourceRestore(cast.FpCost, caster.GetStat(StatType.AbilityFPCostStaminaRestorePercent)), true);
        }

        private void PayStamina(Combatant caster, PendingCast cast)
        {
            caster.Stm -= cast.StmCost;
            caster.Stats.StmSpent += cast.StmCost;
            if (cast.StmCost > 0 && IsRestoreSkill(caster, StatType.AbilityStaminaCostFPRestorePercentSkillType, cast.Ability.SkillType))
                RestoreFp(caster, CrossResourceRestore(cast.StmCost, caster.GetStat(StatType.AbilityStaminaCostFPRestorePercent)), true);
        }

        private static bool IsRestoreSkill(Combatant caster, StatType skillStat, SkillType skill)
        {
            var required = caster.GetStat(skillStat);
            return required > 0 && (int)skill == required;
        }

        private static int CrossResourceRestore(int cost, int percent) =>
            percent <= 0 ? 0 : cost * Math.Min(percent, MaximumCrossResourceRestorePercent) / 100;

        private void TriggerConvergence(Combatant caster)
        {
            if (caster.GetStat(StatType.ForceConvergence) <= 0 || _now < caster.ConvergenceReadyAt)
                return;

            caster.ConvergenceUntil = _now + _rules.ConvergenceDurationSeconds;
            caster.ConvergenceNextTickAt = _now + ConvergenceTickSeconds;
            caster.ConvergenceReadyAt = _now + ConvergenceCooldownSeconds;
            caster.ConvergenceAccuracyUntil = _now + ConvergenceAccuracySeconds;
        }

        private Combatant ResolveTarget(Combatant caster, AbilityUseSpec use, AbilityEffect effect)
        {
            switch (use.Target)
            {
                case AbilityTarget.Self:
                    return caster;
                case AbilityTarget.LowestAlly:
                    // The lowest ally the use can still help: under its HP threshold and, for KeepUp, not yet covered.
                    return Allies(caster).OrderBy(a => a.HpPercentWithTemporaryHp).FirstOrDefault(a => ConditionHolds(caster, use, effect, a));
                case AbilityTarget.NamedAlly:
                    return Allies(caster).FirstOrDefault(a => a.Name == use.TargetName);
                case AbilityTarget.DeadAlly:
                    // A body the rules allow reviving comes first, so one locked-out body doesn't block the rest.
                    return _combatants
                        .Where(c => c.IsDead && c.TeamIndex == caster.TeamIndex)
                        .OrderBy(c => CanRevive(caster, c) ? 0 : 1)
                        .ThenBy(c => c.DiedAt)
                        .FirstOrDefault();
                case AbilityTarget.Enemy:
                    return EnsureAttackTarget(caster);
                default:
                    throw new InvalidDataException($"'{use.Feat}' has an unknown Target.");
            }
        }

        private bool ConditionHolds(Combatant caster, AbilityUseSpec use, AbilityEffect effect, Combatant target)
        {
            if (use.Target == AbilityTarget.DeadAlly)
                return true;

            // Thresholds read current HP, which counts temporary HP, as the game's health checks do.
            if (use.BelowHpPercent.HasValue && target.HpPercentWithTemporaryHp >= use.BelowHpPercent.Value)
                return false;

            if (!use.KeepUp || effect == null)
                return true;

            return effect.Kind switch
            {
                EffectKind.HealOverTime => !target.HealsOverTime.ContainsKey(effect.Key),
                EffectKind.PartyHealOverTime => caster.GetZone(effect.Key) == null,
                EffectKind.TemporaryHp => target.TemporaryHp.All(p => p.Key != effect.Key),
                EffectKind.PartyTemporaryHp => target.TemporaryHp.All(p => p.Key != effect.Key),
                EffectKind.SelfStatus => !target.Statuses.ContainsKey(effect.Key),
                _ => true
            };
        }

        private bool CanRevive(Combatant caster, Combatant target)
        {
            switch (_rules.Revives)
            {
                case ReviveRule.BlockedInPvpLimitedInPve:
                    // The game's ReviveRules. Without positions, every living enemy in the fight counts as within 20m.
                    // A hostile player blocks; after a player kill the party must also go the window without taking
                    // damage from anyone, killing blows included; a player revived mid-fight is then locked out.
                    if (Enemies(caster).Any(enemy => _scenario.IsPvp && !enemy.Spec.IsNpc))
                        return false;
                    if (target.KilledByPlayer && _combatants.Any(c =>
                            c.TeamIndex == caster.TeamIndex && _now >= c.Spec.JoinAtSeconds && WasHitWithinReviveWindow(c)))
                        return false;
                    return _now >= target.ReviveLockedUntil;
                default:
                    return true;
            }
        }

        // The dead (the body too) count from their killing blow; the living from their last hit, as the game does.
        private bool WasHitWithinReviveWindow(Combatant member)
        {
            var sinceHit = _now - member.LastDamageTakenAt;
            return member.IsDead
                ? sinceHit < _rules.PvpReviveDamageWindowSeconds
                : sinceHit <= _rules.PvpReviveDamageWindowSeconds;
        }

        private void Revive(Combatant caster, Combatant target, AbilityEffect effect)
        {
            target.IsDead = false;
            target.Hp = 1;
            // Swings scheduled while dead are not made up, and enemies can hit it again from now.
            target.NextAttackAt = Math.Max(target.NextAttackAt, _now);
            target.AvailableSince = _now;
            target.Stats.RevivesReceived++;
            _revives++;

            // A player revived while a fight is still on (a living enemy remains) starts the mid-fight lockout.
            if (_rules.Revives == ReviveRule.BlockedInPvpLimitedInPve && !target.Spec.IsNpc && Enemies(caster).Any())
                target.ReviveLockedUntil = _now + _rules.PveReviveLockoutSeconds;

            if (effect.Percent <= 0)
                return;

            var heal = HealingMath.DirectHeal(
                effect,
                target.Spec.MaxHp,
                0f,
                false,
                caster.Spec.Willpower,
                caster.Spec.ForceAffinity,
                _rules.ForceAffinityHealingCap,
                caster.OutgoingHealingPercent,
                // The game heals 0.1s after the revive, once the ability's impact has ended, so Combat Readiness never applies.
                0f,
                _healFormula);
            _pendingHeals.Add(new PendingHeal { Source = caster, Target = target, Amount = heal, Kind = HealKind.Revive, At = _now + ReviveHealDelaySeconds });
        }

        private void ProcessAutoAttack(Combatant attacker, double step)
        {
            if (attacker.Spec.WeaponLoadout != null)
            {
                ProcessWeaponAutoAttack(attacker, step);
                return;
            }
            var attack = attacker.Spec.AutoAttack;
            if (attacker.Weapon?.Control.IsIncapacitated(_now) == true)
            {
                attacker.NextAttackAt = Math.Max(attacker.NextAttackAt, attacker.Weapon.Control.IncapacitatedUntil);
                return;
            }
            if (attack == null || attacker.Weapon == null && attack.AttacksPerSecond <= 0 || attacker.Casting != null)
                return;

            // Nothing is due this step, so there is no need to look for enemies.
            if (_now < attacker.NextAttackAt)
                return;

            // Attacks due during this step are all made; ones missed before it (not yet engaged, or with nobody
            // there to hit) are not made up.
            var enemiesSince = Enemies(attacker).Select(enemy => enemy.AvailableSince).DefaultIfEmpty(double.PositiveInfinity).Min();
            if (double.IsPositiveInfinity(enemiesSince))
            {
                // Wait for the next enemy due to join rather than looking again every step.
                var nextArrival = _combatants
                    .Where(c => c.TeamIndex != attacker.TeamIndex && !c.IsDead && c.Spec.JoinAtSeconds > _now)
                    .Select(c => (double)c.Spec.JoinAtSeconds)
                    .DefaultIfEmpty(_now)
                    .Min();
                attacker.NextAttackAt = Math.Max(attacker.NextAttackAt, nextArrival);
                return;
            }

            attacker.NextAttackAt = Math.Max(attacker.NextAttackAt,
                Math.Max(Math.Max(_now - step, attacker.Spec.JoinAtSeconds), enemiesSince));
            while (_now >= attacker.NextAttackAt)
            {
                var swingAt = attacker.NextAttackAt;
                attacker.NextAttackAt += attacker.Weapon?.SwingSeconds(swingAt) ?? 1.0 / attack.AttacksPerSecond;
                // A swing caught up from earlier in the step reaches only enemies who were there by then.
                var target = EnsureAttackTarget(attacker, swingAt);
                if (target == null)
                    return;

                ExpireQueuedWeaponAbility(attacker);
                var queued = attacker.QueuedWeaponAbility;
                if (queued != null) attacker.Weapon?.Target(target, _now);
                if (queued == null || attacker.Weapon != null && queued.Use.WeaponPower.HasValue)
                {
                    if (attacker.Weapon != null)
                    {
                        var attacks = attacker.Weapon.TakeSwingAttacks(swingAt);
                        for (var i = 0; i < attacks && !target.IsDead; i++)
                        {
                            // A formula weapon ability rides the swing's next attack; a miss keeps it queued.
                            if (attacker.QueuedWeaponAbility is { Use.WeaponPower: not null } carried) ApplyQueuedWeaponAbility(attacker, target, carried);
                            else ApplyWeaponOrdinary(attacker, target);
                        }
                    }
                    else ApplyDamageProfile(attacker, target, attack.Damage, 0f, at: swingAt);
                    continue;
                }

                // The queued ability rides this swing: the swing's roll decides it, a miss keeps it waiting for the
                // next one, and a hit lands the whole activation on every recipient without further rolls.
                var hitChance = Math.Min(1f, queued.Use.Damage.GetHitChance(target.Name) + ConvergenceHitBonus(attacker, queued.Ability));
                if (_random.NextDouble() >= hitChance)
                    continue;

                DamageHealingCatalog.TryGet(queued.Ability.Feat, out var damageHealing);
                ApplyDamageProfile(attacker, target, queued.Use.Damage, 0f, damageHealing, alwaysHits: true, at: swingAt);

                attacker.QueuedWeaponAbility = null;
                if (HitPointCostCatalog.TryGet(queued.Ability.Feat, out var hitPointCost))
                    PayHitPoints(attacker, hitPointCost);
            }
        }

        private static double ReactionInterval(CombatantSpec spec) =>
            float.IsFinite(spec.ReactionSeconds) && spec.ReactionSeconds > 0 ? spec.ReactionSeconds : 1.0;

        private static double AttackInterval(CombatantSpec spec) =>
            spec.WeaponCombat is { } weapon
                ? SWLOR.Game.Server.Service.Combat.CalculateAttackSwingDelay(SWLOR.Game.Server.Service.Combat.CalculateEffectiveAttackDelay(SWLOR.Game.Server.Service.Combat.CalculateAttackDelayMilliseconds(weapon.WeaponDelayUnits, 0, weapon.ExternalHastePercent, 0))) / 1000d
                : spec.WeaponLoadout != null ? WeaponInterval(spec.WeaponLoadout, spec.WeaponLoadout.HastePercent)
                : spec.AutoAttack is { AttacksPerSecond: > 0 } attack ? 1.0 / attack.AttacksPerSecond : 2.0;

        private void ExpireQueuedWeaponAbility(Combatant combatant)
        {
            if (combatant.QueuedWeaponAbility != null && _now >= combatant.QueuedWeaponAbility.ExpiresAt)
                combatant.QueuedWeaponAbility = null;
        }

        private float ConvergenceHitBonus(Combatant caster, AbilityInfo ability) =>
            ability.SkillType == SkillType.Force && _now < caster.ConvergenceAccuracyUntil ? ConvergenceHitChanceBonus : 0f;

        /// <returns>Whether a hit landed on the chosen target, or on anyone for an area.</returns>
        private bool ApplyDamageProfile(
            Combatant attacker, Combatant target, DamageSpec damage, float hitBonus, DamageHealing damageHealing = null,
            bool alwaysHits = false, double? at = null, Action<Combatant> beforeDamage = null, PendingCast cast = null)
        {
            var reachable = EnemiesAt(attacker, at ?? _now).ToList();
            var targets = damage.Area ? reachable : new List<Combatant> { target };
            if (!damage.Area && damage.Targets > 1)
            {
                // The rest of the area's victims are distinct enemies, never the same one twice.
                targets.AddRange(reachable
                    .Where(enemy => enemy != target)
                    .OrderBy(_ => _random.Next())
                    .Take(damage.Targets - 1));
            }
            var healingLeft = damageHealing == null
                ? 0.0
                : attacker.Spec.MaxHp * DamageHealing.MaximumMaxHpPercentPerCast / 100.0;
            var landed = false;
            for (var index = 0; index < targets.Count; index++)
            {
                var victim = targets[index];
                if (victim.IsDead)
                    continue;
                // In the Targets form the first victim is the chosen one; the rest take the extra-target share.
                var multiplier = !damage.Area && index > 0 ? damage.ExtraTargetDamagePercent / 100.0 : 1.0;
                var statusesResolved = false;
                for (var hit = 0; hit < damage.Hits; hit++)
                {
                    var weaponAbility = cast?.WeaponActivation != null;
                    if (weaponAbility) attacker.Stats.AbilityImpacts++;
                    var weapon = (uint)(hit % Math.Min(damage.WeaponItems, attacker.Spec.WeaponLoadout?.DistinctAttackItemCount ?? 1) + 1);
                    int TakeFlowBonus()
                    {
                        var bonus = cast?.WeaponActivation?.TryTakeDamageBonus(weapon) ?? 0;
                        attacker.Stats.FlowDamagePowerApplied += bonus;
                        return bonus;
                    }
                    var (hitLanded, dealt) = damage.WeaponPower != null
                        ? RollNativeWeaponProfile(attacker, victim, damage.WeaponPower, weapon,
                            weaponAbility ? TakeFlowBonus : null, cast?.WeaponTraits?.AccuracyAdjustment ?? 0,
                            multiplier, alwaysHits)
                        : RollAndApply(attacker, victim, damage, hitBonus, multiplier, alwaysHits,
                            cast?.WeaponTraits?.AccuracyAdjustment ?? 0, beforeDamage == null ? null : () =>
                        {
                            if (statusesResolved) return;
                            beforeDamage(victim);
                            statusesResolved = true;
                        });
                    if (weaponAbility && hitLanded)
                    {
                        attacker.Stats.AbilityHits++;
                        RecordSuccessfulWeaponAbilityHit(attacker, cast);
                    }
                    if (hitLanded && (damage.Area || index == 0))
                        landed = true;
                    // The game picks the low-HP rate after the hit, from the victim's HP it left.
                    if (damageHealing != null && dealt > 0 && healingLeft > 0)
                        healingLeft -= HealFromDamage(attacker, dealt,
                            victim.HpPercentWithTemporaryHp <= 50f ? damageHealing.LowHpPercent : damageHealing.Percent, healingLeft);
                }
            }

            return landed;
        }

        /// <summary>
        /// The game's damage-derived healing: a share of the damage dealt, adjusted by the caster's healing received,
        /// never more than half the hit or what is left of the cast's cap. Returns the HP requested.
        /// </summary>
        private double HealFromDamage(Combatant caster, double dealt, float percent, double healingLeft)
        {
            // GameMath.PercentOf rounds both the share and the per-hit cap up.
            double requested = HealingMath.AddReceivedPercent((int)Math.Ceiling(dealt * percent / 100.0), caster.HealingReceivedPercent);
            requested = Math.Min(Math.Min(requested, Math.Ceiling(dealt * DamageHealing.MaximumDamagePercentPerHit / 100.0)), healingLeft);
            // Only the HP the caster is missing is healed and charged to the cast's cap, as the game's
            // CalculateEffectiveHealingAmount runs before the activated-healing budget.
            var amount = (int)Math.Min(requested, Math.Max(0, caster.Spec.MaxHp - caster.Hp));
            if (amount <= 0)
                return 0;

            ApplyHealing(caster, caster, amount, HealKind.DamageDerived, receivedAlreadyApplied: true);
            return amount;
        }

        /// <summary>
        /// Pays an ability's HP cost the way the game's SacrificeHitPoints does: temporary HP first, never below 1 HP.
        /// Before the tuning it landed unpaid at 1 HP; Bloodlust's stamina restore follows the payment.
        /// </summary>
        private void PayHitPoints(Combatant caster, HitPointCost cost)
        {
            var currentHp = caster.Hp + caster.TemporaryHpTotal;
            if (currentHp <= 1)
                return;

            // GameMath.PercentOf rounds the cost up.
            var fullCost = (double)Math.Max(1, (int)Math.Ceiling(caster.Spec.MaxHp * (cost.GetPercent(caster.Spec.Might) / 100f)));
            var payment = Math.Min(currentHp - 1, fullCost);
            var remaining = payment;
            foreach (var pool in caster.TemporaryHp.OrderBy(p => p.ExpiresAt))
            {
                var absorbed = Math.Min(pool.Amount, remaining);
                pool.Amount -= absorbed;
                remaining -= absorbed;
                if (remaining <= 0)
                    break;
            }

            caster.TemporaryHp.RemoveAll(p => p.Amount <= 0);
            caster.Hp -= remaining;
            caster.Stats.HitPointsSpent += remaining;
            RestoreStaminaOnHitPointSpend(caster, remaining, fullCost);
        }

        /// <summary>Bloodlust's stamina restore, driven by the same stats as the game's.</summary>
        private void RestoreStaminaOnHitPointSpend(Combatant caster, double hitPointsLost, double fullCost)
        {
            var percent = caster.GetStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent);
            if (percent <= 0 || _now < caster.HitPointSpendRestoreReadyAt)
                return;
            if (_rules.ScaleHitPointSpendRestoreToHitPointsLost && hitPointsLost <= 0)
                return;

            caster.HitPointSpendRestoreReadyAt =
                _now + caster.GetStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds);
            var scalingAbility = (AbilityType)(caster.GetStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility) - 1);
            if (Enum.IsDefined(scalingAbility) && scalingAbility != AbilityType.Invalid)
                percent += Math.Max(0, AbilityScore(caster, scalingAbility));
            var maximum = caster.GetStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent);
            if (maximum > 0)
                percent = Math.Min(maximum, percent);

            // GameMath.PercentOf rounds the restore up, to at least 1.
            double stamina = Math.Max(1, (int)Math.Ceiling(caster.Spec.MaxStm * (percent / 100f)));
            // The current rules scale the restore by the share of the cost actually lost, at least 1.
            if (_rules.ScaleHitPointSpendRestoreToHitPointsLost && hitPointsLost < fullCost)
                stamina = Math.Max(1, Math.Ceiling(stamina * hitPointsLost / fullCost));
            RestoreStamina(caster, (int)stamina, true);
        }

        /// <returns>Whether the roll hit, and the damage that landed on temporary HP or HP.</returns>
        private (bool Hit, double Dealt) RollAndApply(
            Combatant attacker, Combatant target, DamageSpec damage, float hitBonus = 0f, double multiplier = 1.0,
            bool alwaysHits = false, int accuracyBonus = 0, Action beforeDamage = null)
        {
            var hitChance = damage.Accuracy is { } accuracy
                ? Combat.CalculateHitRate(accuracy + attacker.GetStat(StatType.Accuracy) + accuracyBonus, target.Spec.Evasion, 0) / 100f
                : damage.GetHitChance(target.Name);
            if (target.IsDead)
                return (false, 0);
            if (!alwaysHits && _random.NextDouble() >= Math.Min(1f, hitChance + hitBonus))
            {
                // A calibrated miss might be concealment or another avoidance mechanism. Only
                // an explicit ACC-versus-EVA roll proves the production Second Wind trigger.
                if (damage.Accuracy.HasValue && attacker.TeamIndex != target.TeamIndex)
                    target.WeaponTraits.Paired.RecordAccuracyEvasionMiss(WeaponNow, WeaponTraitState.Configuration(target));
                return (false, 0);
            }

            var amount = damage.Min + _random.NextDouble() * Math.Max(0, damage.Max - damage.Min);
            if (damage.CritChance > 0 && _random.NextDouble() < damage.CritChance)
                amount *= damage.CritMultiplier;

            beforeDamage?.Invoke();
            return (true, ApplyDamage(attacker, target, amount * multiplier, damage.Type, directHostile: true));
        }

        /// <returns>Damage that landed, on temporary HP or HP.</returns>
        public double ApplyDamage(Combatant attacker, Combatant target, double amount, DamageType type = DamageType.Physical, bool directHostile = false)
        {
            if (target.IsDead || amount <= 0)
                return 0;

            if (IsPlayerVersusPlayer(attacker, target))
            {
                MarkPvpActivity(attacker);
                MarkPvpActivity(target);
                var percent = RuleSet.GetStepPercent(_rules.PvpDamageBackstop, PvpSeconds(target));
                if (amount == Math.Truncate(amount) && amount <= int.MaxValue)
                    amount = percent == Math.Truncate(percent) && percent <= int.MaxValue
                        ? PvpCombatRules.IncreaseDamage((int)amount, (int)percent)
                        : amount + Math.Ceiling(amount * (percent / 100d));
                else
                    amount *= 1 + percent / 100f; // Preserve calibrated fractional-profile sampling.
            }

            // The game's damage is whole points, and each percentage stage rounds as its code does. A family of
            // overlapping cuts (Leadership's) keeps only its strongest; the combatant's own cut and ungrouped cuts to
            // all damage share the game's one summed damage-taken stat; other typed cuts apply on their own.
            amount = Math.Max(1, Math.Round(amount));
            var active = target.DamageReductions.Values.Where(r => r.ExpiresAt > _now && r.Applies(type)).ToList();
            foreach (var family in active.Where(r => r.Group != null).GroupBy(r => r.Group))
                amount = DamageMath.ApplyPercentCut(amount, family.Max(r => r.Percent));
            var ungrouped = active.Where(r => r.Group == null).ToList();
            // Typed status cuts land first; the generic stage then holds the game's combined cap across both.
            var statusMultiplier = 1.0;
            foreach (var reduction in ungrouped.Where(r => r.Scope != DamageReductionScope.All))
            {
                amount = DamageMath.ApplyPercentCut(amount, reduction.Percent);
                statusMultiplier *= DamageMath.CutMultiplier(reduction.Percent);
            }
            var genericCut = target.Spec.DamageReductionPercent + ungrouped.Where(r => r.Scope == DamageReductionScope.All).Sum(r => (double)r.Percent);
            amount = DamageMath.ApplyGenericCut(amount, genericCut, statusMultiplier);

            var landed = 0.0;
            foreach (var pool in target.TemporaryHp.OrderBy(p => p.ExpiresAt))
            {
                if (amount <= 0)
                    break;

                var absorbed = Math.Min(pool.Amount, amount);
                pool.Amount -= absorbed;
                amount -= absorbed;
                landed += absorbed;
                target.Stats.DamageAbsorbedByTemporaryHp += absorbed;
                attacker.Stats.DamageDealt += absorbed;
            }

            target.TemporaryHp.RemoveAll(p => p.Amount <= 0);
            target.LastDamageTakenAt = _now;
            if (attacker.TeamIndex != target.TeamIndex)
                target.WeaponTraits.Paired.RecordDirectHostileDamage(WeaponNow, WeaponTraitState.Configuration(target));

            if (amount > 0)
            {
                var dealt = Math.Min(amount, target.Hp);
                target.Hp -= amount;
                if (directHostile && dealt > 0 && target.Hp > 0 && attacker.TeamIndex != target.TeamIndex)
                    target.Weapon?.DirectHpDamage(attacker, _now);
                landed += dealt;
                target.Stats.DamageTakenToHp += dealt;
                attacker.Stats.DamageDealt += dealt;
            }

            if (landed > 0 && target.Hp > 0) target.Weapon?.AnyDamage(_now);
            if (target.Casting?.InterruptedByDamage == true)
            {
                target.Casting = null;
                target.Stats.InterruptedCasts++;
                target.NextActionAt = _now + target.Spec.ReactionSeconds;
                // The game resumes the caster's attack as soon as the cast is interrupted.
                target.NextAttackAt = Math.Min(target.NextAttackAt, _now);
            }

            if (target.Hp <= 0)
                Die(target, attacker);
            return landed;
        }

        /// <param name="receivedAlreadyApplied">The amount already carries the target's healing received, as damage-derived heals do.</param>
        public void ApplyHealing(Combatant source, Combatant target, int amount, HealKind kind, bool receivedAlreadyApplied = false)
        {
            if (target.IsDead || amount <= 0)
                return;

            double adjusted = kind == HealKind.NaturalRegeneration || receivedAlreadyApplied
                ? amount
                : HealingMath.AddReceivedPercent(amount, target.HealingReceivedPercent);

            if (IsInPvpCombat(target))
            {
                var ramp = RuleSet.GetStepPercent(_rules.PvpHealingRamp, PvpSeconds(target));
                adjusted *= _rules.PvpHealingMultiplier * (1 - ramp / 100f);
            }

            adjusted = Math.Max(0, adjusted);
            var effective = Math.Min(adjusted, target.Spec.MaxHp - target.Hp);
            if (effective < 0)
                effective = 0;

            target.Hp += effective;
            target.Stats.HealingReceived += effective;
            target.Stats.Overhealing += adjusted - effective;
            if (source != null)
                source.Stats.HealingDone += effective;

            if (effective > 0 && kind != HealKind.NaturalRegeneration)
                TryRestoreStaminaOnHeal(target, kind);
        }

        /// <summary>The game's healing-received stamina restore (Defensive Harmony), driven by the same stats.</summary>
        private void TryRestoreStaminaOnHeal(Combatant target, HealKind kind)
        {
            var chance = target.GetStat(StatType.HealingReceivedStaminaRestoreChance);
            var stamina = target.GetStat(StatType.HealingReceivedStaminaRestore);
            if (chance <= 0 || stamina <= 0)
                return;
            if (_rules.HarmonyInstantHealsOnly && kind != HealKind.InstantAbility)
                return;
            if (_now < target.HarmonyReadyAt)
                return;

            var maximumChance = target.GetStat(StatType.HealingReceivedStaminaRestoreChanceMaximum);
            var scalingAbility = (AbilityType)(target.GetStat(StatType.HealingReceivedStaminaRestoreChanceScalingAbility) - 1);
            if (maximumChance > chance && Enum.IsDefined(scalingAbility) && scalingAbility != AbilityType.Invalid)
                chance = Math.Min(maximumChance, chance + Math.Max(0, AbilityScore(target, scalingAbility)));

            if (_random.Next(100) >= chance)
                return;

            RestoreStamina(target, stamina, true);
            target.HarmonyReadyAt = _now + _rules.HarmonyCooldownSeconds;
        }

        private static int AbilityScore(Combatant combatant, AbilityType ability) => ability switch
        {
            AbilityType.Might => combatant.Spec.Might,
            AbilityType.Willpower => combatant.Spec.Willpower,
            AbilityType.Social => combatant.Spec.Social,
            _ => throw new InvalidDataException($"The simulator does not model {ability}, which '{combatant.Name}' needs for a perk.")
        };

        /// <summary>The timed damage-taken cut an effect carries, such as Pain Suppressant's; recasting replaces it.</summary>
        private void ApplyDamageReduction(Combatant source, Combatant target, AbilityEffect effect, float targetHpPercent)
        {
            if (effect.DamageReductionPercent <= 0 || target.IsDead)
                return;
            if (effect.DamageReductionBelowHpPercent > 0 && targetHpPercent > effect.DamageReductionBelowHpPercent)
                return;

            var percent = effect.DamageReductionMaximumPercent > effect.DamageReductionPercent
                ? HealingMath.SocialScaledPercent(effect.DamageReductionPercent, effect.DamageReductionMaximumPercent, source.Spec.Social)
                : effect.DamageReductionPercent;
            var seconds = effect.DamageReductionSeconds > 0 ? effect.DamageReductionSeconds : effect.DurationSeconds;
            target.DamageReductions[effect.DamageReductionKey ?? effect.Key] = new DamageReductionInstance
            {
                Percent = percent,
                Scope = effect.DamageReductionScope,
                Group = effect.DamageReductionGroup,
                ExpiresAt = _now + seconds
            };
        }

        private void GrantTemporaryHp(Combatant source, Combatant target, AbilityEffect effect)
        {
            if (target.IsDead)
                return;

            double amount = HealingMath.TemporaryHp(
                effect,
                target.Spec.MaxHp,
                source.Spec.Willpower,
                source.Spec.Social,
                source.Spec.ForceAffinity,
                _rules.ForceAffinityHealingCap,
                source.Spec.CombatReadinessPercent,
                _healFormula);

            if (IsInPvpCombat(target))
                amount *= _rules.PvpTemporaryHpMultiplier;

            target.TemporaryHp.RemoveAll(p => p.Key == effect.Key);
            if (_rules.TemporaryHpCapPercent.HasValue)
            {
                var room = target.Spec.MaxHp * _rules.TemporaryHpCapPercent.Value / 100.0 - target.TemporaryHpTotal;
                amount = Math.Min(amount, Math.Max(0, room));
            }

            if (amount <= 0)
                return;

            target.TemporaryHp.Add(new TemporaryHpPool
            {
                Key = effect.Key,
                Amount = amount,
                ExpiresAt = _now + effect.DurationSeconds
            });
            source.Stats.TemporaryHpGranted += amount;
        }

        private void RestoreFp(Combatant combatant, double amount, bool refund)
        {
            var restored = Math.Min(amount, combatant.Spec.MaxFp - combatant.Fp);
            if (restored <= 0)
                return;

            combatant.Fp += restored;
            if (refund)
                combatant.Stats.FpRefunded += restored;
        }

        private void RestoreStamina(Combatant combatant, double amount, bool refund)
        {
            var restored = Math.Min(amount, combatant.Spec.MaxStm - combatant.Stm);
            if (restored <= 0)
                return;

            combatant.Stm += restored;
            if (refund)
                combatant.Stats.StmRefunded += restored;
        }

        private void Die(Combatant target, Combatant killer)
        {
            target.IsDead = true;
            target.DiedAt = _now;
            target.Hp = 0;
            target.Casting = null;
            target.QueuedWeaponAbility = null;
            // Statuses end at death; a revived caster takes its stance up again.
            target.Statuses.Clear();
            target.Weapon?.Death();
            target.WeaponTraits.Paired.ClearTransientState();
            target.WeaponTraits.Haste.Reset();
            target.KilledByPlayer = killer != null && IsPlayerVersusPlayer(killer, target);
            // Combat.ApplyDefeatedEnemyEffects runs from the creature death script, which players do not run.
            if (killer?.Weapon != null && killer != target && !killer.IsDead && target.Spec.IsNpc)
                killer.Weapon.DefeatedEnemy();
            // Death ends PvP combat, as the game's PvpCombat.EndOnDeath does, so a revive starts a new one.
            target.PvpCombatStartedAt = double.NegativeInfinity;
            target.LastPvpActivityAt = double.NegativeInfinity;
            target.TemporaryHp.Clear();
            target.DamageReductions.Clear();
            target.HealsOverTime.Clear();
            target.ConvergenceUntil = -1;
            target.ConvergenceAccuracyUntil = -1;
            target.Stats.Deaths++;
            _deaths.Add(new DeathRecord { Name = target.Name, TeamIndex = target.TeamIndex, IsNpc = target.Spec.IsNpc, At = _now, KilledBy = killer?.Name });

            foreach (var combatant in _combatants.Where(c => c.AttackTarget == target))
                combatant.AttackTarget = null;

            foreach (var teamIndex in _sharedTargets.Where(p => p.Value.Target == target).Select(p => p.Key).ToList())
                _sharedTargets.Remove(teamIndex);
        }

        /// <param name="at">A moment within the current step; only enemies who were there by then can be picked.</param>
        private Combatant EnsureAttackTarget(Combatant attacker, double? at = null)
        {
            var when = at ?? _now;
            var team = _scenario.Teams[attacker.TeamIndex];
            if (team.TargetPolicy == TargetPolicy.Coordinated)
                return EnsureSharedTarget(attacker, team, when);

            if (KeepsCurrentTarget(attacker, team, when))
                return attacker.AttackTarget;

            var enemies = EnemiesAt(attacker, when).ToList();
            if (enemies.Count == 0)
                return null;

            Combatant target = null;
            if (team.TargetPolicy == TargetPolicy.Focus)
                target = FindFocusTarget(enemies, team);

            if (target == null && team.TargetPolicy == TargetPolicy.LowestHp)
                target = PickLowestHp(enemies, 1)[0];

            target ??= enemies[_random.Next(enemies.Count)];
            attacker.AttackTarget = target;
            attacker.AttackTargetPickedAt = _now;
            return target;
        }

        private bool KeepsCurrentTarget(Combatant attacker, TeamSpec team, double when)
        {
            var current = attacker.AttackTarget;
            if (current is not { IsDead: false } || current.AvailableSince > when)
                return false;

            if (team.RetargetSeconds > 0 && _now - attacker.AttackTargetPickedAt >= team.RetargetSeconds)
                return false;

            // A focus target that joins the fight after the attacker settled on someone else takes over.
            return team.TargetPolicy != TargetPolicy.Focus
                   || IsFocusTarget(current, team)
                   || FindFocusTarget(EnemiesAt(attacker, when), team) == null;
        }

        private Combatant EnsureSharedTarget(Combatant attacker, TeamSpec team, double when)
        {
            if (_sharedTargets.TryGetValue(attacker.TeamIndex, out var shared)
                && !shared.Target.IsDead
                && shared.Target.AvailableSince <= when
                && !(team.RetargetSeconds > 0 && _now - shared.PickedAt >= team.RetargetSeconds))
                return shared.Target;

            var enemies = EnemiesAt(attacker, when).ToList();
            if (enemies.Count == 0)
                return null;

            var sample = enemies.Count <= CoordinatedSampleSize
                ? enemies
                : enemies.OrderBy(_ => _random.Next()).Take(CoordinatedSampleSize).ToList();
            var target = PickLowestHp(sample, 1)[0];
            _sharedTargets[attacker.TeamIndex] = new SharedTarget { Target = target, PickedAt = _now };
            return target;
        }

        // An exact name, or the base name of a combatant entry with a Count (its copies are numbered).
        private static bool IsFocusTarget(Combatant combatant, TeamSpec team) =>
            !string.IsNullOrEmpty(team.FocusTarget)
            && (combatant.Name == team.FocusTarget || (combatant.Spec.Count > 1 && combatant.Spec.Name == team.FocusTarget));

        private static Combatant FindFocusTarget(IEnumerable<Combatant> enemies, TeamSpec team) =>
            enemies.FirstOrDefault(e => IsFocusTarget(e, team));

        /// <summary>
        /// The <paramref name="count"/> combatants with the lowest HP percent counting temporary HP, as the game's
        /// current HP does; ties are broken randomly.
        /// </summary>
        private List<Combatant> PickLowestHp(IEnumerable<Combatant> pool, int count) =>
            pool.Select(c => (Combatant: c, Tie: _random.Next()))
                .OrderBy(entry => entry.Combatant.HpPercentWithTemporaryHp)
                .ThenBy(entry => entry.Tie)
                .Take(count)
                .Select(entry => entry.Combatant)
                .ToList();

        private IEnumerable<Combatant> PartyTargets(Combatant caster, int? maxTargets, double? at = null)
        {
            var when = at ?? _now;
            var allies = Allies(caster).Where(ally => ally.AvailableSince <= when).ToList();
            return maxTargets.HasValue && maxTargets.Value < allies.Count ? PickLowestHp(allies, maxTargets.Value) : allies;
        }

        private bool IsEngaged(Combatant combatant) => !combatant.IsDead && _now >= combatant.Spec.JoinAtSeconds;

        private IEnumerable<Combatant> Allies(Combatant combatant) =>
            _combatants.Where(c => IsEngaged(c) && c.TeamIndex == combatant.TeamIndex);

        private IEnumerable<Combatant> Enemies(Combatant combatant) =>
            _combatants.Where(c => IsEngaged(c) && c.TeamIndex != combatant.TeamIndex);

        private IEnumerable<Combatant> EnemiesAt(Combatant combatant, double at) =>
            Enemies(combatant).Where(enemy => enemy.AvailableSince <= at);

        private bool IsPlayerVersusPlayer(Combatant attacker, Combatant target) =>
            _scenario.IsPvp && !attacker.Spec.IsNpc && !target.Spec.IsNpc;

        private void MarkPvpActivity(Combatant combatant)
        {
            if (_now - combatant.LastPvpActivityAt > _rules.PvpCombatWindowSeconds)
                combatant.PvpCombatStartedAt = _now;

            combatant.LastPvpActivityAt = _now;
        }

        private bool IsInPvpCombat(Combatant combatant) =>
            _scenario.IsPvp && !combatant.Spec.IsNpc && _now - combatant.LastPvpActivityAt <= _rules.PvpCombatWindowSeconds;

        private double PvpSeconds(Combatant combatant) => _now - combatant.PvpCombatStartedAt;
    }
}
