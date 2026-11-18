#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using System.Numerics;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class DeathScope : IDisposable
{
    private readonly Action _onDispose;

    public DeathScope(Action onDispose) => _onDispose = onDispose;

    public void Dispose() => _onDispose();
}
