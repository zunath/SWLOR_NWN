using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.CityBountyService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The one-time notice that cities can post bounties on an identity (Player City Revamp, "Conflict and PvP"). A player
    /// accepts it separately for their character and for each disguise, since a disguise keeps its own flags. Nobody has
    /// accepted at launch, so an identity that already shows the PvP flag sees the notice at its next login and cannot be
    /// targeted until it accepts. The notice also shows when an identity starts showing the PvP flag, and when a disguise that
    /// shows it is put on. Declining leaves the identity safe from bounties; the notice shows again at the next login while
    /// the flag stays on. The rules live in <see cref="BountyRules"/>.
    /// </summary>
    public static class BountyConsent
    {
        /// <summary>The client needs a moment after entering before a menu opens cleanly.</summary>
        private const float LoginDelaySeconds = 6f;

        /// <summary>Whether the identity the player shows should be given the notice now.</summary>
        public static bool ShouldPrompt(uint player)
        {
            if (!PvpCombat.IsPlayer(player))
                return false;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return false;

            var flagged = PlayerList.GetFlags(player).Contains(PlayerListFlagType.PvP);
            return BountyRules.ShouldShowNotice(flagged, GetAcceptedUtc(dbPlayer, Disguise.GetActiveDisguise(dbPlayer)));
        }

        /// <summary>Opens the notice when the identity the player shows needs it.</summary>
        public static void PromptIfNeeded(uint player)
        {
            if (ShouldPrompt(player))
                ConversationMenu.TryStart(player, player, nameof(BountyNoticeDialog));
        }

        /// <summary>The player accepts the notice for the identity they show.</summary>
        public static void Accept(uint player)
        {
            if (!PvpCombat.IsPlayer(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            var now = DateTime.UtcNow;
            var disguise = Disguise.GetActiveDisguise(dbPlayer);
            if (disguise != null)
            {
                if (disguise.BountyNoticeAcceptedUtc.HasValue)
                    return;

                disguise.BountyNoticeAcceptedUtc = now;
                DB.Set(disguise);
            }
            else
            {
                if (dbPlayer.BountyNoticeAcceptedUtc.HasValue)
                    return;

                dbPlayer.BountyNoticeAcceptedUtc = now;
                DB.Set(dbPlayer);
            }

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} accepted the bounty notice for {(disguise == null ? "their character" : $"disguise '{disguise.Id}'")}.");
            SendMessageToPC(player, ColorToken.Green("You accepted the bounty notice. Cities can now post a bounty on this identity while it shows the PvP flag."));
        }

        /// <summary>When the identity accepted the notice, or null: the disguise's date when one is shown, otherwise the character's.</summary>
        public static DateTime? GetAcceptedUtc(Player dbPlayer, PlayerDisguise disguise)
        {
            return disguise != null ? disguise.BountyNoticeAcceptedUtc : dbPlayer.BountyNoticeAcceptedUtc;
        }

        /// <summary>An identity that already shows the PvP flag when the feature launches sees the notice at its next login.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void PromptOnLogin()
        {
            var player = GetEnteringObject();
            if (!PvpCombat.IsPlayer(player))
                return;

            DelayCommand(LoginDelaySeconds, () =>
            {
                if (GetIsObjectValid(player))
                    PromptIfNeeded(player);
            });
        }
    }
}
