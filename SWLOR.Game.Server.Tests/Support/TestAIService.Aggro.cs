#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-3, AIAggroService: proximity enmity, infiltration and the stealth check.
public sealed partial class TestAIService
{
    protected override bool HasProximityEnmity(uint creature, uint enemy) =>
        Proximity.TryGetValue(enemy, out var set) && set.Contains(creature);

    protected override void ModifyProximityEnmity(uint creature, uint enemy, int amount)
    {
        _bench.Engine.Note($"ModifyProximityEnmity({creature}, {enemy}, {amount})");
        SetProximityEnmity(enemy, creature, amount);
    }

    protected override bool RemoveTrackedProximityEnmity(uint creature, uint enemy)
    {
        _bench.Engine.Note($"RemoveTrackedProximityEnmity({creature}, {enemy})");
        if (!HasProximityEnmity(creature, enemy))
            return false;

        Proximity[enemy].Remove(creature);
        Tables[enemy].Remove(creature);
        return true;
    }

    protected override bool TryBeginInfiltration(uint player, uint npc)
    {
        _bench.Engine.Note($"TryBeginInfiltration({player}, {npc})");
        return InfiltrationBegins.Contains(player);
    }

    protected override void CompleteInfiltration(uint player, uint npc) =>
        _bench.Engine.Note($"CompleteInfiltration({player}, {npc})");

    protected override bool CanAcquireAggro(uint observer, uint target)
    {
        _bench.Engine.Note($"CanAcquireAggro({observer}, {target})");
        return !HiddenFromAggro.Contains(target);
    }
}
