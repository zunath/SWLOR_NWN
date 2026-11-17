#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-1 (stays): the leash's enmity table calls.
public sealed partial class DefaultSeamsAIService
{
    public Dictionary<uint, int> DefaultEnmityTable(uint enemy) => GetEnmityTable(enemy);

    public void DefaultRemoveEnmity(uint creature, uint enemy) => RemoveEnmity(creature, enemy);

    public void DefaultClearEnmityTable(uint enemy) => ClearEnmityTable(enemy);
}
