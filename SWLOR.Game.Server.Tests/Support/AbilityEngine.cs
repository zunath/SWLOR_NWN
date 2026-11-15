using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The recording engine, which also writes the calls whose order matters into the bench's log.</summary>
public sealed class AbilityEngine : FakeNWScript, INWScript
{
    private readonly AbilityBench _bench;

    public AbilityEngine(AbilityBench bench)
    {
        _bench = bench;
    }

    void INWScript.ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget, float fDuration)
    {
        ApplyEffectToObject(nDurationType, eEffect, oTarget, fDuration);
        _bench.Log.Add($"Effect:{_bench.EffectLabel(eEffect)}:{oTarget}");
    }

    void INWScript.ApplyEffectAtLocation(DurationType nDurationType, Effect eEffect, Location lLocation, float fDuration)
    {
        ApplyEffectAtLocation(nDurationType, eEffect, lLocation, fDuration);
        _bench.Log.Add($"EffectAt:{_bench.EffectLabel(eEffect)}");
    }

    void INWScript.SendMessageToPC(uint oPlayer, string szMessage)
    {
        SendMessageToPC(oPlayer, szMessage);
        _bench.Log.Add($"Message:{oPlayer}:{szMessage}");
    }

    void INWScript.AssignCommand(uint oActionSubject, Action aActionToAssign)
    {
        AssignCommand(oActionSubject, aActionToAssign);
        _bench.Log.Add($"AssignCommand:{oActionSubject}");
    }

    void INWScript.RemoveEffectByTag(uint creature, params string[] tags)
    {
        RemoveEffectByTag(creature, tags);
        _bench.Log.Add($"RemoveEffectByTag:{creature}:{string.Join(",", tags)}");
    }
}
