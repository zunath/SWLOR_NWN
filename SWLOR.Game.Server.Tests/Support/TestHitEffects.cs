#nullable enable
using FluentAssertions;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using System.Collections;
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

public sealed partial class TestHitEffects : CombatHitEffectsService
{
    public TestHitEffects(
        ICombatQueriesService queries,
        ICombatResourcesService resources,
        ICombatAttackTimingService attackTiming,
        ICombatSuppressionService suppression,
        ICombatActivityService combatActivity,
        ICombatRepeatedTargetService repeatedTarget,
        ICombatAbilityHitAdjustmentService abilityHit,
        IStatService stat,
        IStatusEffectService statusEffect,
        IStatusEffectApplicationService statusApplication,
        IStatusEffectDurationService statusDuration,
        ITemporaryStatModifierService temporaryStatModifier,
        IAbilityImpactState ability,
        ICombatRecorderService combatRecorder,
        IPartyRoster party,
        ICombatDealtEffectsService dealtEffects,
        ICombatTriggeredDamageService triggeredDamage)
        : base(queries, resources, attackTiming, suppression, combatActivity, repeatedTarget, abilityHit, stat, statusEffect, statusApplication,
            statusDuration, temporaryStatModifier, ability, combatRecorder, party, dealtEffects, triggeredDamage)
    {
    }
}
