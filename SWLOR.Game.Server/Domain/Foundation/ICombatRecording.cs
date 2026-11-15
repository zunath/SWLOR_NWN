using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// What the domains below combat tell the combat recorder: the scope a change belongs to and the resource and item
    /// changes it records. Foundation owns the interface and the recorder implements it (supply rule 3 of
    /// MaintainabilityRefactorDomainOrder.md: a notice, never a question). Hooks check <see cref="IsRecording"/> first, so a
    /// call costs one flag check while no recording runs and nothing is allocated either way.
    /// </summary>
    public interface ICombatRecording : ICombatRecordingState
    {
        void RestoreAttributionDepth(int depth);

        /// <summary>
        /// Attributes changes made inside the returned scope to an actor and source.
        /// </summary>
        CombatRecordAttributionScope BeginAttribution(
            uint actor,
            CombatRecordSourceKind kind,
            string name = null,
            int code = 0,
            int rank = 0);

        CombatRecordAttributionScope BeginItemAttribution(uint user, uint item);

        /// <summary>
        /// Starts the reactions to a heal on <paramref name="creature"/> that is applied right after
        /// them (a stamina restore on healing received). Their events are written after that heal.
        /// </summary>
        CombatRecordHealingReactionScope BeginHealingReaction(uint creature);

        void EndHealingReaction();

        /// <summary>
        /// Records an FP or STM change that already happened.
        /// </summary>
        void RecordResourceChange(uint creature, CombatRecordResource resource, int delta, int valueAfter);

        /// <summary>
        /// Records an FP or STM reduction that is about to clamp the pool at zero.
        /// </summary>
        void RecordResourceReduction(uint creature, CombatRecordResource resource, int reduceBy, int currentValue);

        /// <summary>
        /// Records a stack reduction made by combat activity (an item use, an ability's cost).
        /// Reductions outside any scope, such as quest turn-ins, are not combat events.
        /// </summary>
        void RecordItemStackReduced(uint item, int quantity);

        /// <summary>
        /// Records a charge an activated item spent, attributed to that item.
        /// </summary>
        void RecordItemChargeUsed(uint user, uint item);
    }
}
