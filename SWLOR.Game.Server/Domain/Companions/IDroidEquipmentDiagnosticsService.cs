using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Logs equipment changes on player droids outside their summon restore, so reports of droids dropping their
    /// weapons mid-fight can be traced to the code path that moved the item. The static facade
    /// <c>Service.DroidEquipmentDiagnostics</c> forwards to the container's instance of this interface and keeps the
    /// event handlers.
    /// </summary>
    public interface IDroidEquipmentDiagnosticsService
    {
        /// <summary>The most recent entry written, for engine tests.</summary>
        string LastEntry { get; }

        /// <summary>
        /// Native equips (RunEquip): equip actions, inventory drags and queued combat-round equips. The after-event
        /// also follows refused equips; only an item now in a slot was equipped.
        /// </summary>
        void OnEquipped();

        /// <summary>
        /// The after-event runs once the item has left its slot, so note the slot first.
        /// </summary>
        void OnUnequipping();

        /// <summary>
        /// Native unequips (RunUnequip): unequip actions, inventory drags and queued combat-round unequips.
        /// </summary>
        void OnUnequipped();

        /// <summary>
        /// Engine paths such as disarm, polymorph, replacement equips and item level checks change hands
        /// without either event. The heartbeat compares the hands with the last state seen.
        /// </summary>
        void CheckHands();

        /// <summary>Sets the hand baseline once a summon has restored the droid's equipment, without logging the restore.</summary>
        void RememberHands(uint droid);

        /// <summary>Forgets what is remembered about the object that is being destroyed.</summary>
        void Forget();

        string Format(IEnumerable<(string Name, string Value)> fields);

        /// <summary>
        /// Reduces a managed stack trace to the SWLOR methods that led to the change. An empty result means
        /// no SWLOR code made the change directly: an engine action, the action queue or a client message did.
        /// </summary>
        string SummarizeStack(string stackTrace);
    }
}
