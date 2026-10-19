using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// Engine-free player list rules: which actions apply to a selection, the status marker, and search matching.
    /// </summary>
    public static class PlayerListRules
    {
        public const string SelectPlayerFirst = "Select a player first.";
        public const string UnassignedName = "Unassigned";
        // The marker the native player list showed through the name override.
        public const string AfkMarker = " [AFK]";
        public const string DMsCannotUsePartyControls = "DMs cannot use party controls.";

        /// <summary>
        /// DMs appear only to other DMs. Players never see a DM in the list, its count, or its actions.
        /// </summary>
        public static bool IsVisibleTo(bool viewerIsDM, bool subjectIsDM)
        {
            return !subjectIsDM || viewerIsDM;
        }

        /// <summary>
        /// Whether a row shows its public descriptor. Staff always see it, and so does everyone for players
        /// they have not named. Players who hide descriptors in Settings see none beside their own name or a
        /// name they assigned.
        /// </summary>
        public static bool ShowsDescriptor(bool viewerIsDM, bool isSelf, bool isNamed, bool showOwnDescriptor, bool showNamedDescriptors)
        {
            if (viewerIsDM)
                return true;

            if (isSelf)
                return showOwnDescriptor;

            return !isNamed || showNamedDescriptors;
        }

        /// <summary>
        /// Maps every non-DM character to the leader of their party. Characters share a party only when the
        /// engine lists them in the same player-party faction. GetFactionLeader alone is not proof: DM faction
        /// tools put players into NPC factions, whose leader is the same creature for everyone in them. A
        /// character outside a party leads their own party of one.
        /// </summary>
        public static Dictionary<uint, uint> GetPartyLeaders(IReadOnlyList<PlayerListPartyMembership> memberships)
        {
            var leaders = new Dictionary<uint, uint>();
            foreach (var membership in memberships)
            {
                if (membership.IsDM)
                    continue;

                leaders[membership.Player] = membership.Player;
                if (membership.PartyFactionId is not { } factionId)
                    continue;

                // The engine's leader names the party when the faction really lists it; a DM avatar can lead.
                var leader = memberships.FirstOrDefault(other =>
                    other.Player == membership.FactionLeader && other.PartyFactionId == factionId);
                leader ??= memberships.First(other => !other.IsDM && other.PartyFactionId == factionId);
                leaders[membership.Player] = leader.Player;
            }

            return leaders;
        }

        public static PlayerListActions Evaluate(PlayerListRelation relation)
        {
            return new PlayerListActions
            {
                SendTell = EvaluateSendTell(relation),
                ToggleHostile = EvaluateToggleHostile(relation),
                ToggleHostileText = relation.HasTarget && relation.ViewerDislikesTarget ? "Clear Hostile" : "Mark Hostile",
                Invite = EvaluateInvite(relation),
                Remove = EvaluateRemove(relation),
                MakeLeader = EvaluateMakeLeader(relation),
                LeaveParty = EvaluateLeaveParty(relation),
                JumpTo = EvaluateJumpTo(relation),
            };
        }

        public static PlayerListStatus GetStatus(PlayerListRelation relation)
        {
            if (relation.TargetIsDM)
                return PlayerListStatus.DM;

            if (relation.TargetIsSelf)
            {
                if (!relation.ViewerIsInParty)
                    return PlayerListStatus.None;

                return relation.ViewerIsPartyLeader ? PlayerListStatus.Leader : PlayerListStatus.Party;
            }

            if (relation.TargetIsInViewerParty)
                return relation.TargetIsPartyLeader ? PlayerListStatus.Leader : PlayerListStatus.Party;

            if (relation.ViewerDislikesTarget)
                return PlayerListStatus.Hostile;

            return relation.TargetIsInOtherParty ? PlayerListStatus.Grouped : PlayerListStatus.None;
        }

        /// <summary>
        /// The name a row shows: the viewer's own name marked "(You)", an assigned name, or Unassigned, with the AFK
        /// marker while the player is away. Long names are shortened before the marker so it always shows.
        /// </summary>
        public static string GetRowName(string name, bool isSelf, bool isAfk, int maxLength = int.MaxValue)
        {
            var rowName = isSelf ? $"{name} (You)" : string.IsNullOrEmpty(name) ? UnassignedName : name;
            return isAfk
                ? DMListText.Fit(rowName, Math.Max(0, maxLength - AfkMarker.Length)) + AfkMarker
                : DMListText.Fit(rowName, maxLength);
        }

        /// <summary>
        /// Returns true when every word of the search starts a word in one of the fields. An empty search matches everyone.
        /// </summary>
        public static bool MatchesSearch(string search, params string[] fields)
        {
            var searchWords = GetWords(search);
            if (searchWords.Count == 0)
                return true;

            var fieldWords = fields.SelectMany(GetWords).ToList();
            return searchWords.All(searchWord =>
                fieldWords.Any(fieldWord => fieldWord.StartsWith(searchWord, StringComparison.OrdinalIgnoreCase)));
        }

        // Apostrophes and hyphens stay inside words so "twil" finds "Twi'lek".
        private static List<string> GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var words = new List<string>();
            var start = -1;
            for (var index = 0; index <= text.Length; index++)
            {
                var isWordCharacter = index < text.Length &&
                                      (char.IsLetterOrDigit(text[index]) || text[index] == '\'' || text[index] == '-');
                if (isWordCharacter && start < 0)
                {
                    start = index;
                }
                else if (!isWordCharacter && start >= 0)
                {
                    words.Add(text[start..index]);
                    start = -1;
                }
            }

            return words;
        }

        private static PlayerListActionState EvaluateSendTell(PlayerListRelation relation)
        {
            if (!relation.HasTarget)
                return PlayerListActionState.Disabled(SelectPlayerFirst);

            if (relation.TargetIsSelf)
                return PlayerListActionState.Disabled("You cannot send a tell to yourself.");

            return PlayerListActionState.Enabled();
        }

        private static PlayerListActionState EvaluateToggleHostile(PlayerListRelation relation)
        {
            if (!relation.HasTarget)
                return PlayerListActionState.Disabled(SelectPlayerFirst);

            if (relation.ViewerIsDM || relation.TargetIsDM)
                return PlayerListActionState.Disabled("DMs cannot be marked hostile.");

            if (relation.TargetIsSelf)
                return PlayerListActionState.Disabled("You cannot mark yourself hostile.");

            if (relation.TargetIsInViewerParty)
                return PlayerListActionState.Disabled("You cannot mark members of your party hostile.");

            return PlayerListActionState.Enabled();
        }

        private static PlayerListActionState EvaluateInvite(PlayerListRelation relation)
        {
            if (!relation.HasTarget)
                return PlayerListActionState.Disabled(SelectPlayerFirst);

            if (relation.ViewerIsDM || relation.TargetIsDM)
                return PlayerListActionState.Disabled(DMsCannotUsePartyControls);

            if (relation.TargetIsSelf)
                return PlayerListActionState.Disabled("You cannot invite yourself.");

            if (relation.TargetIsInViewerParty)
                return PlayerListActionState.Disabled("Already in your party.");

            if (relation.TargetIsInOtherParty)
                return PlayerListActionState.Disabled("Already in another party.");

            if (relation.ViewerIsInParty && !relation.ViewerIsPartyLeader)
                return PlayerListActionState.Disabled("Only your party leader can invite players.");

            return PlayerListActionState.Enabled();
        }

        private static PlayerListActionState EvaluateRemove(PlayerListRelation relation)
        {
            if (!relation.HasTarget)
                return PlayerListActionState.Disabled(SelectPlayerFirst);

            if (relation.ViewerIsDM || relation.TargetIsDM)
                return PlayerListActionState.Disabled(DMsCannotUsePartyControls);

            if (relation.TargetIsSelf)
                return PlayerListActionState.Disabled("Use Leave Party to leave your party.");

            if (!relation.TargetIsInViewerParty)
                return PlayerListActionState.Disabled("Not in your party.");

            if (!relation.ViewerIsPartyLeader)
                return PlayerListActionState.Disabled("Only your party leader can remove members.");

            return PlayerListActionState.Enabled();
        }

        private static PlayerListActionState EvaluateMakeLeader(PlayerListRelation relation)
        {
            if (!relation.HasTarget)
                return PlayerListActionState.Disabled(SelectPlayerFirst);

            if (relation.ViewerIsDM || relation.TargetIsDM)
                return PlayerListActionState.Disabled(DMsCannotUsePartyControls);

            if (relation.TargetIsSelf)
                return PlayerListActionState.Disabled(relation.ViewerIsPartyLeader
                    ? "You already lead your party."
                    : "Only your party leader can choose a new leader.");

            if (!relation.TargetIsInViewerParty)
                return PlayerListActionState.Disabled("Not in your party.");

            if (!relation.ViewerIsPartyLeader)
                return PlayerListActionState.Disabled("Only your party leader can choose a new leader.");

            return PlayerListActionState.Enabled();
        }

        private static PlayerListActionState EvaluateJumpTo(PlayerListRelation relation)
        {
            if (!relation.ViewerIsDM)
                return PlayerListActionState.Disabled("Only DMs can jump to players.");

            if (!relation.HasTarget)
                return PlayerListActionState.Disabled(SelectPlayerFirst);

            if (relation.TargetIsSelf)
                return PlayerListActionState.Disabled("You cannot jump to yourself.");

            return PlayerListActionState.Enabled();
        }

        private static PlayerListActionState EvaluateLeaveParty(PlayerListRelation relation)
        {
            if (relation.ViewerIsDM)
                return PlayerListActionState.Disabled(DMsCannotUsePartyControls);

            if (!relation.ViewerIsInParty)
                return PlayerListActionState.Disabled("You are not in a party.");

            return PlayerListActionState.Enabled();
        }
    }
}
