using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.CityBountyService;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// City bounties (Player City Revamp, "Conflict and PvP"). The mayor or an officer posts a bounty on an identity that
    /// shows the PvP flag and whose player accepted the notice (<see cref="BountyConsent"/>). The reward moves from the
    /// ordinary treasury into the city's escrow, the target is told privately, and the Gazette names only the identity a
    /// stranger sees. Whoever the game credits with the target's defeat or subdual collects it (<see cref="Death"/>), never
    /// in a duel. A bounty that runs out after 7 days or is cancelled returns its reward to the treasury. The rules live in
    /// <see cref="BountyRules"/> and <see cref="PvpCombatWindowRules"/>; this class reads and saves the records and acts on
    /// the game. A payment is journaled on the record, so a restart finishes it once.
    /// </summary>
    public static class CityBounties
    {
        private const string PaymentIdPrefix = "bounty-claim:";

        /// <summary>The client needs a moment after entering before chat messages show.</summary>
        private const float LoginDelaySeconds = 5f;

        private static IDisposable _expirySchedule;

        // Lookups

        public static CityBounty Get(string bountyId)
        {
            return string.IsNullOrWhiteSpace(bountyId) ? null : DB.Get<CityBounty>(bountyId);
        }

        /// <summary>The city's bounties that are open to claim, soonest to run out first.</summary>
        public static List<CityBounty> GetOpenForCity(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return new List<CityBounty>();

            return Search(new DBQuery<CityBounty>().AddFieldSearch(nameof(CityBounty.CityId), cityId, false))
                .Where(x => x.State == BountyState.Active)
                .OrderBy(x => x.ExpiresUtc)
                .ToList();
        }

        /// <summary>The bounty on an identity, in any city and any state, or null.</summary>
        public static CityBounty GetOn(BountyIdentity identity)
        {
            return GetForPlayer(identity.PlayerId)
                .FirstOrDefault(x => x.TargetDisguiseId == identity.DisguiseId);
        }

        private static List<CityBounty> GetForPlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return new List<CityBounty>();

            return Search(new DBQuery<CityBounty>().AddFieldSearch(nameof(CityBounty.TargetPlayerId), playerId, false));
        }

        /// <summary>The identity a player shows now: their active disguise, or their own character.</summary>
        public static BountyIdentity GetShownIdentity(Player dbPlayer)
        {
            return new BountyIdentity(dbPlayer.Id, Disguise.GetActiveDisguise(dbPlayer)?.Id);
        }

        private static DateTime? GetLastClaimedUtc(Player dbPlayer, PlayerDisguise disguise)
        {
            return disguise != null ? disguise.LastBountyClaimedUtc : dbPlayer.LastBountyClaimedUtc;
        }

        /// <summary>
        /// The online identities that show the PvP flag, as the viewer sees them in the Player List, each with the reason a
        /// bounty cannot be posted on it, if any.
        /// </summary>
        public static List<BountyTarget> GetTargets(uint viewer)
        {
            var targets = new List<BountyTarget>();
            var now = DateTime.UtcNow;
            var entries = PlayerList.BuildEntries(viewer, PlayerList.BuildSubjects());
            foreach (var entry in entries)
            {
                if (entry.Relation.TargetIsSelf || entry.Relation.TargetIsDM || !entry.Flags.Contains(PlayerListFlagType.PvP))
                    continue;

                var dbPlayer = DB.Get<Player>(GetObjectUUID(entry.Player));
                if (dbPlayer == null)
                    continue;

                var disguise = Disguise.GetActiveDisguise(dbPlayer);
                var identity = new BountyIdentity(dbPlayer.Id, disguise?.Id);
                var problem = BountyRules.GetTargetProblem(new BountyPostRequest
                {
                    NowUtc = now,
                    TargetFlagged = true,
                    TargetAccepted = BountyRules.HasAccepted(BountyConsent.GetAcceptedUtc(dbPlayer, disguise)),
                    TargetHasActiveBounty = GetOn(identity) != null,
                    TargetLastClaimedUtc = GetLastClaimedUtc(dbPlayer, disguise)
                });

                var name = string.IsNullOrEmpty(entry.Name) ? entry.Descriptor : entry.Name;
                targets.Add(new BountyTarget(
                    entry.Player, dbPlayer.Id, identity.DisguiseId, string.IsNullOrEmpty(name) ? "Unknown" : name, problem));
            }

            return targets;
        }

        // Posting and cancelling

        /// <summary>Whether the player holds the power to post and cancel bounties for the city.</summary>
        public static bool CanPost(WorldProperty city, string playerId)
        {
            return city != null && city.PropertyType == PropertyType.City &&
                   CityPowers.Can(city, playerId, CityPower.PostRequests);
        }

        /// <summary>
        /// Posts a bounty on the identity a player shows. The record is saved first and the reward then moves into escrow,
        /// so a restart in between finds an unfunded bounty and drops it.
        /// </summary>
        /// <param name="poster">The mayor or officer.</param>
        /// <param name="cityId">The city that pays.</param>
        /// <param name="target">The player picked from the Player List.</param>
        /// <param name="expectedDisguiseId">The disguise the poster saw the target showing, or empty for their own character.</param>
        /// <param name="reward">The credits to pay whoever collects it.</param>
        /// <returns>The reason it was refused, or an empty string.</returns>
        public static string Post(uint poster, string cityId, uint target, string expectedDisguiseId, long reward)
        {
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            if (city == null || city.PropertyType != PropertyType.City)
                return "This city no longer exists.";

            if (!PvpCombat.IsPlayer(poster))
                return "Only players can post a bounty.";

            if (!PvpCombat.IsPlayer(target))
                return "That player is no longer online.";

            var posterId = GetObjectUUID(poster);
            var dbTarget = DB.Get<Player>(GetObjectUUID(target));
            if (dbTarget == null)
                return "That player is no longer online.";

            var disguise = Disguise.GetActiveDisguise(dbTarget);
            var identity = new BountyIdentity(dbTarget.Id, disguise?.Id);
            if (identity.DisguiseId != (expectedDisguiseId ?? string.Empty))
                return "That player is no longer showing the identity you picked.";

            var now = DateTime.UtcNow;
            var error = BountyRules.CheckPost(new BountyPostRequest
            {
                NowUtc = now,
                PosterCanPost = CanPost(city, posterId),
                TargetIsPoster = dbTarget.Id == posterId,
                Reward = reward,
                OrdinaryTreasury = CityTreasuryRules.GetSpendable(city, restrictedFirst: false),
                TargetFlagged = PlayerList.GetFlags(target).Contains(PlayerListFlagType.PvP),
                TargetAccepted = BountyRules.HasAccepted(BountyConsent.GetAcceptedUtc(dbTarget, disguise)),
                TargetHasActiveBounty = GetOn(identity) != null,
                TargetLastClaimedUtc = GetLastClaimedUtc(dbTarget, disguise)
            });
            if (!string.IsNullOrEmpty(error))
                return error;

            var bounty = new CityBounty
            {
                CityId = city.Id,
                TargetPlayerId = identity.PlayerId,
                TargetDisguiseId = identity.DisguiseId,
                State = BountyState.Active,
                TargetLabel = Disguise.GetDisplayDescriptor(target),
                Reward = reward,
                PostedByPlayerId = posterId,
                PostedUtc = now,
                ExpiresUtc = BountyRules.GetExpiry(now)
            };
            DB.Set(bounty);

            if (!CityTreasury.PostEscrow(city, bounty.Id, reward, restrictedFirst: false).Succeeded)
            {
                DB.Delete<CityBounty>(bounty.Id);
                return "The reward could not be held. Try again.";
            }

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(poster)} posted bounty '{bounty.Id}' for city '{city.CustomName}' ({city.Id}) on player '{bounty.TargetPlayerId}' identity '{bounty.TargetDisguiseId}': {reward} credits, runs until {bounty.ExpiresUtc:u}.");
            Announce(city, BountyText.BuildPostedText(bounty.TargetLabel, reward, bounty.ExpiresUtc), posterId);
            TellTarget(bounty, city, target);
            return string.Empty;
        }

        /// <summary>An official withdraws a bounty. The reward returns from escrow to the treasury.</summary>
        /// <returns>The reason it was refused, or an empty string.</returns>
        public static string Cancel(uint player, string bountyId)
        {
            var bounty = Get(bountyId);
            if (bounty == null || bounty.State != BountyState.Active)
                return "That bounty is gone.";

            var city = DB.Get<WorldProperty>(bounty.CityId);
            var playerId = GetObjectUUID(player);
            if (!PvpCombat.IsPlayer(player) || !CanPost(city, playerId))
                return "Only the mayor or an officer can withdraw a bounty.";

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} withdrew bounty '{bounty.Id}' of city '{city.CustomName}' ({city.Id}).");
            Refund(bounty, city, BountyText.BuildCancelledText(bounty.TargetLabel, bounty.Reward), playerId);
            return string.Empty;
        }

        /// <summary>
        /// Returns a bounty's reward from escrow to the balance it came from, deletes the bounty, and tells the Gazette. The
        /// reward is released before the record goes, so a restart in between finds a bounty with nothing held and drops it.
        /// </summary>
        private static void Refund(CityBounty bounty, WorldProperty city, string gazetteText, string officialPlayerId = null)
        {
            if (city != null)
                CityTreasury.RefundEscrow(city, bounty.Id);

            DB.Delete<CityBounty>(bounty.Id);
            if (city != null)
                Announce(city, gazetteText, officialPlayerId);
        }

        // Claims

        /// <summary>
        /// A player fell. When a player is credited with it and a bounty is on the identity the fallen player shows, the
        /// bounty is judged (<see cref="BountyRules.EvaluateClaim"/>) and paid. It runs for a defeat and for a subdual, and
        /// never lets a fault here disturb the death.
        /// </summary>
        /// <param name="target">The player who fell.</param>
        /// <param name="hostile">Whoever the game credits with the fall: a player, a player's pet or droid, or the applier of a damaging effect.</param>
        /// <param name="inDuel">Whether a duel, or the protection after a third party voided one, turned the fall into a subdual.</param>
        public static void ClaimOnFall(uint target, uint hostile, bool inDuel)
        {
            try
            {
                if (!PvpCombat.IsPlayer(target))
                    return;

                // Most falls are an enemy's work: no player is credited, so nothing is read from the database.
                if (!BountyRules.TryResolveClaimant(hostile, PvpCombat.IsPlayer, GetMaster, out var claimant))
                    return;

                var dbTarget = DB.Get<Player>(GetObjectUUID(target));
                if (dbTarget == null)
                    return;

                var disguise = Disguise.GetActiveDisguise(dbTarget);
                var shown = new BountyIdentity(dbTarget.Id, disguise?.Id);
                var bounty = GetOn(shown);
                if (bounty == null)
                    return;

                var context = new BountyClaimContext
                {
                    NowUtc = DateTime.UtcNow,
                    Bounty = new BountyIdentity(bounty.TargetPlayerId, bounty.TargetDisguiseId),
                    State = bounty.State,
                    ExpiresUtc = bounty.ExpiresUtc,
                    Shown = shown,
                    ShownFlagOn = PlayerList.GetFlags(target).Contains(PlayerListFlagType.PvP),
                    WindowOpen = PvpCombatWindow.IsOpen(dbTarget.Id),
                    InDuel = inDuel,
                    HasClaimant = true,
                    ClaimantPlayerId = GetObjectUUID(claimant),
                    TargetCdKey = GetPCPublicCDKey(target),
                    ClaimantCdKey = GetPCPublicCDKey(claimant),
                    SharesParty = SharesParty(target, claimant),
                    LastClaimedUtc = GetLastClaimedUtc(dbTarget, disguise)
                };

                var refusal = BountyRules.EvaluateClaim(context);
                if (refusal != BountyClaimRefusal.None)
                {
                    Log.Write(LogGroup.Property, $"Bounty '{bounty.Id}' was not paid for the fall of '{dbTarget.Id}': {refusal}.");
                    return;
                }

                Claim(bounty, claimant);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to judge bounties for a fallen player: {ex}");
            }
        }

        private static bool SharesParty(uint target, uint claimant)
        {
            var targetParty = PlayerList.GetParty(target);
            return targetParty.PlayerCount >= 2 && targetParty.Leader == PlayerList.GetParty(claimant).Leader;
        }

        /// <summary>
        /// Decides the claim and pays it. The bounty is saved as Paying with the claimant first, and works as the journal: the
        /// claimant's credits are written under an id that never changes, the escrow is released, and the bounty is deleted.
        /// A restart at any point repeats the steps that did not finish and none that did.
        /// </summary>
        private static void Claim(CityBounty bounty, uint claimant)
        {
            bounty.State = BountyState.Paying;
            bounty.ClaimantPlayerId = GetObjectUUID(claimant);
            bounty.ClaimantLabel = Disguise.GetDisplayDescriptor(claimant);
            bounty.ClaimedUtc = DateTime.UtcNow;
            DB.Set(bounty);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(claimant)} collected bounty '{bounty.Id}' of city '{bounty.CityId}' on player '{bounty.TargetPlayerId}' identity '{bounty.TargetDisguiseId}': {bounty.Reward} credits.");
            FinishPayment(bounty, announce: true);
        }

        /// <summary>
        /// Finishes a claim that was decided: marks the identity claimed for the week, owes the claimant the credits, pays the
        /// escrow out, deletes the bounty, and tells the Gazette. Every step can repeat after a restart.
        /// </summary>
        private static void FinishPayment(CityBounty bounty, bool announce)
        {
            MarkClaimed(bounty);

            PendingCredits.Add(
                PaymentIdPrefix + bounty.Id,
                bounty.ClaimantPlayerId,
                bounty.Reward,
                BountyText.BuildClaimerNotice(bounty.TargetLabel, bounty.Reward));

            var city = DB.Get<WorldProperty>(bounty.CityId);
            if (city != null && city.Escrow.ContainsKey(bounty.Id))
                CityTreasury.PayOutEscrow(city, bounty.Id);

            DB.Delete<CityBounty>(bounty.Id);

            if (announce && city != null)
                Announce(city, BountyText.BuildClaimedText(bounty.TargetLabel, bounty.ClaimantLabel, bounty.Reward));

            PendingCredits.DeliverIfOnline(bounty.ClaimantPlayerId);
        }

        // Starts the identity's week, so a second bounty on it cannot be claimed until the interval has passed.
        private static void MarkClaimed(CityBounty bounty)
        {
            var claimedUtc = bounty.ClaimedUtc ?? DateTime.UtcNow;
            if (string.IsNullOrEmpty(bounty.TargetDisguiseId))
            {
                var dbPlayer = DB.Get<Player>(bounty.TargetPlayerId);
                if (dbPlayer == null || dbPlayer.LastBountyClaimedUtc >= claimedUtc)
                    return;

                dbPlayer.LastBountyClaimedUtc = claimedUtc;
                DB.Set(dbPlayer);
                return;
            }

            var disguise = DB.Get<PlayerDisguise>(bounty.TargetDisguiseId);
            if (disguise == null || disguise.LastBountyClaimedUtc >= claimedUtc)
                return;

            disguise.LastBountyClaimedUtc = claimedUtc;
            DB.Set(disguise);
        }

        // Expiry, restart and city removal

        /// <summary>
        /// Settles what a restart left behind: a claim that was decided is paid, and a bounty whose reward was never held, was
        /// already returned, or whose city is gone is dropped. Then bounties start running out on a timer.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Recover()
        {
            foreach (var bounty in Search(new DBQuery<CityBounty>()))
            {
                try
                {
                    if (bounty.State == BountyState.Paying)
                    {
                        FinishPayment(bounty, announce: true);
                        continue;
                    }

                    var city = DB.Get<WorldProperty>(bounty.CityId);
                    if (city == null || !city.Escrow.ContainsKey(bounty.Id))
                        DB.Delete<CityBounty>(bounty.Id);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Failed to recover bounty '{bounty.Id}': {ex}");
                }
            }

            _expirySchedule ??= Scheduler.ScheduleRepeating(ExpireDue, TimeSpan.FromMinutes(1));
        }

        /// <summary>Returns the reward of every bounty that ran out unclaimed.</summary>
        private static void ExpireDue()
        {
            var now = DateTime.UtcNow;
            var due = Search(new DBQuery<CityBounty>()
                    .AddFieldSearch(nameof(CityBounty.State), (int)BountyState.Active))
                .Where(x => BountyRules.IsExpired(x.ExpiresUtc, now))
                .ToList();

            foreach (var bounty in due)
            {
                try
                {
                    var city = DB.Get<WorldProperty>(bounty.CityId);
                    Log.Write(LogGroup.Property, $"Bounty '{bounty.Id}' of city '{bounty.CityId}' ran out unclaimed: {bounty.Reward} credits returned.");
                    Refund(bounty, city, BountyText.BuildExpiredText(bounty.TargetLabel, bounty.Reward));
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Failed to expire bounty '{bounty.Id}': {ex}");
                }
            }
        }

        /// <summary>
        /// A city is being removed. Its open bounties go with its treasury, escrow included. A claim that was already decided
        /// is still paid to the claimant out of the escrow it was held in.
        /// </summary>
        public static void OnCityDeleted(WorldProperty city)
        {
            if (city == null)
                return;

            foreach (var bounty in Search(new DBQuery<CityBounty>().AddFieldSearch(nameof(CityBounty.CityId), city.Id, false)))
            {
                if (bounty.State == BountyState.Paying)
                    FinishPayment(bounty, announce: false);
                else
                    DB.Delete<CityBounty>(bounty.Id);
            }
        }

        // Notices and the Gazette

        private static void TellTarget(CityBounty bounty, WorldProperty city, uint target)
        {
            SendMessageToPC(target, ColorToken.Red(BountyText.BuildTargetNotice(city.CustomName, bounty.Reward, bounty.ExpiresUtc)));
            bounty.TargetNotified = true;
            DB.Set(bounty);
        }

        /// <summary>A target who was away when a bounty was posted on them is told at their next login.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void NotifyOnLogin()
        {
            var player = GetEnteringObject();
            if (!PvpCombat.IsPlayer(player))
                return;

            DelayCommand(LoginDelaySeconds, () =>
            {
                if (!GetIsObjectValid(player))
                    return;

                foreach (var bounty in GetForPlayer(GetObjectUUID(player)).Where(x => x.State == BountyState.Active && !x.TargetNotified))
                {
                    var city = DB.Get<WorldProperty>(bounty.CityId);
                    if (city != null)
                        TellTarget(bounty, city, player);
                }
            });
        }

        /// <summary>Writes a line in the city's Gazette, under the official's title when one acted.</summary>
        private static void Announce(WorldProperty city, string text, string officialPlayerId = null)
        {
            if (city == null || string.IsNullOrWhiteSpace(text))
                return;

            var title = string.IsNullOrWhiteSpace(officialPlayerId) ? null : CityOfficers.GetTitle(city, officialPlayerId);
            CityGazette.Post(city.Id, CityGazetteEntryType.Notice, text, officialPlayerId, title);
        }

        private static List<CityBounty> Search(DBQuery<CityBounty> query)
        {
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<CityBounty>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(x => x != null)
                .ToList();
        }
    }
}
