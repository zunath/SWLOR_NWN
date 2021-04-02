using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.ImplantService
{
    public class ImplantBuilder
    {
        private readonly Dictionary<string, ImplantDetail> _implants = new Dictionary<string, ImplantDetail>();
        private ImplantDetail _activeImplant;

        /// <summary>
        /// Creates a new implant.
        /// </summary>
        /// <param name="itemTag">The tag of the item which will use these rules.</param>
        /// <returns>An implant builder with the configured options.</returns>
        public ImplantBuilder Create(string itemTag)
        {
            _activeImplant = new ImplantDetail();
            _implants[itemTag] = _activeImplant;

            return this;
        }

        /// <summary>
        /// Sets the name of the active implant we're building.
        /// </summary>
        /// <param name="name">The name to set.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder Name(string name)
        {
            _activeImplant.Name = name;

            return this;
        }

        /// <summary>
        /// Sets the description of the active implant we're building.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder Description(string description)
        {
            _activeImplant.Description = description;

            return this;
        }

        /// <summary>
        /// Sets the implant slot type of the active implant we're building.
        /// </summary>
        /// <param name="slot">The slot to set.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder Slot(ImplantSlotType slot)
        {
            _activeImplant.Slot = slot;

            return this;
        }


        /// <summary>
        /// Sets the required level of the active implant we're building.
        /// </summary>
        /// <param name="requiredLevel">The required level to set.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder RequiredLevel(int requiredLevel)
        {
            _activeImplant.RequiredLevel = requiredLevel;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust HP by a certain amount when installed or uninstalled.
        /// </summary>
        /// <param name="adjustBy">The amount to adjust by.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ModifyHP(int adjustBy)
        {
            _activeImplant.HPAdjustment = adjustBy;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust HP regen by a certain amount when installed or uninstalled.
        /// </summary>
        /// <param name="adjustBy">The amount to adjust by.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ModifyHPRegen(int adjustBy)
        {
            _activeImplant.HPRegenAdjustment = adjustBy;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust FP by a certain amount when installed or uninstalled.
        /// </summary>
        /// <param name="adjustBy">The amount to adjust by.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ModifyFP(int adjustBy)
        {
            _activeImplant.FPAdjustment = adjustBy;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust FP regen by a certain amount when installed or uninstalled.
        /// </summary>
        /// <param name="adjustBy">The amount to adjust by.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ModifyFPRegen(int adjustBy)
        {
            _activeImplant.FPRegenAdjustment = adjustBy;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust STM by a certain amount when installed or uninstalled.
        /// </summary>
        /// <param name="adjustBy">The amount to adjust by.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ModifySTM(int adjustBy)
        {
            _activeImplant.STMAdjustment = adjustBy;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust STM regen by a certain amount when installed or uninstalled.
        /// </summary>
        /// <param name="adjustBy">The amount to adjust by.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ModifySTMRegen(int adjustBy)
        {
            _activeImplant.STMRegenAdjustment = adjustBy;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust a specific ability score when installed or uninstalled.
        /// </summary>
        /// <param name="ability">The ability type</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ModifyAbilityScore(AbilityType ability, int adjustBy)
        {
            _activeImplant.StatAdjustments[ability] = adjustBy;

            return this;
        }

        /// <summary>
        /// Specifies that this implant will adjust the movement rate of the creature when installed or uninstalled.
        /// </summary>
        /// <param name="adjustBy">The amount to adjust by.</param>
        /// <returns></returns>
        public ImplantBuilder ModifyMovementRate(float adjustBy)
        {
            _activeImplant.MovementRateAdjustment = adjustBy;

            return this;
        }

        /// <summary>
        /// Sets the action to run when an implant is installed to a creature.
        /// </summary>
        /// <param name="action">The action to run when an implant is installed.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder InstalledAction(ImplantInstalledDelegate action)
        {
            _activeImplant.InstalledAction = action;

            return this;
        }

        /// <summary>
        /// Sets the action to run when an implant is uninstalled from a creature.
        /// </summary>
        /// <param name="action">The action to run when an implant is uninstalled.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder UninstalledAction(ImplantUninstalledDelegate action)
        {
            _activeImplant.UninstalledAction = action;

            return this;
        }

        /// <summary>
        /// Sets the action to run when validating whether a creature can install the active implant.
        /// </summary>
        /// <param name="action">The action to run when an implant is being validated.</param>
        /// <returns>An implant builder with the configured settings.</returns>
        public ImplantBuilder ValidationAction(ImplantValidationDelegate action)
        {
            _activeImplant.ValidationAction = action;

            return this;
        }

        /// <summary>
        /// Returns a built dictionary of implant details.
        /// </summary>
        /// <returns>A dictionary of implant details.</returns>
        public Dictionary<string, ImplantDetail> Build()
        {
            return _implants;
        }
    }
}
