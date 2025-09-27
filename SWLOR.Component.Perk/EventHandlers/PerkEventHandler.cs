using SWLOR.Component.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Perk.EventHandlers
{
    /// <summary>
    /// Event handlers for the Perk component.
    /// </summary>
    public class PerkEventHandler
    {
        private readonly IPerkService _perkService;
        private readonly IUsePerkFeat _usePerkFeat;
        private readonly IPerkEffectService _perkEffectService;

        public PerkEventHandler(
            IPerkService perkService,
            IUsePerkFeat usePerkFeat,
            IPerkEffectService perkEffectService)
        {
            _perkService = perkService;
            _usePerkFeat = usePerkFeat;
            _perkEffectService = perkEffectService;
        }

        /// <summary>
        /// When the module loads, cache all perk and character type information.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _perkService.CacheData();
        }

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorLoseSkill)]
        public void RemovePerkLevelOnSkillDecay()
        {
            _perkService.RemovePerkLevelOnSkillDecay();
        }

        /// <summary>
        /// When a creature uses any feat, this will check and see if the feat is registered with the perk system.
        /// If it is, requirements to use the feat will be checked and then the ability will activate.
        /// If there are errors at any point in this process, the creature will be notified and the execution will end.
        /// </summary>
        [ScriptHandler<OnFeatUseBefore>]
        public void UseFeat()
        {
            _usePerkFeat.UseFeat();
        }

        /// <summary>
        /// When a weapon hits, process any queued weapon abilities.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemHit)]
        public void ProcessQueuedWeaponAbility()
        {
            _usePerkFeat.ProcessQueuedWeaponAbility();
        }

        /// <summary>
        /// When a player enters the module, clear any temporary queued variables.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void ClearTemporaryQueuedVariables()
        {
            _usePerkFeat.ClearTemporaryQueuedVariables();
        }

        /// <summary>
        /// When a player starts resting, clear any temporary queued variables.
        /// </summary>
        [ScriptHandler(ScriptName.OnRestStarted)]
        public void ClearTemporaryQueuedVariablesOnRest()
        {
            _usePerkFeat.ClearTemporaryQueuedVariablesOnRest();
        }

        /// <summary>
        /// When a player equips an item, clear any temporary queued variables.
        /// </summary>
        [ScriptHandler<OnSWLORItemEquipValidBefore>]
        public void ClearTemporaryQueuedVariablesOnEquip()
        {
            _usePerkFeat.ClearTemporaryQueuedVariablesOnEquip();
        }

        /// <summary>
        /// When a weapon hits, apply Alacrity and Clarity effects for OneHanded perks.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemHit)]
        public void ApplyAlacrityAndClarity()
        {
            _perkEffectService.ApplyAlacrityAndClarity();
        }

        /// <summary>
        /// When a weapon hits, process Force Link for Beast Force perks.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemHit)]
        public void OnForceLinkHit()
        {
            _perkEffectService.OnForceLinkHit();
        }

        /// <summary>
        /// When a weapon hits, process Endurance Link for Beast Bruiser perks.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemHit)]
        public void OnEnduranceLinkHit()
        {
            _perkEffectService.OnEnduranceLinkHit();
        }
    }
}
