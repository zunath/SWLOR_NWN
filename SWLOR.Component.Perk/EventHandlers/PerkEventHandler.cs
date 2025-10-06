using SWLOR.Component.Perk.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;

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
            IPerkEffectService perkEffectService,
            IEventAggregator eventAggregator)
        {
            _perkService = perkService;
            _usePerkFeat = usePerkFeat;
            _perkEffectService = perkEffectService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheData());
            eventAggregator.Subscribe<OnPlayerLoseSkillRank>(e => RemovePerkLevelOnSkillDecay());
            eventAggregator.Subscribe<OnFeatUseBefore>(e => UseFeat());
            eventAggregator.Subscribe<OnItemHit>(e => ProcessQueuedWeaponAbility());
            eventAggregator.Subscribe<OnModuleEnter>(e => ClearTemporaryQueuedVariables());
            eventAggregator.Subscribe<OnPlayerRestStarted>(e => ClearTemporaryQueuedVariablesOnRest());
            eventAggregator.Subscribe<OnSWLORItemEquipValidBefore>(e => ClearTemporaryQueuedVariablesOnEquip());
            eventAggregator.Subscribe<OnItemHit>(e => ApplyAlacrityAndClarity());
            eventAggregator.Subscribe<OnItemHit>(e => OnForceLinkHit());
            eventAggregator.Subscribe<OnItemHit>(e => OnEnduranceLinkHit());
        }

        /// <summary>
        /// When the module loads, cache all perk and character type information.
        /// </summary>
        public void CacheData()
        {
            _perkService.CacheData();
        }

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        public void RemovePerkLevelOnSkillDecay()
        {
            _perkService.RemovePerkLevelOnSkillDecay();
        }

        /// <summary>
        /// When a creature uses any feat, this will check and see if the feat is registered with the perk system.
        /// If it is, requirements to use the feat will be checked and then the ability will activate.
        /// If there are errors at any point in this process, the creature will be notified and the execution will end.
        /// </summary>
        public void UseFeat()
        {
            _usePerkFeat.UseFeat();
        }

        /// <summary>
        /// When a weapon hits, process any queued weapon abilities.
        /// </summary>
        public void ProcessQueuedWeaponAbility()
        {
            _usePerkFeat.ProcessQueuedWeaponAbility();
        }

        /// <summary>
        /// When a player enters the module, clear any temporary queued variables.
        /// </summary>
        public void ClearTemporaryQueuedVariables()
        {
            _usePerkFeat.ClearTemporaryQueuedVariables();
        }

        /// <summary>
        /// When a player starts resting, clear any temporary queued variables.
        /// </summary>
        public void ClearTemporaryQueuedVariablesOnRest()
        {
            _usePerkFeat.ClearTemporaryQueuedVariablesOnRest();
        }

        /// <summary>
        /// When a player equips an item, clear any temporary queued variables.
        /// </summary>
        public void ClearTemporaryQueuedVariablesOnEquip()
        {
            _usePerkFeat.ClearTemporaryQueuedVariablesOnEquip();
        }

        /// <summary>
        /// When a weapon hits, apply Alacrity and Clarity effects for OneHanded perks.
        /// </summary>
        public void ApplyAlacrityAndClarity()
        {
            _perkEffectService.ApplyAlacrityAndClarity();
        }

        /// <summary>
        /// When a weapon hits, process Force Link for Beast Force perks.
        /// </summary>
        public void OnForceLinkHit()
        {
            _perkEffectService.OnForceLinkHit();
        }

        /// <summary>
        /// When a weapon hits, process Endurance Link for Beast Bruiser perks.
        /// </summary>
        public void OnEnduranceLinkHit()
        {
            _perkEffectService.OnEnduranceLinkHit();
        }
    }
}
