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

internal sealed partial class TestWaiver
{
    public TestWaiver(bool consumes)
    {
        Effect = ScriptedProxy.Create<IDeathDebtWaiverStatusEffect>(out var proxy);
        proxy.On("TryConsumeDeathDebtWaiver", _ =>
        {
            Asked = true;
            return consumes;
        });
    }

    public IDeathDebtWaiverStatusEffect Effect { get; }

    public bool Asked { get; private set; }
}
