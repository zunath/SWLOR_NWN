using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Calling a beast: the one-companion check and the spawn of a player's beast next to them, with its stats, feats and
    /// event scripts. The catalog, the IDs and the stat application stay on <see cref="BeastMasteryService"/>. The
    /// feat grant is a seam, so a unit test can answer it.
    /// </summary>
    public class BeastSpawnService : IBeastSpawnService
    {
        private const string BeastResref = "pc_beast";

        private readonly IDBService _db;
        private readonly IAIService _ai;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IPerkService _perk;
        private readonly IBeastMasteryService _beastMastery;

        public BeastSpawnService(
            IDBService db,
            IAIService ai,
            ICombatRecorderService combatRecorder,
            IPerkService perk,
            IBeastMasteryService beastMastery)
        {
            _db = db;
            _ai = ai;
            _combatRecorder = combatRecorder;
            _perk = perk;
            _beastMastery = beastMastery;
        }

        /// <summary>
        /// Determines whether a player is free to spawn a new companion (beast or droid).
        /// The game engine only permits one henchman-type associate at a time, so beast and
        /// droid activation must share this check to prevent an orphaned, master-less companion.
        /// </summary>
        /// <param name="player">The player attempting to spawn a companion.</param>
        /// <returns>An error message if a companion is already active, otherwise an empty string.</returns>
        public string GetCompanionSlotValidationError(uint player)
        {
            if (GetIsObjectValid(GetAssociate(AssociateType.Henchman, player)))
            {
                return "Only one companion may be active at a time.";
            }

            return string.Empty;
        }

        public void SpawnBeast(uint player, string beastId, int percentHeal)
        {
            var companionSlotError = GetCompanionSlotValidationError(player);
            if (!string.IsNullOrEmpty(companionSlotError))
            {
                SendMessageToPC(player, companionSlotError);
                return;
            }

            var dbBeast = _db.Get<Beast>(beastId);

            if (dbBeast == null)
            {
                SendMessageToPC(player, "Unable to locate beast in DB. Notify an admin.");
                return;
            }

            var beastDetail = _beastMastery.GetBeastDetail(dbBeast.Type);
            var beast = CreateObject(ObjectType.Creature, BeastResref, GetLocation(player));

            SetName(beast, dbBeast.Name);
            _beastMastery.SetBeastId(beast, beastId);
            _beastMastery.SetBeastType(beast, dbBeast.Type);

            SetCreatureAppearanceType(beast, beastDetail.Appearance);
            SetObjectVisualTransform(beast, ObjectVisualTransform.Scale, beastDetail.AppearanceScale);
            SetPortraitId(beast, dbBeast.PortraitId > -1 ? dbBeast.PortraitId : beastDetail.PortraitId);
            SetSoundset(beast, dbBeast.SoundSetId > -1 ? dbBeast.SoundSetId : beastDetail.SoundSetId);

            _beastMastery.ApplyStats(beast);
            _ai.SetAIProfile(beast, AIProfileType.BeastCompanion);

            AddHenchman(player, beast);


            // Perks
            foreach (var (perk, level) in dbBeast.Perks)
            {
                SyncGrantedFeats(beast, perk, level, false);
            }

            // Scripts
            SetEventScript(beast, EventScript.Creature_OnBlockedByDoor, ScriptName.OnBeastBlocked);
            SetEventScript(beast, EventScript.Creature_OnEndCombatRound, ScriptName.OnBeastRoundEnd);
            SetEventScript(beast, EventScript.Creature_OnDialogue, ScriptName.OnBeastConversation);
            SetEventScript(beast, EventScript.Creature_OnDamaged, ScriptName.OnBeastDamaged);
            SetEventScript(beast, EventScript.Creature_OnDeath, ScriptName.OnBeastDeath);
            SetEventScript(beast, EventScript.Creature_OnDisturbed, ScriptName.OnBeastDisturbed);
            SetEventScript(beast, EventScript.Creature_OnHeartbeat, ScriptName.OnBeastHeartbeat);
            SetEventScript(beast, EventScript.Creature_OnMeleeAttacked, ScriptName.OnBeastAttacked);
            SetEventScript(beast, EventScript.Creature_OnRested, ScriptName.OnBeastRest);
            SetEventScript(beast, EventScript.Creature_OnSpawnIn, ScriptName.OnBeastSpawn);
            SetEventScript(beast, EventScript.Creature_OnSpellCastAt, ScriptName.OnBeastSpellCast);
            SetEventScript(beast, EventScript.Creature_OnUserDefined, ScriptName.OnBeastUserDefined);

            // Ensure the spawn script gets called as it normally gets skipped
            // because it doesn't exist at the time of the beast being created.
            ExecuteScript(GetEventScript(beast, EventScript.Creature_OnSpawnIn), beast);

            // A normal Call Beast spawn requests 100% health. Restore it immediately so the
            // companion does not appear near death while waiting for the delayed HP correction.
            if (percentHeal >= 100)
            {
                SetCurrentHitPoints(beast, GetMaxHitPoints(beast));
            }

            AssignCommand(GetModule(), () =>
            {
                DelayCommand(4f, () =>
                {
                    SetCurrentHitPoints(beast, 1);

                    if (percentHeal > 0)
                    {
                        var healHP = (int)(GetMaxHitPoints(beast) * (percentHeal * 0.01f));
                        using var recorderAttribution = _combatRecorder.BeginSetupAttribution(beast);
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(healHP), beast);
                    }

                });
            });
        }

        /// <summary>
        /// Grants the feats a perk level gives to a creature. <c>Perk.SyncGrantedFeats</c> reads the perk catalog and
        /// the native feat lists, which a unit test does not load, so a test overrides this.
        /// </summary>
        protected virtual void SyncGrantedFeats(uint creature, PerkType perkType, int perkLevel, bool addByLevel)
        {
            _perk.SyncGrantedFeats(creature, perkType, perkLevel, addByLevel);
        }
    }
}
