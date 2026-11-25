using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage records part of the combat recorder: damage, healing, temporary hit point and death records, with the
    /// handlers that record creature and player deaths. It writes through the session core and reads the attribution stack of
    /// <see cref="ICombatRecorderService"/>. Static callers reach the container's instance through
    /// <c>GameServices.CombatRecordDamage</c>.
    /// </summary>
    public interface ICombatRecordDamageService
    {
        /// <summary>
        /// Called by the native damage hook after the engine applied a damage effect.
        /// Hit points exclude temporary HP, which is reported separately.
        /// </summary>
        /// <param name="isEngineApplied">True when no script or closure applied the effect, as for weapon hits.</param>
        void RecordDamageApplied(
            int applicationIndex,
            bool isEngineApplied,
            uint source,
            uint target,
            in CombatRecordDamageBreakdown damage,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            CombatRecordTemporaryHitPointPool[] poolsBefore,
            CombatRecordTemporaryHitPointPool[] poolsAfter);

        /// <summary>
        /// Called by the native heal hook after the engine applied a heal effect.
        /// </summary>
        void RecordHealingApplied(
            uint creator,
            uint target,
            int requested,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            int pvpReduced = 0);

        /// <summary>
        /// Called before a keyed temporary-HP pool is removed or replaced. During a hit on that
        /// creature, what each pool had left is kept, so the hit's absorption is measured against
        /// that remainder instead of treating the removed pool as fully absorbed.
        /// </summary>
        void RecordTemporaryHitPointPoolRemoving(uint creature);

        /// <summary>
        /// Records a pool cut back to fit the temporary HP cap, credited to whoever granted it.
        /// </summary>
        void RecordTemporaryHitPointsTrimmed(uint target, string effectKey, int trimmed, int remaining);

        void RecordTemporaryHitPointsGranted(uint source, uint target, string effectKey, int amount, float durationSeconds);

        void RecordCreatureDeath();

        void RecordPlayerDeath();

        /// <summary>
        /// Records a respawn. Called by the respawn handler after the resurrection and before it
        /// sends the player home, which may be outside the recorded area.
        /// </summary>
        void RecordRespawn(uint player);

        /// <summary>
        /// Keeps the HP a lethal hit left when a handler inside the damage call restores the
        /// creature (a fatal-damage save), so the damage event reports the hit, not the recovery.
        /// </summary>
        void KeepLethalHitPoints(uint creature);

        /// <summary>
        /// Records a resurrection, attributed to the innermost scope (for example the reviving
        /// ability's impact) or to <paramref name="kindWithoutScope"/>.
        /// </summary>
        void RecordResurrection(uint target, CombatRecordSourceKind kindWithoutScope = CombatRecordSourceKind.Unattributed);

        /// <summary>
        /// Records a subdual and its resurrection. Call before the subdued player is resurrected:
        /// inside the lethal damage call both events wait for that damage and death, and the HP
        /// the hit left is kept for the damage event.
        /// </summary>
        void RecordSubdual(uint subdued, uint subduedBy);
    }
}
