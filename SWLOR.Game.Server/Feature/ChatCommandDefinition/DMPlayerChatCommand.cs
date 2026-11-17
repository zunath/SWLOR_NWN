using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Player management commands: experience, cooldowns, faction standing and testing unlocks.
    /// </summary>
    public class DMPlayerChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            GiveRPXP();
            ResetPerkCooldown();
            ResetAbilityRecastTimers();
            AdjustFactionStanding();
            GetFactionStanding();
            SetXPBonus();
            GetXPBonus();
            GetPlayerId();
            LearnAllTechniques();
            UnlockCapstoneQuests();

            return _builder.Build();
        }

        private void UnlockCapstoneQuests()
        {
            _builder.Create("unlockcapstones")
                .Description("Marks every perk-gating quest chain (capstone mastery lines) complete for yourself. Testing tool.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        SendMessageToPC(user, "Only players may unlock capstone quest gates.");
                        return;
                    }

                    var questIds = GetPerkGatingQuestChains();
                    foreach (var questId in questIds)
                    {
                        Quest.ForceCompleteQuest(user, questId);
                    }

                    Log.Write(LogGroup.DM,
                        $"Player '{GetName(user)}' ({GetObjectUUID(user)}) used unlockcapstones to mark {questIds.Count} perk-gating quest(s) complete: {string.Join(", ", questIds.OrderBy(x => x))}");

                    SendMessageToPC(user,
                        $"Marked {questIds.Count} perk-gating quest(s) complete. Quest-gated perks can now be purchased and their abilities tested.");
                });
        }

        private static HashSet<string> GetPerkGatingQuestChains()
        {
            var questIds = new HashSet<string>();
            foreach (var perk in Perk.GetAllPerks().Values)
            {
                foreach (var perkLevel in perk.PerkLevels.Values)
                {
                    foreach (var requirement in perkLevel.Requirements.OfType<PerkRequirementQuest>())
                    {
                        CollectQuestChain(requirement.QuestId, questIds);
                    }
                }
            }

            return questIds;
        }

        private static void CollectQuestChain(string questId, ISet<string> questIds)
        {
            if (string.IsNullOrWhiteSpace(questId) || questIds.Contains(questId))
                return;

            var quest = Quest.GetQuestByIdOrDefault(questId);
            if (quest == null)
                return;

            questIds.Add(questId);

            foreach (var prerequisite in quest.Prerequisites.OfType<RequiredQuestPrerequisite>())
            {
                CollectQuestChain(prerequisite.QuestId, questIds);
            }
        }

        private void LearnAllTechniques()
        {
            _builder.Create("learntechniques")
                .Description("Grants (learns) every Mimicry technique to yourself. Testing tool.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        SendMessageToPC(user, "Only players may learn Mimicry techniques.");
                        return;
                    }

                    var learnedCount = GameServices.Mimicry.GrantAllTechniques(user);
                    SendMessageToPC(user,
                        $"Learned {learnedCount} new Mimicry technique(s). Open the Techniques window to equip them.");
                });
        }

        private void GiveRPXP()
        {
            const int MaxAmount = 500000;

            _builder.Create("giverpxp", "xp")
                .Description("Gives XP to a target player or beast.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    // Missing an amount argument?
                    if (args.Length <= 0)
                    {
                        return "Please specify an amount of RP XP to give. Valid range: 1-" + MaxAmount;
                    }

                    // Can't parse the amount?
                    if (!int.TryParse(args[0], out var amount))
                    {
                        return "Please specify a valid amount between 1 and " + MaxAmount + ".";
                    }

                    // Amount is outside of our allowed range?
                    if (amount < 1 || amount > MaxAmount)
                    {
                        return "Please specify a valid amount between 1 and " + MaxAmount + ".";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var amount = int.Parse(args[0]);

                    if (GetIsPC(target) && !GetIsDM(target))
                    {
                        var playerId = GetObjectUUID(target);
                        var dbPlayer = DB.Get<Player>(playerId);
                        dbPlayer.UnallocatedXP += amount;

                        DB.Set(dbPlayer);
                        SendMessageToPC(target, $"A DM has awarded you with {amount} roleplay XP.");
                        Gui.PublishRefreshEvent(target, new RPXPRefreshEvent());
                    }
                    else if (BeastMastery.IsPlayerBeast(target))
                    {
                        var player = GetMaster(target);
                        BeastMastery.GiveBeastXP(target, amount, true);

                        SendMessageToPC(player, $"A DM has awarded your beast with {amount} XP.");
                    }
                    else
                    {
                        SendMessageToPC(user, "Only players or beasts may be targeted with this command.");
                    }

                });
        }

        private void ResetPerkCooldown()
        {
            _builder.Create("resetperkcooldown")
                .Description("Resets a player's perk refund cooldowns.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "Only players may be targeted with this command.");
                        return;
                    }

                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);
                    dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow;

                    DB.Set(dbPlayer);
                    Gui.PublishRefreshEvent(target, new PerkRefundCooldownResetRefreshEvent());
                    SendMessageToPC(target, $"A DM has reset your perk refund cooldown.");
                });
        }

        private void ResetAbilityRecastTimers()
        {
            _builder.Create("resetcooldown", "resetcooldowns")
                .Description("Resets a player's ability, disguise, introduction, and perk refund cooldowns.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(target, "Only players may be targeted with this command.");
                        return;
                    }

                    var targetName = GetName(target);
                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);
                    dbPlayer.RecastTimes ??= new();
                    dbPlayer.RecastTimes.Clear();
                    dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow;
                    DB.Set(dbPlayer);
                    Gui.PublishRefreshEvent(target, new PerkRefundCooldownResetRefreshEvent());
                    AbilityCooldownVisual.ClearAllRecastDelays(target);
                    Disguise.ResetActivationCooldowns(target, user);

                    SendMessageToPC(user, $"You have reset all of {targetName}'s cooldowns.");
                    SendMessageToPC(target, "A DM has reset all of your cooldowns.");
                });
        }

        private void AdjustFactionStanding()
        {
            _builder.Create("adjustfactionstanding")
                .Description($"Modifies a player's standing toward a particular faction. Scale ranges from {FactionService.MinimumFaction} to {FactionService.MaximumFaction}")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if(!int.TryParse(args[0], out var factionId) ||
                       ((FactionType)factionId) == FactionType.Invalid)
                    {
                        var error = "Invalid faction Id. Must be one of the following values:";
                        foreach (var (faction, detail) in GameServices.Faction.GetAllFactions())
                        {
                            error += $"{(int) faction} = {detail.Name}";
                        }

                        return error;
                    }

                    if(!int.TryParse(args[1], out var amount))
                    {
                        return $"Invalid amount. Must be a value ranging from {FactionService.MinimumFaction} to {FactionService.MaximumFaction}";
                    }

                    if (amount < FactionService.MinimumFaction || amount > FactionService.MaximumFaction)
                    {
                        return $"Invalid amount. Must be a value ranging from {FactionService.MinimumFaction} to {FactionService.MaximumFaction}";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "Only players may be targeted with this command.");
                        return;
                    }

                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);

                    foreach (var (faction, standingDetail) in dbPlayer.Factions)
                    {
                        var factionDetail = GameServices.Faction.GetFactionDetail(faction);

                        SendMessageToPC(user, $"{factionDetail.Name}: {standingDetail.Standing}");
                    }
                });
        }

        private void GetFactionStanding()
        {
            _builder.Create("getfactionstanding")
                .Description($"Retrieves a player's standing towards all factions. Scale ranges from {FactionService.MinimumFaction} to {FactionService.MaximumFaction}")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "Only players may be targeted with this command.");
                        return;
                    }

                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);

                    foreach (var (faction, standingDetail) in dbPlayer.Factions)
                    {
                        var factionDetail = GameServices.Faction.GetFactionDetail(faction);

                        SendMessageToPC(user, $"{factionDetail.Name}: {standingDetail.Standing}");
                    }
                });
        }

        private void SetXPBonus()
        {
            _builder.Create("setxpbonus")
                .Description("Sets a player's XP bonus to the specified value. Example: /setxpbonus 10")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget(ObjectType.Creature)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return "Requires a number to be entered. Example: /setxpbonus 10";
                    }

                    var amount = Convert.ToInt32(args[0]);
                    if (amount < 0 || amount > 25)
                    {
                        return "Value must be between 0 and 25.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "Only players may be targeted with this command.");
                        return;
                    }

                    var amount = Convert.ToInt32(args[0]);
                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);
                    dbPlayer.DMXPBonus = amount;

                    DB.Set(dbPlayer);

                    SendMessageToPC(user, $"{GetName(target)}'s DM XP bonus set to {amount}%.");
                    SendMessageToPC(target, $"Your DM XP bonus has been changed to {amount}%.");
                });
        }

        private void GetXPBonus()
        {
            _builder.Create("getxpbonus")
                .Description("Gets a player's DM XP bonus.")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, location, args) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "Only players may be targeted with this command.");
                        return;
                    }

                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);

                    SendMessageToPC(user, $"{GetName(target)}'s DM XP bonus is {dbPlayer.DMXPBonus}%.");
                });
        }

        private void GetPlayerId()
        {
            _builder.Create("playerid")
                .Description("Gets a player's Id.")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, location, args) =>
                {
                    var playerId = GetObjectUUID(target);

                    SendMessageToPC(user, $"{GetName(target)}'s player Id is {playerId}.");
                });
        }
    }
}
