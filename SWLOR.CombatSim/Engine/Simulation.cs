using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// Runs one fight on a fixed time step. Damage comes from scenario profiles (calibrated against combat logs);
    /// heals, shields, costs, recasts, regeneration and refunds follow the game's rules under the chosen <see cref="RuleSet"/>.
    /// </summary>
    public sealed class Simulation
    {
        private const double HeartbeatSeconds = 6.0;
        private const int HeartbeatsPerRegenCycle = 5;
        private const float ConvergencePercentPerTick = 4f;
        private const double ConvergenceTickSeconds = 2.0;
        private const double ConvergenceCooldownSeconds = 45.0;
        private const int HarmonyStamina = 8;
        private const int HarmonyBaseChance = 10;
        private const int HarmonyMaximumChance = 40;
        private const int CoordinatedSampleSize = 3;

        private readonly ScenarioSpec _scenario;
        private readonly RuleSet _rules;
        private readonly AbilityCatalog _catalog;
        private readonly Random _random;
        private readonly List<Combatant> _combatants = new();
        private readonly List<DeathRecord> _deaths = new();
        private readonly Dictionary<AbilityUseSpec, (AbilityInfo Info, AbilityEffect Effect)> _resolvedUses = new();
        private readonly Dictionary<int, SharedTarget> _sharedTargets = new();
        private int _revives;
        private double _now;

        public Simulation(ScenarioSpec scenario, RuleSet rules, AbilityCatalog catalog, int seed)
        {
            _scenario = scenario;
            _rules = rules;
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

            for (_now = 0; _now < _scenario.DurationSeconds; _now += step)
            {
                Step(step);

                winner = GetLastTeamStanding();
                if (winner != null)
                    break;
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
            for (var teamIndex = 0; teamIndex < _scenario.Teams.Count; teamIndex++)
            {
                if (_scenario.Teams[teamIndex].RetargetSeconds < 0)
                    throw new InvalidDataException($"Team '{_scenario.Teams[teamIndex].Name}' has a negative RetargetSeconds.");

                foreach (var spec in _scenario.Teams[teamIndex].Members)
                {
                    if (spec.JoinAtSeconds < 0)
                        throw new InvalidDataException($"'{spec.Name}' has a negative JoinAtSeconds.");

                    for (var copy = 1; copy <= Math.Max(1, spec.Count); copy++)
                    {
                        var name = spec.Count > 1 ? $"{spec.Name} {copy}" : spec.Name;
                        var combatant = new Combatant(name, teamIndex, spec)
                        {
                            NextHeartbeatAt = _random.NextDouble() * HeartbeatSeconds,
                            NextAttackAt = _random.NextDouble() * 2.0,
                            NextActionAt = _random.NextDouble()
                        };
                        _combatants.Add(combatant);
                    }

                    foreach (var use in spec.Abilities)
                        ResolveUse(use);
                }
            }
        }

        public void Step(double step)
        {
            foreach (var combatant in _combatants.Where(c => !c.IsDead).ToList())
            {
                ProcessHeartbeat(combatant);
                ProcessConvergence(combatant);
                ProcessHealsOverTime(combatant);
                ProcessPartyPulses(combatant);
                combatant.TemporaryHp.RemoveAll(p => p.ExpiresAt <= _now || p.Amount <= 0);
            }

            var order = _combatants.OrderBy(_ => _random.Next()).ToList();
            foreach (var combatant in order)
            {
                if (!IsEngaged(combatant))
                    continue;

                ProcessCast(combatant);
                Decide(combatant);
                ProcessAutoAttack(combatant);

                if (combatant.IsStarved)
                {
                    combatant.Stats.StarvedSeconds += step;
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
            EffectCatalog.TryGet(feat, out var effect);
            if (effect == null && use.Damage == null)
                throw new InvalidDataException($"'{use.Feat}' has no modeled effect; give it a damage profile or add it to the effect catalog.");

            if (use.MinIntervalSeconds < 0)
                throw new InvalidDataException($"'{use.Feat}' has a negative MinIntervalSeconds.");

            if (use.MaxTargets.HasValue)
            {
                if (use.MaxTargets.Value < 1)
                    throw new InvalidDataException($"'{use.Feat}' needs MaxTargets of at least 1.");

                if (use.Damage != null || effect?.Kind is not (EffectKind.PartyHealOverTime or EffectKind.PartyTemporaryHp))
                    throw new InvalidDataException($"'{use.Feat}' sets MaxTargets but is not a zone or party heal or shield.");
            }

            _resolvedUses[use] = (info, effect);
        }

        private int? GetLastTeamStanding()
        {
            var alive = _combatants.Where(c => !c.IsDead).Select(c => c.TeamIndex).Distinct().ToList();
            return alive.Count == 1 ? alive[0] : alive.Count == 0 ? -1 : null;
        }

        private void ProcessHeartbeat(Combatant combatant)
        {
            if (_now < combatant.NextHeartbeatAt)
                return;

            combatant.NextHeartbeatAt += HeartbeatSeconds;
            combatant.HeartbeatCount++;

            combatant.StmRegenCarry += combatant.Spec.StmRegenPer30Seconds / HeartbeatsPerRegenCycle;
            var stamina = Math.Floor(combatant.StmRegenCarry);
            combatant.StmRegenCarry -= stamina;
            RestoreStamina(combatant, stamina, false);

            if (combatant.HeartbeatCount % HeartbeatsPerRegenCycle != 0)
                return;

            RestoreFp(combatant, Math.Floor(combatant.Spec.FpRegenPer30Seconds), false);
            if (combatant.Spec.HpRegenPer30Seconds > 0)
                ApplyHealing(null, combatant, (int)combatant.Spec.HpRegenPer30Seconds, HealKind.NaturalRegeneration);
        }

        private void ProcessConvergence(Combatant combatant)
        {
            if (combatant.ConvergenceUntil < 0 || _now < combatant.ConvergenceNextTickAt)
                return;

            if (combatant.ConvergenceNextTickAt > combatant.ConvergenceUntil + 1e-6)
            {
                combatant.ConvergenceUntil = -1;
                return;
            }

            RestoreFp(combatant, Math.Floor(combatant.Spec.MaxFp * ConvergencePercentPerTick / 100f), true);
            combatant.ConvergenceNextTickAt += ConvergenceTickSeconds;
        }

        private void ProcessHealsOverTime(Combatant target)
        {
            foreach (var (key, instance) in target.HealsOverTime.ToList())
            {
                if (_now < instance.NextTickAt)
                    continue;

                var source = instance.Source;
                var amount = HealingMath.HealOverTimeTick(
                    instance.Effect,
                    target.Spec.MaxHp,
                    source.Spec.Willpower,
                    instance.AffinityMultiplier,
                    source.Spec.OutgoingHealingPercent);
                ApplyHealing(source, target, amount, HealKind.OverTime);

                instance.TicksLeft--;
                instance.NextTickAt += instance.Effect.TickSeconds;
                if (instance.TicksLeft <= 0)
                    target.HealsOverTime.Remove(key);
            }
        }

        private void ProcessPartyPulses(Combatant caster)
        {
            foreach (var (key, pulse) in caster.PartyPulses.ToList())
            {
                if (_now < pulse.NextTickAt)
                    continue;

                PulseParty(caster, pulse.Effect, pulse.MaxTargets);
                pulse.TicksLeft--;
                pulse.NextTickAt += pulse.Effect.TickSeconds;
                if (pulse.TicksLeft <= 0)
                    caster.PartyPulses.Remove(key);
            }
        }

        private void PulseParty(Combatant caster, AbilityEffect effect, int? maxTargets)
        {
            foreach (var ally in PartyTargets(caster, maxTargets))
            {
                var amount = HealingMath.PartyPulse(
                    effect,
                    ally.Spec.MaxHp,
                    caster.Spec.Willpower,
                    caster.Spec.ForceAffinity,
                    _rules.ForceAffinityHealingCap,
                    caster.Spec.OutgoingHealingPercent,
                    caster.Spec.CombatReadinessPercent);
                ApplyHealing(caster, ally, amount, HealKind.OverTime);
            }
        }

        private void ProcessCast(Combatant caster)
        {
            var cast = caster.Casting;
            if (cast == null || _now < cast.ImpactAt)
                return;

            caster.Casting = null;
            Resolve(caster, cast);
        }

        private void Decide(Combatant caster)
        {
            if (caster.Casting != null || _now < caster.NextActionAt)
                return;

            var starved = false;
            foreach (var use in caster.Spec.Abilities)
            {
                var (info, effect) = _resolvedUses[use];
                if (caster.RecastReadyAt.TryGetValue(info.RecastGroup, out var readyAt) && _now < readyAt)
                    continue;

                if (use.MinIntervalSeconds > 0 && caster.LastUsedAt.TryGetValue(use.Feat, out var lastUsedAt) && _now < lastUsedAt + use.MinIntervalSeconds)
                    continue;

                var target = ResolveTarget(caster, use, effect);
                if (target == null || !ConditionHolds(caster, use, effect, target))
                    continue;

                var fp = _rules.GetFpCost(info);
                var stm = _rules.GetStmCost(info);
                if (caster.Fp < fp || caster.Stm < stm)
                {
                    starved = true;
                    continue;
                }

                if (effect?.Kind == EffectKind.Revive && !CanRevive(caster, target))
                {
                    caster.Stats.BlockedRevives++;
                    continue;
                }

                StartCast(caster, use, info, effect, target, fp, stm);
                caster.IsStarved = false;
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
                InterruptedByDamage = effect?.Kind == EffectKind.Revive
                                      && _rules.Revives == ReviveRule.BlockedInPvpLimitedInPve
                                      && !_scenario.IsPvp,
                FpCost = fp,
                StmCost = stm
            };

            caster.NextActionAt = _now + info.CastSeconds + caster.Spec.ReactionSeconds;
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
            if (isRevive ? !target.IsDead : target.IsDead)
                return;

            if (caster.Fp < cast.FpCost || caster.Stm < cast.StmCost)
            {
                caster.IsStarved = true;
                return;
            }

            caster.Fp -= cast.FpCost;
            caster.Stm -= cast.StmCost;
            caster.Stats.FpSpent += cast.FpCost;
            caster.Stats.StmSpent += cast.StmCost;
            caster.Stats.AbilitiesUsed++;
            caster.RecastReadyAt[cast.Ability.RecastGroup] = _now + _rules.GetRecastSeconds(cast.Ability);
            caster.LastUsedAt[cast.Use.Feat] = _now;

            if (cast.FpCost > 0)
                TriggerConvergence(caster);

            if (cast.Use.Damage != null)
            {
                ApplyAbilityDamage(caster, target, cast.Use.Damage);
                return;
            }

            switch (effect.Kind)
            {
                case EffectKind.DirectHeal:
                    var heal = HealingMath.DirectHeal(
                        effect,
                        target.Spec.MaxHp,
                        target.HpPercent,
                        target == caster,
                        caster.Spec.Willpower,
                        caster.Spec.ForceAffinity,
                        _rules.ForceAffinityHealingCap,
                        caster.Spec.OutgoingHealingPercent,
                        caster.Spec.CombatReadinessPercent);
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
                        PulseParty(caster, effect, cast.Use.MaxTargets);
                        break;
                    }

                    caster.PartyPulses[effect.Key] = new PartyPulseInstance
                    {
                        Effect = effect,
                        MaxTargets = cast.Use.MaxTargets,
                        TicksLeft = effect.Ticks,
                        NextTickAt = _now + effect.TickSeconds
                    };
                    break;

                case EffectKind.TemporaryHp:
                    GrantTemporaryHp(caster, target, effect);
                    break;

                case EffectKind.PartyTemporaryHp:
                    foreach (var ally in PartyTargets(caster, cast.Use.MaxTargets))
                        GrantTemporaryHp(caster, ally, effect);
                    break;

                case EffectKind.Revive:
                    Revive(caster, target, effect);
                    break;
            }
        }

        private void TriggerConvergence(Combatant caster)
        {
            if (!caster.HasPerk(Combatant.ForceConvergencePerk) || _now < caster.ConvergenceReadyAt)
                return;

            caster.ConvergenceUntil = _now + _rules.ConvergenceDurationSeconds;
            caster.ConvergenceNextTickAt = _now + ConvergenceTickSeconds;
            caster.ConvergenceReadyAt = _now + ConvergenceCooldownSeconds;
        }

        private Combatant ResolveTarget(Combatant caster, AbilityUseSpec use, AbilityEffect effect)
        {
            switch (use.Target)
            {
                case AbilityTarget.Self:
                    return caster;
                case AbilityTarget.LowestAlly:
                    return Allies(caster).OrderBy(a => a.HpPercent).FirstOrDefault();
                case AbilityTarget.NamedAlly:
                    return Allies(caster).FirstOrDefault(a => a.Name == use.TargetName);
                case AbilityTarget.DeadAlly:
                    return _combatants
                        .Where(c => c.IsDead && c.TeamIndex == caster.TeamIndex)
                        .OrderBy(c => c.DiedAt)
                        .FirstOrDefault();
                default:
                    return EnsureAttackTarget(caster);
            }
        }

        private bool ConditionHolds(Combatant caster, AbilityUseSpec use, AbilityEffect effect, Combatant target)
        {
            if (use.Target == AbilityTarget.DeadAlly)
                return true;

            if (use.BelowHpPercent.HasValue && target.HpPercent >= use.BelowHpPercent.Value)
                return false;

            if (!use.KeepUp || effect == null)
                return true;

            return effect.Kind switch
            {
                EffectKind.HealOverTime => !target.HealsOverTime.ContainsKey(effect.Key),
                EffectKind.PartyHealOverTime => !caster.PartyPulses.ContainsKey(effect.Key),
                EffectKind.TemporaryHp => target.TemporaryHp.All(p => p.Key != effect.Key),
                EffectKind.PartyTemporaryHp => caster.TemporaryHp.All(p => p.Key != effect.Key),
                _ => true
            };
        }

        private bool CanRevive(Combatant caster, Combatant target)
        {
            switch (_rules.Revives)
            {
                case ReviveRule.BlockedInPvpLimitedInPve when _scenario.IsPvp:
                    // Without positions, every living enemy that has joined the fight counts as within 20m.
                    var enemiesAlive = Enemies(caster).Any();
                    var partyRecentlyHit = Allies(caster).Any(a => _now - a.LastDamageTakenAt <= _rules.PvpReviveDamageWindowSeconds);
                    return !enemiesAlive && !partyRecentlyHit;
                case ReviveRule.BlockedInPvpLimitedInPve:
                    return _now >= target.ReviveLockedUntil;
                default:
                    return true;
            }
        }

        private void Revive(Combatant caster, Combatant target, AbilityEffect effect)
        {
            target.IsDead = false;
            target.Hp = 1;
            target.NextHeartbeatAt = Math.Max(target.NextHeartbeatAt, _now + HeartbeatSeconds);
            target.Stats.RevivesReceived++;
            _revives++;

            if (_rules.Revives == ReviveRule.BlockedInPvpLimitedInPve && !_scenario.IsPvp)
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
                caster.Spec.OutgoingHealingPercent,
                0f);
            ApplyHealing(caster, target, heal, HealKind.Revive);
        }

        private void ProcessAutoAttack(Combatant attacker)
        {
            var attack = attacker.Spec.AutoAttack;
            if (attack == null || attack.AttacksPerSecond <= 0 || attacker.Casting != null || _now < attacker.NextAttackAt)
                return;

            attacker.NextAttackAt = Math.Max(attacker.NextAttackAt + 1.0 / attack.AttacksPerSecond, _now);
            var target = EnsureAttackTarget(attacker);
            if (target == null)
                return;

            RollAndApply(attacker, target, attack.Damage);
        }

        private void ApplyAbilityDamage(Combatant attacker, Combatant target, DamageSpec damage)
        {
            var targets = damage.Area ? Enemies(attacker).ToList() : new List<Combatant> { target };
            foreach (var victim in targets)
            {
                for (var hit = 0; hit < Math.Max(1, damage.Hits); hit++)
                    RollAndApply(attacker, victim, damage);
            }
        }

        private void RollAndApply(Combatant attacker, Combatant target, DamageSpec damage)
        {
            if (target.IsDead || _random.NextDouble() >= damage.GetHitChance(target.Name))
                return;

            var amount = damage.Min + _random.NextDouble() * Math.Max(0, damage.Max - damage.Min);
            if (damage.CritChance > 0 && _random.NextDouble() < damage.CritChance)
                amount *= damage.CritMultiplier;

            ApplyDamage(attacker, target, amount);
        }

        public void ApplyDamage(Combatant attacker, Combatant target, double amount)
        {
            if (target.IsDead || amount <= 0)
                return;

            if (IsPlayerVersusPlayer(attacker, target))
            {
                MarkPvpActivity(attacker);
                MarkPvpActivity(target);
                amount *= 1 + RuleSet.GetStepPercent(_rules.PvpDamageBackstop, PvpSeconds(target)) / 100f;
            }

            amount *= 1 - target.Spec.DamageReductionPercent / 100f;

            foreach (var pool in target.TemporaryHp.OrderBy(p => p.ExpiresAt))
            {
                if (amount <= 0)
                    break;

                var absorbed = Math.Min(pool.Amount, amount);
                pool.Amount -= absorbed;
                amount -= absorbed;
                target.Stats.DamageAbsorbedByTemporaryHp += absorbed;
                attacker.Stats.DamageDealt += absorbed;
            }

            target.TemporaryHp.RemoveAll(p => p.Amount <= 0);
            target.LastDamageTakenAt = _now;

            if (amount > 0)
            {
                var dealt = Math.Min(amount, target.Hp);
                target.Hp -= amount;
                target.Stats.DamageTakenToHp += dealt;
                attacker.Stats.DamageDealt += dealt;
            }

            if (target.Casting?.InterruptedByDamage == true)
            {
                target.Casting = null;
                target.Stats.InterruptedCasts++;
                target.NextActionAt = _now + target.Spec.ReactionSeconds;
            }

            if (target.Hp <= 0)
                Die(target, attacker);
        }

        public void ApplyHealing(Combatant source, Combatant target, int amount, HealKind kind)
        {
            if (target.IsDead || amount <= 0)
                return;

            double adjusted = kind == HealKind.NaturalRegeneration
                ? amount
                : HealingMath.AddPercent(amount, target.Spec.HealingReceivedPercent);

            if (IsInPvpCombat(target))
            {
                var ramp = RuleSet.GetStepPercent(_rules.PvpHealingRamp, PvpSeconds(target));
                adjusted *= _rules.PvpHealingMultiplier * (1 - ramp / 100f);
            }

            var effective = Math.Min(adjusted, target.Spec.MaxHp - target.Hp);
            if (effective < 0)
                effective = 0;

            target.Hp += effective;
            target.Stats.HealingReceived += effective;
            target.Stats.Overhealing += adjusted - effective;
            if (source != null)
                source.Stats.HealingDone += effective;

            if (effective > 0 && kind != HealKind.NaturalRegeneration)
                TryDefensiveHarmony(target, kind);
        }

        private void TryDefensiveHarmony(Combatant target, HealKind kind)
        {
            if (!target.HasPerk(Combatant.DefensiveHarmonyPerk))
                return;
            if (_rules.HarmonyInstantHealsOnly && kind != HealKind.InstantAbility)
                return;
            if (_now < target.HarmonyReadyAt)
                return;

            var chance = Math.Min(HarmonyMaximumChance, HarmonyBaseChance + target.Spec.Might);
            if (_random.Next(100) >= chance)
                return;

            RestoreStamina(target, HarmonyStamina, true);
            target.HarmonyReadyAt = _now + _rules.HarmonyCooldownSeconds;
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
                source.Spec.CombatReadinessPercent);

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
            target.TemporaryHp.Clear();
            target.HealsOverTime.Clear();
            target.PartyPulses.Clear();
            target.ConvergenceUntil = -1;
            target.Stats.Deaths++;
            _deaths.Add(new DeathRecord { Name = target.Name, TeamIndex = target.TeamIndex, At = _now, KilledBy = killer?.Name });

            foreach (var combatant in _combatants.Where(c => c.AttackTarget == target))
                combatant.AttackTarget = null;

            foreach (var teamIndex in _sharedTargets.Where(p => p.Value.Target == target).Select(p => p.Key).ToList())
                _sharedTargets.Remove(teamIndex);
        }

        private Combatant EnsureAttackTarget(Combatant attacker)
        {
            var team = _scenario.Teams[attacker.TeamIndex];
            if (team.TargetPolicy == TargetPolicy.Coordinated)
                return EnsureSharedTarget(attacker, team);

            if (KeepsCurrentTarget(attacker, team))
                return attacker.AttackTarget;

            var enemies = Enemies(attacker).ToList();
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

        private bool KeepsCurrentTarget(Combatant attacker, TeamSpec team)
        {
            var current = attacker.AttackTarget;
            if (current is not { IsDead: false })
                return false;

            if (team.RetargetSeconds > 0 && _now - attacker.AttackTargetPickedAt >= team.RetargetSeconds)
                return false;

            // A focus target that joins the fight after the attacker settled on someone else takes over.
            return team.TargetPolicy != TargetPolicy.Focus
                   || IsFocusTarget(current, team)
                   || FindFocusTarget(Enemies(attacker), team) == null;
        }

        private Combatant EnsureSharedTarget(Combatant attacker, TeamSpec team)
        {
            if (_sharedTargets.TryGetValue(attacker.TeamIndex, out var shared)
                && !shared.Target.IsDead
                && !(team.RetargetSeconds > 0 && _now - shared.PickedAt >= team.RetargetSeconds))
                return shared.Target;

            var enemies = Enemies(attacker).ToList();
            if (enemies.Count == 0)
                return null;

            var sample = enemies.Count <= CoordinatedSampleSize
                ? enemies
                : enemies.OrderBy(_ => _random.Next()).Take(CoordinatedSampleSize).ToList();
            var target = PickLowestHp(sample, 1)[0];
            _sharedTargets[attacker.TeamIndex] = new SharedTarget { Target = target, PickedAt = _now };
            return target;
        }

        private static bool IsFocusTarget(Combatant combatant, TeamSpec team) =>
            !string.IsNullOrEmpty(team.FocusTarget)
            && (combatant.Name == team.FocusTarget || combatant.Name.StartsWith(team.FocusTarget + " "));

        private static Combatant FindFocusTarget(IEnumerable<Combatant> enemies, TeamSpec team) =>
            enemies.FirstOrDefault(e => IsFocusTarget(e, team));

        /// <summary>The <paramref name="count"/> combatants with the lowest HP percent; ties are broken randomly.</summary>
        private List<Combatant> PickLowestHp(IEnumerable<Combatant> pool, int count) =>
            pool.Select(c => (Combatant: c, Tie: _random.Next()))
                .OrderBy(entry => entry.Combatant.HpPercent)
                .ThenBy(entry => entry.Tie)
                .Take(count)
                .Select(entry => entry.Combatant)
                .ToList();

        private IEnumerable<Combatant> PartyTargets(Combatant caster, int? maxTargets)
        {
            var allies = Allies(caster).ToList();
            return maxTargets.HasValue && maxTargets.Value < allies.Count ? PickLowestHp(allies, maxTargets.Value) : allies;
        }

        private bool IsEngaged(Combatant combatant) => !combatant.IsDead && _now >= combatant.Spec.JoinAtSeconds;

        private IEnumerable<Combatant> Allies(Combatant combatant) =>
            _combatants.Where(c => IsEngaged(c) && c.TeamIndex == combatant.TeamIndex);

        private IEnumerable<Combatant> Enemies(Combatant combatant) =>
            _combatants.Where(c => IsEngaged(c) && c.TeamIndex != combatant.TeamIndex);

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
