using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.Engine;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Wrecks of destroyed NPC ships (plan section 12, "Salvage"). A destroyed preset NPC ship leaves a wreck placeable
    /// for <see cref="ShipSalvageRules.WreckSeconds"/>; a Salvager works it, and the killing party has it to itself for
    /// <see cref="ShipSalvageRules.ClaimSeconds"/>.
    /// </summary>
    public static partial class Space
    {
        /// <summary>The wreck blueprint of a Small, Medium or Large ship.</summary>
        public const string WreckResref = "spc_wreck";

        /// <summary>The wreck blueprint of a Capital ship.</summary>
        public const string CapitalWreckResref = "spc_wreck_cap";

        /// <summary>The tag every wreck shares, registered as a space object so a click targets it.</summary>
        public const string WreckTag = "spc_wreck";

        /// <summary>Set on NPC ships that must leave no wreck, such as a carrier's launched fighters.</summary>
        public const string NoWreckVariable = "SPACE_NO_WRECK";

        private static readonly Dictionary<uint, ShipWreck> _wrecks = new();

        /// <summary>When a preset NPC ship is destroyed, leave its wreck where it died.</summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void SpawnWreckOnShipDeath()
        {
            var creature = OBJECT_SELF;
            if (GetIsPC(creature) || !_shipNPCs.TryGetValue(creature, out var status))
                return;

            if (GetLocalBool(creature, NoWreckVariable) || !_npcPresets.TryGetValue(GetTag(creature), out var preset))
                return;

            ForgetVanishedWrecks();

            var size = status.Size;
            var difficulty = ShipSalvageRules.ParseDifficulty(preset.Difficulty);
            var resref = size == ShipSize.Capital ? CapitalWreckResref : WreckResref;
            var wreck = CreateObject(ObjectType.Placeable, resref, GetLocation(creature));
            if (!GetIsObjectValid(wreck))
                return;

            SetName(wreck, $"{GetName(creature)} Wreck");
            _wrecks[wreck] = new ShipWreck
            {
                Tier = ShipSalvageRules.WreckTier(GetTag(creature), preset.Level),
                Difficulty = difficulty,
                Size = size,
                DestroyedAt = DateTime.UtcNow,
                Claimants = GetKillingParty(creature),
                RemainingCycles = ShipSalvageRules.WreckCycles(size, difficulty)
            };

            // The wreck schedules its own removal: a delay on the dying ship would be lost when the ship is destroyed.
            AssignCommand(wreck, () => DelayCommand(ShipSalvageRules.WreckSeconds, () => DestroyObject(wreck)));
        }

        /// <summary>
        /// Places a salvageable wreck that no ship's death left, such as a derelict site's (plan section 24). The
        /// claimants have it to themselves for the claim time from now, and it is removed after
        /// <paramref name="seconds"/>.
        /// </summary>
        public static uint CreateWreck(Location location, string name, int tier, ShipSize size, WreckDifficulty difficulty,
            HashSet<string> claimants, float seconds)
        {
            ForgetVanishedWrecks();

            var resref = size == ShipSize.Capital ? CapitalWreckResref : WreckResref;
            var wreck = CreateObject(ObjectType.Placeable, resref, location);
            if (!GetIsObjectValid(wreck))
                return OBJECT_INVALID;

            SetName(wreck, name);
            _wrecks[wreck] = new ShipWreck
            {
                Tier = Math.Clamp(tier, 1, 6),
                Difficulty = difficulty,
                Size = size,
                DestroyedAt = DateTime.UtcNow,
                Claimants = claimants ?? new HashSet<string>(),
                RemainingCycles = ShipSalvageRules.WreckCycles(size, difficulty)
            };

            AssignCommand(wreck, () => DelayCommand(seconds, () => RemoveWreck(wreck)));
            return wreck;
        }

        /// <summary>The wreck a placeable is, or null when it is not a wreck (or has been stripped).</summary>
        public static ShipWreck GetWreck(uint placeable)
        {
            return _wrecks.TryGetValue(placeable, out var wreck) && GetIsObjectValid(placeable) ? wreck : null;
        }

        /// <summary>Removes a stripped wreck.</summary>
        public static void RemoveWreck(uint placeable)
        {
            _wrecks.Remove(placeable);
            if (GetIsObjectValid(placeable))
                DestroyObject(placeable);
        }

        /// <summary>
        /// The player IDs of the killer's party. The killer is the ship that dealt the killing blow (space deaths are
        /// applied by the attacker), otherwise the ship with the most threat. Empty when no player ship killed it.
        /// </summary>
        private static HashSet<string> GetKillingParty(uint creature)
        {
            var killer = GetLastKiller();
            if (!GetIsPC(killer))
                killer = Enmity.GetHighestEnmityTarget(creature);

            var party = new HashSet<string>();
            if (!GetIsObjectValid(killer) || !GetIsPC(killer))
                return party;

            for (var member = GetFirstFactionMember(killer); GetIsObjectValid(member); member = GetNextFactionMember(killer))
            {
                if (!GetIsDM(member))
                    party.Add(GetObjectUUID(member));
            }

            party.Add(GetObjectUUID(killer));
            return party;
        }

        /// <summary>Drops the records of wrecks that timed out (DestroyObject removed the placeable).</summary>
        private static void ForgetVanishedWrecks()
        {
            foreach (var placeable in _wrecks.Keys.Where(x => !GetIsObjectValid(x)).ToList())
            {
                _wrecks.Remove(placeable);
            }
        }
    }
}
