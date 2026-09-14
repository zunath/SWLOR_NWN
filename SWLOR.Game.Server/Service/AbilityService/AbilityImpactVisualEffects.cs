using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>Prevents several successful riders from replaying the same receipt burst on one recipient.</summary>
    public sealed class AbilityImpactVisualEffects
    {
        private readonly HashSet<uint> _recipients = new();
        public VisualEffect Effect { get; }

        public AbilityImpactVisualEffects(VisualEffect effect)
        {
            Effect = effect;
        }

        public bool TryRecordRecipient(uint recipient)
        {
            return Effect != VisualEffect.None && _recipients.Add(recipient);
        }
    }
}
