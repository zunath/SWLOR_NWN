#nullable enable
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class TestDamageModifiers : CombatDamageModifiersService
{
    public Func<uint, DeflectionSource, float, bool> Deflection { get; set; } = (_, _, _) => false;
    public List<string> DeflectionCalls { get; } = new();

    public TestDamageModifiers(
        ICombatQueriesService queries,
        ICombatResourcesService resources,
        IStatService stat,
        IStatusEffectService statusEffect,
        IStatusEffectApplicationService statusApplication,
        ITemporaryStatModifierService temporaryStatModifier,
        IAbilityImpactState ability,
        IPlayerFeedbackService playerFeedback)
        : base(queries, resources, stat, statusEffect, statusApplication, temporaryStatModifier, ability, playerFeedback, FacadeForwarder.LazyFor<ICombatDamageTakenService>())
    {
    }

    protected override bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds)
    {
        DeflectionCalls.Add($"{creature} {source} {windowSeconds}");
        return Deflection(creature, source, windowSeconds);
    }

    public bool BaseHasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds) =>
        base.HasRecentDeflection(creature, source, windowSeconds);
}
