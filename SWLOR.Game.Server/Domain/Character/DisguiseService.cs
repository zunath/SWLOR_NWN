using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Disguise identities: the logic that used to be static in <c>Service.Disguise</c>, moved here unchanged. The
    /// login handler and the constants are here.
    /// </summary>
    /// <remarks>
    /// <c>PlayerName</c>, <c>PlayerDescriptor</c> and <c>PlayerIntroduction</c> reach this service through the
    /// <c>Disguise</c> facade (their own <c>protected virtual</c> wrappers), so this service can take them by interface.
    /// </remarks>
    public class DisguiseService : IDisguiseService
    {
        public const string IdentityKeyPrefix = "disguise:";
        public const int WipeCreditCost = 100000;
        public const int WipeRoleplayXPCost = 25000;
        public const int ActivationDelayMinutes = 30;
        public const int MinimumActivationDelayMinutes = 5;

        public const int MaxPrivateNameLength = 32;
        public const int MaxBiographyLength = 5000;
        private const string BlankBiographyPlaceholder = "No description is available.";
        private const int DisguiseQueryPageSize = 50;

        private readonly IDBService _db;
        private readonly ICacheService _cache;
        private readonly IGuiService _gui;
        private readonly IDMPresenceService _dmPresence;
        private readonly IPlayerNameService _playerName;
        private readonly IPlayerDescriptorService _playerDescriptor;
        private readonly IStatService _stat;

        public DisguiseService(
            IDBService db,
            ICacheService cache,
            IGuiService gui,
            IDMPresenceService dmPresence,
            IPlayerNameService playerName,
            IPlayerDescriptorService playerDescriptor,
            IStatService stat)
        {
            _db = db;
            _cache = cache;
            _gui = gui;
            _dmPresence = dmPresence;
            _playerName = playerName;
            _playerDescriptor = playerDescriptor;
            _stat = stat;
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "The disguise's portrait, soundset and description stay in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyActiveDisguiseOnEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var activeDisguise = GetActiveDisguise(player);
            if (activeDisguise == null)
                return;

            ApplyAppearance(player, activeDisguise);

            WriteStructuredLog(LogGroup.PlayerName,
                "Disguise reapplied on login: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor}",
                GetObjectUUID(player), GetName(player), activeDisguise.Id, activeDisguise.PrivateName, activeDisguise.Descriptor);
        }

        public string BuildIdentityKey(string disguiseId)
        {
            return $"{IdentityKeyPrefix}{disguiseId}";
        }

        public bool IsDisguiseIdentityKey(string identityKey)
        {
            return !string.IsNullOrWhiteSpace(identityKey) &&
                   identityKey.StartsWith(IdentityKeyPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public string GetIdentityKey(uint player)
        {
            var activeDisguise = GetActiveDisguise(player);
            return activeDisguise == null
                ? GetObjectUUID(player)
                : BuildIdentityKey(activeDisguise.Id);
        }

        public string GetDisplayDescriptor(uint player)
        {
            var activeDisguise = GetActiveDisguise(player);
            return activeDisguise == null
                ? _playerDescriptor.GetUnknownDisplayName(player)
                : string.IsNullOrWhiteSpace(activeDisguise.Descriptor)
                    ? _playerDescriptor.GetUnknownDisplayName(player)
                    : activeDisguise.Descriptor;
        }

        public bool ShouldScrambleAccountName(uint player)
        {
            var activeDisguise = GetActiveDisguise(player);
            if (activeDisguise != null)
                return activeDisguise.ScrambleAccountId;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            return dbPlayer?.Settings?.ScrambleAccountName ?? true;
        }

        public PlayerDisguise GetActiveDisguise(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                return null;

            var playerId = GetObjectUUID(player);
            if (string.IsNullOrWhiteSpace(playerId))
                return null;

            var dbPlayer = _db.Get<Player>(playerId);
            return GetActiveDisguise(dbPlayer);
        }

        public PlayerDisguise GetActiveDisguise(Player dbPlayer)
        {
            if (dbPlayer == null ||
                string.IsNullOrWhiteSpace(dbPlayer.ActiveDisguiseId))
            {
                return null;
            }

            var disguise = _db.Get<PlayerDisguise>(dbPlayer.ActiveDisguiseId);
            if (disguise == null ||
                disguise.IsRetired ||
                disguise.PlayerId != dbPlayer.Id)
            {
                return null;
            }

            return disguise;
        }

        public List<PlayerDisguise> GetDisguises(string playerId, bool retired)
        {
            var results = new List<PlayerDisguise>();
            var offset = 0;

            while (true)
            {
                var page = _db.Search(new DBQuery<PlayerDisguise>()
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

        public int GetDisguiseSlotLimit(uint player, Player dbPlayer)
        {
            var baseLimit = dbPlayer?.DisguiseSlotLimit > 0
                ? dbPlayer.DisguiseSlotLimit
                : Player.DefaultDisguiseSlotLimit;

            return CalculateDisguiseSlotLimit(
                baseLimit,
                GetStatAdjustment(player, StatType.AdditionalDisguiseSlots));
        }

        public int CalculateDisguiseSlotLimit(int baseLimit, int additionalSlots)
        {
            return Math.Max(Player.DefaultDisguiseSlotLimit, baseLimit + additionalSlots);
        }

        /// <summary>
        /// The delay this player must wait between disguise activations, after perk reductions.
        /// </summary>
        public TimeSpan GetActivationDelay(uint player)
        {
            return CalculateActivationDelay(
                GetStatAdjustment(player, StatType.DisguiseSwapCooldownReductionPercent));
        }

        /// <summary>
        /// Applies a cooldown reduction percent to the base activation delay. The result never drops
        /// below <see cref="MinimumActivationDelayMinutes"/> so that stacked reduction sources cannot
        /// remove the delay entirely and let a player cycle identities faster than they can be observed.
        /// </summary>
        public TimeSpan CalculateActivationDelay(int reductionPercent)
        {
            var clamped = Math.Clamp(reductionPercent, 0, 100);
            var minutes = ActivationDelayMinutes * (100 - clamped) / 100f;

            return TimeSpan.FromMinutes(Math.Max(MinimumActivationDelayMinutes, minutes));
        }

        public int CountUsedSlots(string playerId)
        {
            return (int)_db.SearchCount(new DBQuery<PlayerDisguise>()
                .AddFieldSearch(nameof(PlayerDisguise.PlayerId), playerId, false));
        }

        public PlayerDisguise CreateDisguise(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var usedSlots = CountUsedSlots(playerId);
            var slotLimit = GetDisguiseSlotLimit(player, dbPlayer);

            if (usedSlots >= slotLimit)
            {
                WriteStructuredLog(LogGroup.PlayerName,
                    "Disguise creation rejected: PlayerId={PlayerId} PlayerName={PlayerName} UsedSlots={UsedSlots} SlotLimit={SlotLimit}",
                    playerId, GetName(player), usedSlots, slotLimit);
                return null;
            }

            var portraitInternalId = ResolvePortraitInternalId(player);
            var disguise = new PlayerDisguise
            {
                PlayerId = playerId,
                PrivateName = $"Disguise #{usedSlots + 1}",
                Descriptor = _playerDescriptor.GetUnknownDisplayName(player),
                Biography = string.Empty,
                PortraitInternalId = portraitInternalId,
                SoundSetId = GetSoundset(player),
                ScrambleAccountId = true
            };

            _db.Set(disguise);

            WriteStructuredLog(
                LogGroup.PlayerName,
                "Disguise created: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor} BiographyLength={BiographyLength} PortraitInternalId={PortraitInternalId} SoundSetId={SoundSetId} ScrambleAccountId={ScrambleAccountId} Biography={Biography}",
                playerId,
                GetName(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor,
                disguise.Biography.Length,
                disguise.PortraitInternalId,
                disguise.SoundSetId,
                disguise.ScrambleAccountId,
                JsonConvert.SerializeObject(disguise.Biography));

            return disguise;
        }

        public SaveDisguiseResult SaveDisguise(
            uint player,
            string disguiseId,
            string privateName,
            string descriptor,
            string biography,
            int portraitInternalId,
            int soundSetId,
            bool scrambleAccountId)
        {
            var playerId = GetObjectUUID(player);
            SaveDisguiseResult Reject(string reason)
            {
                WriteStructuredLog(LogGroup.PlayerName,
                    "Disguise customization rejected: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} Reason={Reason} PrivateName={PrivateName} Descriptor={Descriptor} Biography={Biography} PortraitInternalId={PortraitInternalId} SoundSetId={SoundSetId} ScrambleAccountId={ScrambleAccountId}",
                    playerId, GetName(player), disguiseId, reason, JsonConvert.SerializeObject(privateName ?? string.Empty),
                    JsonConvert.SerializeObject(descriptor ?? string.Empty), JsonConvert.SerializeObject(biography ?? string.Empty),
                    portraitInternalId, soundSetId, scrambleAccountId);
                return SaveDisguiseResult.Failure(reason);
            }

            var disguise = _db.Get<PlayerDisguise>(disguiseId);
            if (disguise == null || disguise.PlayerId != playerId)
                return Reject("Unable to locate that disguise.");

            if (disguise.IsRetired)
                return Reject("Retired disguises cannot be edited.");

            var privateNameError = ValidatePrivateName(privateName);
            if (!string.IsNullOrWhiteSpace(privateNameError))
                return Reject(privateNameError);

            var descriptorError = _playerName.ValidateKnownNameInput(descriptor);
            if (!string.IsNullOrWhiteSpace(descriptorError))
                return Reject(descriptorError);

            biography ??= string.Empty;
            if (biography.Length > MaxBiographyLength)
                return Reject($"Disguise biographies may be no longer than {MaxBiographyLength} characters.");

            portraitInternalId = Math.Clamp(portraitInternalId, 1, GetMaxPortraitCount());
            soundSetId = ResolveSoundSetId(soundSetId);

            var previousPrivateName = disguise.PrivateName;
            var previousDescriptor = disguise.Descriptor;
            var previousBiography = disguise.Biography ?? string.Empty;
            var previousPortraitInternalId = disguise.PortraitInternalId;
            var previousSoundSetId = disguise.SoundSetId;
            var previousScrambleAccountId = disguise.ScrambleAccountId;

            disguise.PrivateName = _playerName.SanitizeKnownName(privateName);
            disguise.Descriptor = _playerName.SanitizeKnownName(descriptor);
            disguise.Biography = biography;
            disguise.PortraitInternalId = portraitInternalId;
            disguise.SoundSetId = soundSetId;
            disguise.ScrambleAccountId = scrambleAccountId;
            _db.Set(disguise);

            WriteStructuredLog(
                LogGroup.PlayerName,
                "Disguise saved: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PreviousPrivateName={PreviousPrivateName} PrivateName={PrivateName} PreviousDescriptor={PreviousDescriptor} Descriptor={Descriptor} PreviousBiographyLength={PreviousBiographyLength} BiographyLength={BiographyLength} PreviousPortraitInternalId={PreviousPortraitInternalId} PortraitInternalId={PortraitInternalId} PreviousSoundSetId={PreviousSoundSetId} SoundSetId={SoundSetId} PreviousScrambleAccountId={PreviousScrambleAccountId} ScrambleAccountId={ScrambleAccountId} PreviousBiography={PreviousBiography} Biography={Biography}",
                playerId,
                GetName(player),
                disguise.Id,
                previousPrivateName,
                disguise.PrivateName,
                previousDescriptor,
                disguise.Descriptor,
                previousBiography.Length,
                disguise.Biography.Length,
                previousPortraitInternalId,
                disguise.PortraitInternalId,
                previousSoundSetId,
                disguise.SoundSetId,
                previousScrambleAccountId,
                disguise.ScrambleAccountId,
                JsonConvert.SerializeObject(previousBiography),
                JsonConvert.SerializeObject(disguise.Biography));

            var isActive = IsActiveDisguise(player, disguise.Id);
            if (!string.Equals(previousDescriptor, disguise.Descriptor, StringComparison.Ordinal))
            {
                WriteStructuredLog(LogGroup.PublicDescriptors,
                    "Disguise public descriptor changed: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} IdentityKey={IdentityKey} PreviousDescriptor={PreviousDescriptor} Descriptor={Descriptor} IsActive={IsActive}",
                    playerId, GetName(player), disguise.Id, BuildIdentityKey(disguise.Id),
                    previousDescriptor, disguise.Descriptor, isActive);
            }

            if (isActive)
            {
                ApplyAppearance(player, disguise);
                RefreshDisguiseDisplay(player);
            }

            return SaveDisguiseResult.Success();
        }

        public ActivateDisguiseResult Activate(uint player, string disguiseId)
        {
            var playerId = GetObjectUUID(player);
            ActivateDisguiseResult Reject(string reason)
            {
                WriteStructuredLog(LogGroup.PlayerName,
                    "Disguise activation rejected: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} Reason={Reason}",
                    playerId, GetName(player), disguiseId, reason);
                return ActivateDisguiseResult.Failure(reason);
            }

            var dbPlayer = _db.Get<Player>(playerId);
            var disguise = _db.Get<PlayerDisguise>(disguiseId);

            if (dbPlayer == null ||
                disguise == null ||
                disguise.PlayerId != playerId ||
                disguise.IsRetired)
            {
                return Reject("Unable to activate that disguise.");
            }

            // Re-activating the disguise that is already active is a harmless no-op. Return early so
            // the activation delay does not falsely block it and DateLastActivated is not re-stamped.
            if (dbPlayer.ActiveDisguiseId == disguise.Id)
            {
                ApplyAppearance(player, disguise);
                RefreshDisguiseDisplay(player);
                WriteStructuredLog(LogGroup.PlayerName,
                    "Active disguise reapplied: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor}",
                    playerId, GetName(player), disguise.Id, disguise.PrivateName, disguise.Descriptor);
                return ActivateDisguiseResult.Success();
            }

            var delayError = ValidateActivationDelay(player, playerId);
            if (!string.IsNullOrWhiteSpace(delayError))
                return Reject(delayError);

            var previousDisguiseId = dbPlayer.ActiveDisguiseId;

            // Only snapshot the undisguised baseline when transitioning from no active disguise.
            // Keying off the stored id (rather than a resolved disguise) avoids capturing an
            // already-applied disguise appearance as the baseline if the stored id is ever stale.
            if (string.IsNullOrWhiteSpace(dbPlayer.ActiveDisguiseId))
            {
                dbPlayer.UndisguisedPortraitId = GetPortraitId(player);
                dbPlayer.UndisguisedPortraitResref = GetPortraitResRef(player);
                dbPlayer.UndisguisedSoundSetId = GetSoundset(player);
                dbPlayer.UndisguisedDescription = GetDescription(player) ?? string.Empty;
                dbPlayer.HasUndisguisedDescriptionSnapshot = true;
            }

            dbPlayer.ActiveDisguiseId = disguise.Id;
            disguise.DateLastActivated = UtcNow();
            _db.Set(disguise);
            _db.Set(dbPlayer);

            WriteStructuredLog(
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

        public bool Deactivate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null ||
                string.IsNullOrWhiteSpace(dbPlayer.ActiveDisguiseId))
            {
                WriteStructuredLog(LogGroup.PlayerName,
                    "Disguise deactivation rejected: PlayerId={PlayerId} PlayerName={PlayerName} Reason={Reason}",
                    playerId, GetName(player), "No active disguise.");
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

            if (dbPlayer.HasUndisguisedDescriptionSnapshot)
                SetDescription(player, dbPlayer.UndisguisedDescription ?? string.Empty);
            else
                SetDescription(player, GetDescription(player, true) ?? string.Empty);

            dbPlayer.ActiveDisguiseId = string.Empty;
            dbPlayer.UndisguisedPortraitId = -1;
            dbPlayer.UndisguisedPortraitResref = string.Empty;
            dbPlayer.UndisguisedSoundSetId = -1;
            dbPlayer.UndisguisedDescription = string.Empty;
            dbPlayer.HasUndisguisedDescriptionSnapshot = false;
            _db.Set(dbPlayer);

            WriteStructuredLog(
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

        public bool Retire(uint player, string disguiseId)
        {
            var playerId = GetObjectUUID(player);
            var disguise = _db.Get<PlayerDisguise>(disguiseId);
            if (disguise == null ||
                disguise.PlayerId != playerId ||
                disguise.IsRetired)
            {
                WriteStructuredLog(LogGroup.PlayerName,
                    "Disguise retirement rejected: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} Reason={Reason}",
                    playerId, GetName(player), disguiseId, "Disguise not found, not owned, or already retired.");
                return false;
            }

            if (IsActiveDisguise(player, disguiseId))
                Deactivate(player);

            disguise.IsRetired = true;
            disguise.DateRetired = UtcNow();
            _db.Set(disguise);

            WriteStructuredLog(
                LogGroup.PlayerName,
                "Disguise retired: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor}",
                playerId,
                GetName(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor);

            return true;
        }

        public bool Unretire(uint player, string disguiseId)
        {
            var playerId = GetObjectUUID(player);
            var disguise = _db.Get<PlayerDisguise>(disguiseId);
            if (disguise == null ||
                disguise.PlayerId != playerId ||
                !disguise.IsRetired)
            {
                WriteStructuredLog(LogGroup.PlayerName,
                    "Disguise restoration rejected: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} Reason={Reason}",
                    playerId, GetName(player), disguiseId, "Disguise not found, not owned, or not retired.");
                return false;
            }

            disguise.IsRetired = false;
            disguise.DateRetired = null;
            _db.Set(disguise);

            WriteStructuredLog(
                LogGroup.PlayerName,
                "Disguise restored: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor}",
                playerId,
                GetName(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor);

            return true;
        }

        public DeleteRetiredDisguiseResult DeleteRetiredDisguise(
            uint player,
            string disguiseId,
            DisguisePaymentMethod paymentMethod)
        {
            var playerId = GetObjectUUID(player);
            DeleteRetiredDisguiseResult Reject(DeleteRetiredDisguiseResult reason)
            {
                WriteStructuredLog(LogGroup.PlayerName,
                    "Disguise deletion rejected: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PaymentMethod={PaymentMethod} Reason={Reason}",
                    playerId, GetName(player), disguiseId, paymentMethod.ToString(), reason.ToString());
                return reason;
            }

            var dbPlayer = _db.Get<Player>(playerId);
            var disguise = _db.Get<PlayerDisguise>(disguiseId);

            if (disguise == null)
                return Reject(DeleteRetiredDisguiseResult.NotFound);

            if (disguise.PlayerId != playerId)
                return Reject(DeleteRetiredDisguiseResult.NotOwner);

            if (!disguise.IsRetired)
                return Reject(DeleteRetiredDisguiseResult.NotRetired);

            if (dbPlayer != null && dbPlayer.ActiveDisguiseId == disguise.Id)
                return Reject(DeleteRetiredDisguiseResult.IsActive);

            var amount = 0;
            switch (paymentMethod)
            {
                case DisguisePaymentMethod.Credits:
                    amount = WipeCreditCost;
                    if (GetGold(player) < amount)
                        return Reject(DeleteRetiredDisguiseResult.InsufficientCredits);

                    TakeGoldFromCreature(amount, player, true);
                    break;
                case DisguisePaymentMethod.RoleplayXP:
                    amount = WipeRoleplayXPCost;
                    if (dbPlayer == null || dbPlayer.UnallocatedXP < amount)
                        return Reject(DeleteRetiredDisguiseResult.InsufficientRoleplayXP);

                    dbPlayer.UnallocatedXP -= amount;
                    _db.Set(dbPlayer);
                    _gui.PublishRefreshEvent(player, new RPXPRefreshEvent());
                    break;
                default:
                    return Reject(DeleteRetiredDisguiseResult.InvalidPaymentMethod);
            }

            var identityKey = BuildIdentityKey(disguise.Id);
            var removedKnownNames = _playerName.DeleteKnownNameReferences(identityKey);

            WriteStructuredLog(
                LogGroup.PlayerName,
                "Retired disguise deleted: PlayerId={PlayerId} PlayerName={PlayerName} PublicCDKey={PublicCDKey} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor} PaymentMethod={PaymentMethod} PaymentAmount={PaymentAmount} RemovedKnownNameReferences={RemovedKnownNameReferences} Biography={Biography} PortraitInternalId={PortraitInternalId} SoundSetId={SoundSetId} ScrambleAccountId={ScrambleAccountId}",
                playerId,
                GetName(player),
                GetPCPublicCDKey(player),
                disguise.Id,
                disguise.PrivateName,
                disguise.Descriptor,
                paymentMethod.ToString(),
                amount,
                removedKnownNames,
                JsonConvert.SerializeObject(disguise.Biography ?? string.Empty),
                disguise.PortraitInternalId,
                disguise.SoundSetId,
                disguise.ScrambleAccountId);

            _db.Delete<PlayerDisguise>(disguise.Id);
            return DeleteRetiredDisguiseResult.Success;
        }

        public int ResetActivationCooldowns(uint player, uint actor = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(actor))
                actor = player;

            actor = _dmPresence.GetDM(actor);

            var playerId = GetObjectUUID(player);
            var resetCount = 0;
            var offset = 0;

            while (true)
            {
                var page = _db.Search(new DBQuery<PlayerDisguise>()
                        .AddFieldSearch(nameof(PlayerDisguise.PlayerId), playerId, false)
                        .AddPaging(DisguiseQueryPageSize, offset))
                    .ToList();

                foreach (var disguise in page)
                {
                    if (!disguise.DateLastActivated.HasValue)
                        continue;

                    var previousDateLastActivated = disguise.DateLastActivated.Value;
                    disguise.DateLastActivated = null;
                    _db.Set(disguise);

                    WriteStructuredLog(LogGroup.PlayerName,
                        "Disguise activation cooldown reset: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId} PrivateName={PrivateName} Descriptor={Descriptor} PreviousDateLastActivated={PreviousDateLastActivated} ActorPlayerId={ActorPlayerId} ActorName={ActorName}",
                        playerId, GetName(player), disguise.Id, disguise.PrivateName, disguise.Descriptor, previousDateLastActivated,
                        GetObjectUUID(actor), GetName(actor));
                    resetCount++;
                }

                if (page.Count < DisguiseQueryPageSize)
                    break;

                offset += DisguiseQueryPageSize;
            }

            return resetCount;
        }

        public string GetPortraitResref(PlayerDisguise disguise)
        {
            if (disguise == null)
                return string.Empty;

            try
            {
                return _cache.GetPortraitResrefByInternalId(disguise.PortraitInternalId) + "l";
            }
            catch (KeyNotFoundException)
            {
                return string.Empty;
            }
        }

        public string GetSoundSetName(int soundSetId)
        {
            var soundSets = _cache.GetSoundSets();
            return soundSets.TryGetValue(soundSetId, out var label)
                ? label
                : "Unknown";
        }

        private string ValidatePrivateName(string privateName)
        {
            if (string.IsNullOrWhiteSpace(privateName))
                return "Please enter a private disguise name.";

            if (privateName.Length > MaxPrivateNameLength)
                return $"Private disguise names may be no longer than {MaxPrivateNameLength} characters.";

            if (privateName != UtilPlugin.StripColors(privateName))
                return "Private disguise names may not contain color codes.";

            return string.Empty;
        }

        private bool IsActiveDisguise(uint player, string disguiseId)
        {
            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null &&
                   !string.IsNullOrWhiteSpace(disguiseId) &&
                   dbPlayer.ActiveDisguiseId == disguiseId;
        }

        private string ValidateActivationDelay(uint player, string playerId)
        {
            var latestActivation = GetLatestActivationDate(playerId);
            if (!latestActivation.HasValue)
                return string.Empty;

            var delay = GetActivationDelay(player);
            var remaining = delay - (UtcNow() - latestActivation.Value);
            if (remaining <= TimeSpan.Zero)
                return string.Empty;

            var delayMinutes = (int)Math.Round(delay.TotalMinutes);

            return $"There is a {delayMinutes}-minute delay between disguise activations. You must wait {FormatRemainingDelay(remaining)} before activating another disguise. Deactivation is available immediately.";
        }

        private DateTime? GetLatestActivationDate(string playerId)
        {
            DateTime? latestActivation = null;
            var offset = 0;

            while (true)
            {
                var page = _db.Search(new DBQuery<PlayerDisguise>()
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

        private string FormatRemainingDelay(TimeSpan remaining)
        {
            var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
            return minutes <= 1
                ? "less than 1 minute"
                : $"{minutes} minutes";
        }

        private int ResolvePortraitInternalId(uint player)
        {
            var resref = GetPortraitResRef(player);
            var internalId = _cache.GetPortraitInternalIdByResref(resref);
            if (internalId > 0)
                return internalId;

            var portraitId = GetPortraitId(player);
            try
            {
                internalId = _cache.GetPortraitInternalId(portraitId);
                return internalId > 0
                    ? internalId
                    : 1;
            }
            catch (KeyNotFoundException)
            {
                return 1;
            }
        }

        private void ApplyAppearance(uint player, PlayerDisguise disguise)
        {
            if (disguise.PortraitInternalId > 0)
            {
                try
                {
                    SetPortraitId(player, _cache.GetPortraitByInternalId(disguise.PortraitInternalId));
                }
                catch (KeyNotFoundException)
                {
                    // Ignore invalid legacy portrait ids; the disguise descriptor still applies.
                }
            }

            var soundSetId = ResolveSoundSetId(disguise.SoundSetId);
            if (soundSetId >= 0)
                SetSoundset(player, soundSetId);

            EnsureUndisguisedDescriptionSnapshot(player);
            var biography = string.IsNullOrWhiteSpace(disguise.Biography)
                ? BlankBiographyPlaceholder
                : disguise.Biography;
            SetDescription(player, biography);
        }

        private void EnsureUndisguisedDescriptionSnapshot(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null ||
                string.IsNullOrWhiteSpace(dbPlayer.ActiveDisguiseId) ||
                dbPlayer.HasUndisguisedDescriptionSnapshot)
            {
                return;
            }

            dbPlayer.UndisguisedDescription = GetDescription(player) ?? string.Empty;
            dbPlayer.HasUndisguisedDescriptionSnapshot = true;
            _db.Set(dbPlayer);
        }

        private void RefreshDisguiseDisplay(uint player)
        {
            _playerName.RefreshNameOverridesForPlayer(player);
            _gui.PublishRefreshEvent(player, new DisguiseChangedRefreshEvent());
        }

        private int GetMaxPortraitCount()
        {
            return Math.Max(1, _cache.PortraitCount);
        }

        private int ResolveSoundSetId(int soundSetId)
        {
            return _cache.GetSoundSets().ContainsKey(soundSetId)
                ? soundSetId
                : -1;
        }

        /// <summary>The current time: <c>DateTime.UtcNow</c>.</summary>
        protected virtual DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        /// <summary>The stat adjustment from gear and effects: <c>Stat.GetStatAdjustment</c>.</summary>
        protected virtual int GetStatAdjustment(uint creature, StatType stat)
        {
            return _stat.GetStatAdjustment(creature, stat);
        }

        /// <summary>The structured log line: <c>Log.WriteStructured</c> (<c>Log</c> stays static in Phase 2).</summary>
        protected virtual void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues)
        {
            Log.WriteStructured(group, messageTemplate, propertyValues);
        }
    }
}
