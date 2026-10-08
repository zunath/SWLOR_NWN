using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PerkService
{
    public sealed class PerkRequirementAnyCompletedLine : IPerkRequirement
    {
        public IReadOnlyList<PerkType> Lines { get; }
        public PerkRequirementAnyCompletedLine(params PerkType[] lines)
        {
            if (lines == null || lines.Length == 0) throw new ArgumentException("At least one perk line is required.", nameof(lines));
            Lines = Array.AsReadOnly(lines.ToArray());
        }
        public PerkRequirementCategory Category => PerkRequirementCategory.MustHavePerk;
        public string RequirementText => $"Complete any one of: {string.Join(", ", Lines.Select(line => Perk.GetPerkDetails(line).Name))}.";
        public string CheckRequirements(uint player) => Lines.Any(line => Perk.GetPerkLevel(player, line) >= 2) ? string.Empty : RequirementText;
    }
}
