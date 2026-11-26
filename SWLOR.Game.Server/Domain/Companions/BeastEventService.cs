using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// The beast event handlers: the twelve scripts a spawned beast runs, the despawn when its master leaves or enters
    /// space, the item check when a beast picks something up, and the stables terminal. The catalog, the IDs and the XP stay
    /// on <see cref="BeastMasteryService"/>. The calls into the companion control, the stat service, the status effects and
    /// the windows are seams, so a unit test can answer them.
    /// </summary>
    public class BeastEventService : IBeastEventService
    {
        private readonly IDBService _db;
        private readonly ICompanionControlService _companionControl;
        private readonly ICreatureStatService _creatureStat;
        private readonly IStatusEffectApplicationService _statusEffect;
        private readonly IItemService _item;
        private readonly IGuiService _gui;
        private readonly IBeastMasteryService _beastMastery;

        public BeastEventService(
            IDBService db,
            ICompanionControlService companionControl,
            ICreatureStatService creatureStat,
            IStatusEffectApplicationService statusEffect,
            IItemService item,
            IGuiService gui,
            IBeastMasteryService beastMastery)
        {
            _db = db;
            _companionControl = companionControl;
            _creatureStat = creatureStat;
            _statusEffect = statusEffect;
            _item = item;
            _gui = gui;
            _beastMastery = beastMastery;
        }

        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        [NWNEventHandler(ScriptName.OnAssociateRemoveBefore)]
        public void RemoveAssociate()
        {
            var player = OBJECT_SELF;
            var beast = GetAssociate(AssociateType.Henchman, player);
            ClearCompanionControl(beast);
            DestroyObject(beast);
        }

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public void OnAcquireItem()
        {
            var beast = GetModuleItemAcquiredBy();
            if (!_beastMastery.IsPlayerBeast(beast))
                return;

            var master = GetMaster(beast);
            var item = GetModuleItemAcquired();
            var type = GetBaseItemType(item);

            // Creature items are OK to acquire.
            if (type == BaseItem.CreatureBludgeonWeapon ||
                type == BaseItem.CreaturePierceWeapon ||
                type == BaseItem.CreatureSlashPierceWeapon ||
                type == BaseItem.CreatureSlashWeapon ||
                type == BaseItem.CreatureItem)
                return;

            SendMessageToPC(master, "Beasts cannot hold items.");
            AssignCommand(beast, () => ClearAllActions());
            ReturnItem(master, item);
        }

        /// <summary>
        /// Gives an item back to its owner. <c>Item.ReturnItem</c> moves the item through the native inventory, which a
        /// unit test cannot run, so a test overrides this.
        /// </summary>
        protected virtual void ReturnItem(uint owner, uint item)
        {
            _item.ReturnItem(owner, item);
        }

        [NWNEventHandler(ScriptName.OnBeastBlocked)]
        public void BeastOnBlocked()
        {
            ExecuteScript("x0_ch_hen_block", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastRoundEnd)]
        public void BeastOnEndCombatRound()
        {
            ProcessCompanionCombatRound(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastConversation)]
        public void BeastOnConversation()
        {
            if (!HandleCompanionConversation(OBJECT_SELF))
                ExecuteScript("x0_ch_hen_conv", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastDamaged)]
        public void BeastOnDamaged()
        {
            RegisterDefensiveThreat(OBJECT_SELF, GetLastDamager(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnBeastDeath)]
        public void BeastOnDeath()
        {
            var beast = OBJECT_SELF;
            var beastId = _beastMastery.GetBeastId(beast);
            var killer = GetLastKiller();
            RecordBeastDeath(
                GetMaster(beast),
                beastId,
                GetIsObjectValid(killer) ? killer : GetLastHostileActor(beast));
            ClearCompanionControl(beast);
            ExecuteScript("x2_hen_death", beast);

            var dbBeast = _db.Get<Beast>(beastId);
            if (dbBeast == null)
                return;

            dbBeast.IsDead = true;

            _db.Set(dbBeast);
        }

        [NWNEventHandler(ScriptName.OnBeastDisturbed)]
        public void BeastOnDisturbed()
        {
            ExecuteScript("x0_ch_hen_distrb", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastHeartbeat)]
        public void BeastOnHeartbeat()
        {
            RestoreBeastStats();
            ProcessCompanionHeartbeat(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastAttacked)]
        public void BeastOnPhysicalAttacked()
        {
            RegisterDefensiveThreat(OBJECT_SELF, GetLastAttacker(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnBeastRest)]
        public void BeastOnRested()
        {
            var beast = OBJECT_SELF;
            ExecuteScript("x0_ch_hen_rest", beast);

            AssignCommand(beast, () => ClearAllActions());

            ApplyRestStatusEffect(beast);
        }

        [NWNEventHandler(ScriptName.OnBeastSpawn)]
        public void BeastOnSpawn()
        {
            var beast = OBJECT_SELF;
            AssignCommand(beast, () =>
            {
                SetIsDestroyable(true, false, false);
            });
            LoadNPCStats();
            ApplyCreatureMovementRate(beast);
            InitializeCompanionControl(beast);
        }

        [NWNEventHandler(ScriptName.OnBeastSpellCast)]
        public void BeastOnSpellCastAt()
        {
            if (GetLastSpellHarmful())
                RegisterDefensiveThreat(OBJECT_SELF, GetLastSpellCaster());
        }

        [NWNEventHandler(ScriptName.OnBeastUserDefined)]
        public void BeastOnUserDefined()
        {
            ExecuteScript("x0_ch_hen_usrdef", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastTerminate)]
        public void OpenStablesMenu()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, ColorToken.Red("Only players may use this terminal."));
                return;
            }

            TogglePlayerWindow(player, GuiWindowType.Stables, null, OBJECT_SELF);
        }

        // The calls below go to services that are not converted yet and keep their state in statics or read the native
        // engine. Each is the original call on its own, so a unit test can answer it and see it made.

        /// <summary>Clears the companion control state of a companion. The original call into <c>CompanionControl</c>.</summary>
        protected virtual void ClearCompanionControl(uint companion)
        {
            _companionControl.Clear(companion);
        }

        /// <summary>Sets up the companion control state of a companion. The original call into <c>CompanionControl</c>.</summary>
        protected virtual void InitializeCompanionControl(uint companion)
        {
            _companionControl.Initialize(companion);
        }

        /// <summary>Runs a companion's combat round. The original call into <c>CompanionControl</c>.</summary>
        protected virtual void ProcessCompanionCombatRound(uint companion)
        {
            _companionControl.ProcessCombatRound(companion);
        }

        /// <summary>Runs a companion's heartbeat. The original call into <c>CompanionControl</c>.</summary>
        protected virtual void ProcessCompanionHeartbeat(uint companion)
        {
            _companionControl.ProcessHeartbeat(companion);
        }

        /// <summary>Lets the companion control answer a conversation. The original call into <c>CompanionControl</c>.</summary>
        protected virtual bool HandleCompanionConversation(uint companion)
        {
            return _companionControl.HandleConversation(companion);
        }

        /// <summary>Registers a threat a companion defends against. The original call into <c>CompanionControl</c>.</summary>
        protected virtual void RegisterDefensiveThreat(uint companion, uint threat)
        {
            _companionControl.RegisterDefensiveThreat(companion, threat);
        }

        /// <summary>
        /// Records on a master whether its beast was killed by a player or a player's pet. The original call into
        /// <c>ReviveRules</c>, which keeps the death times in static state.
        /// </summary>
        protected virtual void RecordBeastDeath(uint master, string beastId, uint killer)
        {
            Feature.AbilityDefinition.ReviveRules.RecordBeastDeath(master, beastId, killer);
        }

        /// <summary>Restores the stamina and focus of the beasts. The original call into <c>Stat</c>.</summary>
        protected virtual void RestoreBeastStats()
        {
            _creatureStat.RestoreBeastStats();
        }

        /// <summary>Loads the stats of the NPC that runs the current script. The original call into <c>Stat</c>.</summary>
        protected virtual void LoadNPCStats()
        {
            _creatureStat.LoadNPCStats();
        }

        /// <summary>Applies the movement rate of a creature. The original call into <c>Stat</c>.</summary>
        protected virtual void ApplyCreatureMovementRate(uint creature)
        {
            _creatureStat.ApplyCreatureMovementRate(creature);
        }

        /// <summary>Puts the rest status effect on a beast. The original call into <c>StatusEffect</c>.</summary>
        protected virtual void ApplyRestStatusEffect(uint beast)
        {
            _statusEffect.ApplyStatusEffect(beast, beast, typeof(RestStatusEffect), 0f);
        }

        /// <summary>
        /// Opens or closes a window for a player. The original call into <c>Gui</c>, whose window table is static state.
        /// </summary>
        protected virtual void TogglePlayerWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject)
        {
            _gui.TogglePlayerWindow(player, windowType, payload, tetherObject);
        }
    }
}
