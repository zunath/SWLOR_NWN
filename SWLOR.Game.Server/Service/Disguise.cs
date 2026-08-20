using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DisguiseService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class Disguise
    {
        public const string IdentityKeyPrefix = "disguise:";
        public const int WipeCreditCost = 100000;
        public const int WipeRoleplayXPCost = 25000;
        public const int ActivationDelayMinutes = 30;
        public const int MinimumActivationDelayMinutes = 5;

        public const int MaxPrivateNameLength = 32;
        private const int DisguiseQueryPageSize = 50;

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyActiveDisguiseOnEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var activeDisguise = GetActiveDisguise(player);
            if (activeDisguise == null)
                return;

            ApplyAppearance(player, activeDisguise);
        }

        public static string BuildIdentityKey(string disguiseId)
        {
            return $"{IdentityKeyPrefix}{disguiseId}";
        }

        public static bool IsDisguiseIdentityKey(string identityKey)
        {
            return !string.IsNullOrWhiteSpace(identityKey) &&
                   identityKey.StartsWith(IdentityKeyPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetIdentityKey(uint player)
        {
            var activeDisguise = GetActiveDisguise(player);
            return activeDisguise == null
                ? GetObjectUUID(player)
                : BuildIdentityKey(activeDisguise.Id);
        }

        public static string GetDisplayDescriptor(uint player)
        {
            var activeDisguise = GetActiveDisguise(player);
            return activeDisguise == null
                ? PlayerDescriptor.GetUnknownDisplayName(player)
                : string.IsNullOrWhiteSpace(activeDisguise.Descriptor)
                    ? PlayerDescriptor.GetUnknownDisplayName(player)
                    : activeDisguise.Descriptor;
        }

        public static bool ShouldScrambleAccountName(uint player)
        {
            var activeDisguise = GetActiveDisguise(player);
            if (activeDisguise != null)
                return activeDisguise.ScrambleAccountId;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            return dbPlayer?.Settings?.ScrambleAccountName ?? true;
        }

        public static PlayerDisguise GetActiveDisguise(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                return null;

            var playerId = GetObjectUUID(player);
            if (string.IsNullOrWhiteSpace(playerId))
                return null;

            var dbPlayer = DB.Get<Player>(playerId);
            return GetActiveDisguise(dbPlayer);
        }

        public static PlayerDisguise GetActiveDisguise(Player dbPlayer)
        {
            if (dbPlayer == null ||
                string.IsNullOrWhiteSpace(dbPlayer.ActiveDisguiseId))
            {
                return null;
            }

            var disguise = DB.Get<PlayerDisguise>(dbPlayer.ActiveDisguiseId);
            if (disguise == null ||
                disguise.IsRetired ||
                disguise.PlayerId != dbPlayer.Id)
            {
                return null;
            }

            return disguise;
        }

        public static List<PlayerDisguise> GetDisguises(string playerId, bool retired)
        {
            var results = new List<PlayerDisguise>();
            var offset = 0;

            while (true)
            {
                var page = DB.Search(new DBQuery<PlayerDisguise>()
                        .AddFieldSearch(nameof(PlayerDisguise.PlayerId), playerId, false)
                        .AddFieldSearch(nameof(PlayerDisguise.IsRetired), retired)
                        .AddPaging(DisguiseQueryPageSize, offset))
                    .ToList();

                results.AddRange(page);

                if (page.Count < DisguiseQueryPageSize)
                    break;

                offset += DisguiseQueryPageSize;
            }

            return results
                .OrderBy(disguise => disguise.PrivateName)
                .ToList();
        }

        public static int GetDisguiseSlotLimit(uint player, Player dbPlayer)
        {
            var baseLimit = dbPlayer?.DisguiseSlotLimit > 0
                ? dbPlayer.DisguiseSlotLimit
                : Player.DefaultDisguiseSlotLimit;

            return CalculateDisguiseSlotLimit(
                baseLimit,
                Stat.GetStatAdjustment(player, StatType.AdditionalDisguiseSlots));
        }

        public static int CalculateDisguiseSlotLimit(int baseLimit, int additionalSlots)
        {
            return Math.Max(Player.DefaultDisguiseSlotLimit, baseLimit + additionalSlots);
        }

        /// <summary>
        /// The delay this player must wait between disguise activations, after perk reductions.
        /// </summary>
        public static TimeSpan GetActivationDelay(uint player)
        {
            return CalculateActivationDelay(
                Stat.GetStatAdjustment(player, StatType.DisguiseSwapCooldownReductionPercent));
        }

        /// <summary>
        /// Applies a cooldown reduction percent to the base activation delay. The result never drops
        /// below <see cref="MinimumActivationDelayMinutes"/> so that stacked reduction sources cannot
        /// remove the delay entirely and let a player cycle identities faster than they can be observed.
        /// </summary>
        public static TimeSpan CalculateActivationDelay(int reductionPercent)
        {
            var clamped = Math.Clamp(reductionPercent, 0, 100);
            var minutes = ActivationDelayMinutes * (100 - clamped) / 100f;

            return TimeSpan.FromMinutes(Math.Max(MinimumActivationDelayMinutes, minutes));
        }

        public static int CountUsedSlots(string playerId)
        {
            return (int)DB.SearchCount(new DBQuery<PlayerDisguise>()
                .AddFieldSearch(nameof(PlayerDisguise.PlayerId), playerId, false));
        }

        public static PlayerDisguise CreateDisguise(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var usedSlots = CountUsedSlots(playerId);
            var slotLimit = GetDisguiseSlotLimit(player, dbPlayer);

            if (usedSlots >= slotLimit)
                return null;

            var portraitInternalId = ResolvePortraitInternalId(player);
            var disguise = new PlayerDisguise
            {
                PlayerId = playerId,
                PrivateName = $"Disguise #{usedSlots + 1}",
                Descriptor = PlayerDescriptor.GetUnknownDisplayName(player),
                PortraitInternalId = portraitInternalId,
                SoundSetId = GetSoundset(player),
                ScrambleAccountId = true
            };

            DB.Set(disguise);

            Log.WriteStructured(
                LogGroup.PlayerName,
                "Disguise created: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor} PortraitInternalId={PortraitInternalId} SoundSetId={SoundSetId} ScrambleAccountId={ScrambleAccountId}",
                playerId,
                GetName(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor,
                disguise.PortraitInternalId,
                disguise.SoundSetId,
                disguise.ScrambleAccountId);

            return disguise;
        }

        public static SaveDisguiseResult SaveDisguise(
            uint player,
            string disguiseId,
            string privateName,
            string descriptor,
            int portraitInternalId,
            int soundSetId,
            bool scrambleAccountId)
        {
            var playerId = GetObjectUUID(player);
            var disguise = DB.Get<PlayerDisguise>(disguiseId);
            if (disguise == null || disguise.PlayerId != playerId)
                return SaveDisguiseResult.Failure("Unable to locate that disguise.");

            if (disguise.IsRetired)
                return SaveDisguiseResult.Failure("Retired disguises cannot be edited.");

            var privateNameError = ValidatePrivateName(privateName);
            if (!string.IsNullOrWhiteSpace(privateNameError))
                return SaveDisguiseResult.Failure(privateNameError);

            var descriptorError = PlayerName.ValidateKnownNameInput(descriptor);
            if (!string.IsNullOrWhiteSpace(descriptorError))
                return SaveDisguiseResult.Failure(descriptorError);

            portraitInternalId = Math.Clamp(portraitInternalId, 1, GetMaxPortraitCount());
            soundSetId = ResolveSoundSetId(soundSetId);

            var previousPrivateName = disguise.PrivateName;
            var previousDescriptor = disguise.Descriptor;
            var previousPortraitInternalId = disguise.PortraitInternalId;
            var previousSoundSetId = disguise.SoundSetId;
            var previousScrambleAccountId = disguise.ScrambleAccountId;

            disguise.PrivateName = PlayerName.SanitizeKnownName(privateName);
            disguise.Descriptor = PlayerName.SanitizeKnownName(descriptor);
            disguise.PortraitInternalId = portraitInternalId;
            disguise.SoundSetId = soundSetId;
            disguise.ScrambleAccountId = scrambleAccountId;
            DB.Set(disguise);

            Log.WriteStructured(
                LogGroup.PlayerName,
                "Disguise saved: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PreviousPrivateName={PreviousPrivateName} PrivateName={PrivateName} PreviousDescriptor={PreviousDescriptor} Descriptor={Descriptor} PreviousPortraitInternalId={PreviousPortraitInternalId} PortraitInternalId={PortraitInternalId} PreviousSoundSetId={PreviousSoundSetId} SoundSetId={SoundSetId} PreviousScrambleAccountId={PreviousScrambleAccountId} ScrambleAccountId={ScrambleAccountId}",
                playerId,
                GetName(player),
                disguise.Id,
                previousPrivateName,
                disguise.PrivateName,
                previousDescriptor,
                disguise.Descriptor,
                previousPortraitInternalId,
                disguise.PortraitInternalId,
                previousSoundSetId,
                disguise.SoundSetId,
                previousScrambleAccountId,
                disguise.ScrambleAccountId);

            if (IsActiveDisguise(player, disguise.Id))
            {
                ApplyAppearance(player, disguise);
                RefreshDisguiseDisplay(player);
            }

            return SaveDisguiseResult.Success();
        }

        public static ActivateDisguiseResult Activate(uint player, string disguiseId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var disguise = DB.Get<PlayerDisguise>(disguiseId);

            if (dbPlayer == null ||
                disguise == null ||
                disguise.PlayerId != playerId ||
                disguise.IsRetired)
            {
                return ActivateDisguiseResult.Failure("Unable to activate that disguise.");
            }

            // Re-activating the disguise that is already active is a harmless no-op. Return early so
            // the activation delay does not falsely block it and DateLastActivated is not re-stamped.
            if (dbPlayer.ActiveDisguiseId == disguise.Id)
            {
                ApplyAppearance(player, disguise);
                RefreshDisguiseDisplay(player);
                return ActivateDisguiseResult.Success();
            }

            var delayError = ValidateActivationDelay(player, playerId);
            if (!string.IsNullOrWhiteSpace(delayError))
                return ActivateDisguiseResult.Failure(delayError);

            var previousDisguiseId = dbPlayer.ActiveDisguiseId;

            // Only snapshot the undisguised baseline when transitioning from no active disguise.
            // Keying off the stored id (rather than a resolved disguise) avoids capturing an
            // already-applied disguise appearance as the baseline if the stored id is ever stale.
            if (string.IsNullOrWhiteSpace(dbPlayer.ActiveDisguiseId))
            {
                dbPlayer.UndisguisedPortraitId = GetPortraitId(player);
                dbPlayer.UndisguisedPortraitResref = GetPortraitResRef(player);
                dbPlayer.UndisguisedSoundSetId = GetSoundset(player);
            }

            dbPlayer.ActiveDisguiseId = disguise.Id;
            disguise.DateLastActivated = DateTime.UtcNow;
            DB.Set(disguise);
            DB.Set(dbPlayer);

            Log.WriteStructured(
                LogGroup.PlayerName,
                "Disguise activated: PlayerId={PlayerId} PlayerName={PlayerName} PreviousDisguiseId={PreviousDisguiseId} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor} PortraitInternalId={PortraitInternalId} SoundSetId={SoundSetId} ScrambleAccountId={ScrambleAccountId}",
                playerId,
                GetName(player),
                previousDisguiseId,
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor,
                disguise.PortraitInternalId,
                disguise.SoundSetId,
                disguise.ScrambleAccountId);

            ApplyAppearance(player, disguise);
            RefreshDisguiseDisplay(player);
            return ActivateDisguiseResult.Success();
        }

        public static bool Deactivate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null ||
                string.IsNullOrWhiteSpace(dbPlayer.ActiveDisguiseId))
            {
                return false;
            }

            var activeDisguiseId = dbPlayer.ActiveDisguiseId;
            var activeDisguise = GetActiveDisguise(dbPlayer);

            if (!string.IsNullOrWhiteSpace(dbPlayer.UndisguisedPortraitResref))
                SetPortraitResRef(player, dbPlayer.UndisguisedPortraitResref);
            else if (dbPlayer.UndisguisedPortraitId >= 0)
                SetPortraitId(player, dbPlayer.UndisguisedPortraitId);

            if (dbPlayer.UndisguisedSoundSetId >= 0)
                SetSoundset(player, dbPlayer.UndisguisedSoundSetId);

            dbPlayer.ActiveDisguiseId = string.Empty;
            dbPlayer.UndisguisedPortraitId = -1;
            dbPlayer.UndisguisedPortraitResref = string.Empty;
            dbPlayer.UndisguisedSoundSetId = -1;
            DB.Set(dbPlayer);

            Log.WriteStructured(
                LogGroup.PlayerName,
                "Disguise deactivated: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor} PortraitInternalId={PortraitInternalId} SoundSetId={SoundSetId} ScrambleAccountId={ScrambleAccountId}",
                playerId,
                GetName(player),
                activeDisguiseId,
                activeDisguise?.PrivateName ?? string.Empty,
                activeDisguise?.Descriptor ?? string.Empty,
                activeDisguise?.PortraitInternalId ?? -1,
                activeDisguise?.SoundSetId ?? -1,
                activeDisguise?.ScrambleAccountId ?? false);

            RefreshDisguiseDisplay(player);
            return true;
        }

        public static bool Retire(uint player, string disguiseId)
        {
            var playerId = GetObjectUUID(player);
            var disguise = DB.Get<PlayerDisguise>(disguiseId);
            if (disguise == null ||
                disguise.PlayerId != playerId ||
                disguise.IsRetired)
            {
                return false;
            }

            if (IsActiveDisguise(player, disguiseId))
                Deactivate(player);

            disguise.IsRetired = true;
            disguise.DateRetired = DateTime.UtcNow;
            DB.Set(disguise);

            Log.WriteStructured(
                LogGroup.PlayerName,
                "Disguise retired: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor}",
                playerId,
                GetName(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor);

            return true;
        }

        public static bool Unretire(uint player, string disguiseId)
        {
            var playerId = GetObjectUUID(player);
            var disguise = DB.Get<PlayerDisguise>(disguiseId);
            if (disguise == null ||
                disguise.PlayerId != playerId ||
                !disguise.IsRetired)
            {
                return false;
            }

            disguise.IsRetired = false;
            disguise.DateRetired = null;
            DB.Set(disguise);

            Log.WriteStructured(
                LogGroup.PlayerName,
                "Disguise restored: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor}",
                playerId,
                GetName(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor);

            return true;
        }

        public static DeleteRetiredDisguiseResult DeleteRetiredDisguise(
            uint player,
            string disguiseId,
            DisguisePaymentMethod paymentMethod)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var disguise = DB.Get<PlayerDisguise>(disguiseId);

            if (disguise == null)
                return DeleteRetiredDisguiseResult.NotFound;

            if (disguise.PlayerId != playerId)
                return DeleteRetiredDisguiseResult.NotOwner;

            if (!disguise.IsRetired)
                return DeleteRetiredDisguiseResult.NotRetired;

            if (dbPlayer != null && dbPlayer.ActiveDisguiseId == disguise.Id)
                return DeleteRetiredDisguiseResult.IsActive;

            var amount = 0;
            switch (paymentMethod)
            {
                case DisguisePaymentMethod.Credits:
                    amount = WipeCreditCost;
                    if (GetGold(player) < amount)
                        return DeleteRetiredDisguiseResult.InsufficientCredits;

                    TakeGoldFromCreature(amount, player, true);
                    break;
                case DisguisePaymentMethod.RoleplayXP:
                    amount = WipeRoleplayXPCost;
                    if (dbPlayer == null || dbPlayer.UnallocatedXP < amount)
                        return DeleteRetiredDisguiseResult.InsufficientRoleplayXP;

                    dbPlayer.UnallocatedXP -= amount;
                    DB.Set(dbPlayer);
                    Gui.PublishRefreshEvent(player, new RPXPRefreshEvent());
                    break;
                default:
                    return DeleteRetiredDisguiseResult.InvalidPaymentMethod;
            }

            var identityKey = BuildIdentityKey(disguise.Id);
            var removedKnownNames = PlayerName.DeleteKnownNameReferences(identityKey);

            Log.WriteStructured(
                LogGroup.PlayerName,
                "Retired disguise deleted: PlayerId={PlayerId} PlayerName={PlayerName} PublicCDKey={PublicCDKey} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor} PaymentMethod={PaymentMethod} PaymentAmount={PaymentAmount} RemovedKnownNameReferences={RemovedKnownNameReferences}",
                playerId,
                GetName(player),
                GetPCPublicCDKey(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor,
                paymentMethod.ToString(),
                amount,
                removedKnownNames);

            DB.Delete<PlayerDisguise>(disguise.Id);
            return DeleteRetiredDisguiseResult.Success;
        }

        public static int ResetActivationCooldowns(uint player)
        {
            var playerId = GetObjectUUID(player);
            var resetCount = 0;
            var offset = 0;

            while (true)
            {
                var page = DB.Search(new DBQuery<PlayerDisguise>()
                        .AddFieldSearch(nameof(PlayerDisguise.PlayerId), playerId, false)
                        .AddPaging(DisguiseQueryPageSize, offset))
                    .ToList();

                foreach (var disguise in page)
                {
                    if (!disguise.DateLastActivated.HasValue)
                        continue;

                    disguise.DateLastActivated = null;
                    DB.Set(disguise);
                    resetCount++;
                }

                if (page.Count < DisguiseQueryPageSize)
                    break;

                offset += DisguiseQueryPageSize;
            }

            return resetCount;
        }

        public static string GetPortraitResref(PlayerDisguise disguise)
        {
            if (disguise == null)
                return string.Empty;

            try
            {
                return Cache.GetPortraitResrefByInternalId(disguise.PortraitInternalId) + "l";
            }
            catch (KeyNotFoundException)
            {
                return string.Empty;
            }
        }

        public static string GetSoundSetName(int soundSetId)
        {
            var soundSets = Cache.GetSoundSets();
            return soundSets.TryGetValue(soundSetId, out var label)
                ? label
                : "Unknown";
        }

        private static string ValidatePrivateName(string privateName)
        {
            if (string.IsNullOrWhiteSpace(privateName))
                return "Please enter a private disguise name.";

            if (privateName.Length > MaxPrivateNameLength)
                return $"Private disguise names may be no longer than {MaxPrivateNameLength} characters.";

            if (privateName != UtilPlugin.StripColors(privateName))
                return "Private disguise names may not contain color codes.";

            return string.Empty;
        }

        private static bool IsActiveDisguise(uint player, string disguiseId)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null &&
                   !string.IsNullOrWhiteSpace(disguiseId) &&
                   dbPlayer.ActiveDisguiseId == disguiseId;
        }

        private static string ValidateActivationDelay(uint player, string playerId)
        {
            var latestActivation = GetLatestActivationDate(playerId);
            if (!latestActivation.HasValue)
                return string.Empty;

            var delay = GetActivationDelay(player);
            var remaining = delay - (DateTime.UtcNow - latestActivation.Value);
            if (remaining <= TimeSpan.Zero)
                return string.Empty;

            var delayMinutes = (int)Math.Round(delay.TotalMinutes);

            return $"There is a {delayMinutes}-minute delay between disguise activations. You must wait {FormatRemainingDelay(remaining)} before activating another disguise. Deactivation is available immediately.";
        }

        private static DateTime? GetLatestActivationDate(string playerId)
        {
            DateTime? latestActivation = null;
            var offset = 0;

            while (true)
            {
                var page = DB.Search(new DBQuery<PlayerDisguise>()
                        .AddFieldSearch(nameof(PlayerDisguise.PlayerId), playerId, false)
                        .AddPaging(DisguiseQueryPageSize, offset))
                    .ToList();

                foreach (var disguise in page)
                {
                    if (!disguise.DateLastActivated.HasValue)
                        continue;

                    if (!latestActivation.HasValue ||
                        disguise.DateLastActivated.Value > latestActivation.Value)
                    {
                        latestActivation = disguise.DateLastActivated.Value;
                    }
                }

                if (page.Count < DisguiseQueryPageSize)
                    break;

                offset += DisguiseQueryPageSize;
            }

            return latestActivation;
        }

        private static string FormatRemainingDelay(TimeSpan remaining)
        {
            var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
            return minutes <= 1
                ? "less than 1 minute"
                : $"{minutes} minutes";
        }

        private static int ResolvePortraitInternalId(uint player)
        {
            var resref = GetPortraitResRef(player);
            var internalId = Cache.GetPortraitInternalIdByResref(resref);
            if (internalId > 0)
                return internalId;

            var portraitId = GetPortraitId(player);
            try
            {
                internalId = Cache.GetPortraitInternalId(portraitId);
                return internalId > 0
                    ? internalId
                    : 1;
            }
            catch (KeyNotFoundException)
            {
                return 1;
            }
        }

        private static void ApplyAppearance(uint player, PlayerDisguise disguise)
        {
            if (disguise.PortraitInternalId > 0)
            {
                try
                {
                    SetPortraitId(player, Cache.GetPortraitByInternalId(disguise.PortraitInternalId));
                }
                catch (KeyNotFoundException)
                {
                    // Ignore invalid legacy portrait ids; the disguise descriptor still applies.
                }
            }

            var soundSetId = ResolveSoundSetId(disguise.SoundSetId);
            if (soundSetId >= 0)
                SetSoundset(player, soundSetId);
        }

        private static void RefreshDisguiseDisplay(uint player)
        {
            PlayerName.RefreshNameOverridesForPlayer(player);
            Gui.PublishRefreshEvent(player, new DisguiseChangedRefreshEvent());
        }

        private static int GetMaxPortraitCount()
        {
            return Math.Max(1, Cache.PortraitCount);
        }

        private static int ResolveSoundSetId(int soundSetId)
        {
            return Cache.GetSoundSets().ContainsKey(soundSetId)
                ? soundSetId
                : -1;
        }
    }
}
