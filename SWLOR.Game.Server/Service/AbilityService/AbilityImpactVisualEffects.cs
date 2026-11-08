using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Prevents several successful riders from replaying the same receipt burst on one recipient,
    /// and limits how many recipients one impact can light up. Bursts blend additively, so an area
    /// ability playing one on every target washes the group out to solid white.
    /// </summary>
    public sealed class AbilityImpactVisualEffects
    {
        public const int MaxRecipients = 3;

        private readonly HashSet<uint> _recipients = new();
        public VisualEffect Effect { get; }

        public AbilityImpactVisualEffects(VisualEffect effect)
        {
            Effect = effect;
        }

        public bool TryRecordRecipient(uint recipient)
        {
            return Effect != VisualEffect.None &&
                   _recipients.Count < MaxRecipients &&
                   _recipients.Add(recipient);
        }
    }
}
