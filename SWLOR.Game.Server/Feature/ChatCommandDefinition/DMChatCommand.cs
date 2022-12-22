using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.FactionService;
using Faction = SWLOR.Game.Server.Service.Faction;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SWLOR.Game.Server.Core.NWNX;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class DMChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            CopyTargetItem();
            Day();
            Night();
            GetPlot();
            Kill();
            Resurrect();
            SpawnGold();
            TeleportWaypoint();
            GetLocalVariable();
            SetLocalVariable();
            SetPortrait();
            SpawnItem();
            GiveRPXP();
            ResetPerkCooldown();
            PlayVFX();
            ResetAbilityRecastTimers();
            AdjustFactionStanding();
            GetFactionStanding();
            RestartServer();
            SetXPBonus();
            GetXPBonus();
            GetPlayerId();
            GetTag();
            Notes();
            CreatureManager();
            DMBroadcast();

            return _builder.Build();
        }

        private void CopyTargetItem()
        {
            _builder.Create("copyitem")
                .Description("Copies the targeted item.")
                .RequiresTarget()
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    if (GetObjectType(target) != ObjectType.Item)
                    {
                        SendMessageToPC(user, "You can only copy items with this command.");
                        return;
                    }

                    CopyItem(target, user, true);
                    SendMessageToPC(user, "Item copied successfully.");
                });
        }

        private void Day()
        {
            _builder.Create("day")
                .Description("Sets the world time to 8 AM.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    SetTime(8, 0, 0, 0);
                });
        }

        private void Night()
        {
            _builder.Create("night")
                .Description("Sets the world time to 8 PM.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    SetTime(20, 0, 0, 0);
                });
        }

        private void GetPlot()
        {
            _builder.Create("getplot")
                .Description("Gets whether an object is marked plot.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    SendMessageToPC(user, GetPlotFlag(target) ? "Target is marked plot." : "Target is NOT marked plot.");
                })
                .RequiresTarget();
        }

        private void Kill()
        {
            _builder.Create("kill")
                .Description("Kills your target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    var amount = GetMaxHitPoints(target) + 11;
                    var damage = EffectDamage(amount);
                    ApplyEffectToObject(DurationType.Instant, damage, target);
                })
                .RequiresTarget();
        }

        private void Resurrect()
        {
            _builder.Create("rez")
                .Description("Revives you, heals you to full, and restores all FP/STM.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, location, args) =>
                {
                    if (GetIsDead(target))
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(999), target);
                    Stat.RestoreFP(target, Stat.GetMaxFP(target));
                    Stat.RestoreStamina(target, Stat.GetMaxStamina(target));
                });
        }

        private void SpawnGold()
        {
            _builder.Create("spawngold")
                .Description("Spawns gold of a specific quantity on your character. Example: /spawngold 33")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return ColorToken.Red("Please specify a quantity. Example: /spawngold 34");
                    }
                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var quantity = 1;

                    if (args.Length >= 1)
                    {
                        if (!int.TryParse(args[0], out quantity))
                        {
                            return;
                        }
                    }

                    GiveGoldToCreature(user, quantity);
                });
        }

        private void TeleportWaypoint()
        {
            _builder.Create("tpwp")
                .Description("Teleports you to a waypoint with a specified tag.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "You must specify a waypoint tag. Example: /tpwp MY_WAYPOINT_TAG";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var tag = args[0];
                    var wp = GetWaypointByTag(tag);

                    if (!GetIsObjectValid(wp))
                    {
                        SendMessageToPC(user, "Invalid waypoint tag. Did you enter the right tag?");
                        return;
                    }

                    AssignCommand(user, () => ActionJumpToLocation(GetLocation(wp)));
                });
        }

        private void GetLocalVariable()
        {
            _builder.Create("getlocalfloat")
                .Description("Gets a local float on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /GetLocalFloat Variable_Name. Example: /GetLocalFloat MY_VARIABLE";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = NWScript.GetLocalFloat(target, variableName);

                    SendMessageToPC(user, variableName + " = " + value);
                });

            _builder.Create("getlocalint")
                .Description("Gets a local integer on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /GetLocalInt Variable_Name. Example: /GetLocalInt MY_VARIABLE";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = GetLocalInt(target, variableName);

                    SendMessageToPC(user, variableName + " = " + value);
                });

            _builder.Create("getlocalstring")
                .Description("Gets a local string on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /GetLocalString Variable_Name. Example: /GetLocalString MY_VARIABLE";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = GetLocalString(target, variableName);

                    SendMessageToPC(user, variableName + " = " + value);
                });
        }

        private void SetLocalVariable()
        {
            _builder.Create("setlocalfloat")
                .Description("Sets a local float on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 2)
                    {
                        return "Missing arguments. Format should be: /SetLocalFloat Variable_Name <VALUE>. Example: /SetLocalFloat MY_VARIABLE 6.9";
                    }

                    if (!float.TryParse(args[1], out var value))
                    {
                        return "Invalid value entered. Please try again.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = args[0];
                    var value = float.Parse(args[1]);

                    SetLocalFloat(target, variableName, value);

                    SendMessageToPC(user, "Local float set: " + variableName + " = " + value);
                });


            _builder.Create("setlocalint")
                .Description("Sets a local int on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 2)
                    {
                        return "Missing arguments. Format should be: /SetLocalInt Variable_Name <VALUE>. Example: /SetLocalInt MY_VARIABLE 69";
                    }

                    if (!int.TryParse(args[1], out var value))
                    {
                        return "Invalid value entered. Please try again.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = args[0];
                    var value = Convert.ToInt32(args[1]);

                    SetLocalInt(target, variableName, value);

                    SendMessageToPC(user, "Local integer set: " + variableName + " = " + value);
                });

            _builder.Create("setlocalstring")
                .Description("Sets a local string on a target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Missing arguments. Format should be: /SetLocalString Variable_Name <VALUE>. Example: /SetLocalString MY_VARIABLE My Text";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "Target is invalid. Targeting area instead.");
                        target = GetArea(user);
                    }

                    var variableName = Convert.ToString(args[0]);
                    var value = string.Empty;

                    for (var x = 1; x < args.Length; x++)
                    {
                        value += " " + args[x];
                    }

                    value = value.Trim();

                    SetLocalString(target, variableName, value);

                    SendMessageToPC(user, "Local string set: " + variableName + " = " + value);
                });
        }

        private void SetPortrait()
        {
            _builder.Create("setportrait")
                .Description("Sets portrait of the target player using the string specified. (Remember to add po_ to the portrait)")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return "Please enter the name of the portrait and try again. Example: /SetPortrait po_myportrait";
                    }

                    if (args[0].Length > 16)
                    {
                        return "The portrait you entered is too long. Portrait names should be between 1 and 16 characters.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Creature)
                    {
                        SendMessageToPC(user, "Only creatures may be targeted with this command.");
                        return;
                    }

                    SetPortraitResRef(target, args[0]);
                    FloatingTextStringOnCreature("Your portrait has been changed.", target, false);
                });
        }

        private void SpawnItem()
        {
            _builder.Create("spawnitem")
                .Description("Spawns an item of a specific quantity on your character. Example: /spawnitem my_item 3")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return ColorToken.Red("Please specify a resref and optionally a quantity. Example: /spawnitem my_resref 20");
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var resref = args[0];
                    var quantity = 1;

                    if (args.Length > 1)
                    {
                        if (!int.TryParse(args[1], out quantity))
                        {
                            return;
                        }
                    }

                    var item = CreateItemOnObject(resref, user, quantity);

                    if (!GetIsObjectValid(item))
                    {
                        SendMessageToPC(user, ColorToken.Red("Item not found! Did you enter the correct ResRef?"));
                        return;
                    }

                    SetIdentified(item, true);
                });
        }

        private void GiveRPXP()
        {
            const int MaxAmount = 500000;
            
            _builder.Create("giverpxp")
                .Description("Gives Roleplay XP to a target player.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
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
                    if (!GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "Only players may be targeted with this command.");
                        return;
                    }

                    var amount = int.Parse(args[0]);
                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);
                    dbPlayer.UnallocatedXP += amount;
                    
                    DB.Set(dbPlayer);
                    SendMessageToPC(target, $"A DM has awarded you with {amount} roleplay XP.");
                    Gui.PublishRefreshEvent(target, new RPXPRefreshEvent());
                });
        }

        private void ResetPerkCooldown()
        {
            _builder.Create("resetperkcooldown")
                .Description("Resets a player's perk refund cooldowns.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
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
                    SendMessageToPC(target, $"A DM has reset your perk refund cooldown.");
                });
        }

        private void PlayVFX()
        {
            _builder.Create("playvfx")
                .Description("Plays a visual effect from visualeffects.2da.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Enter the ID from visauleffects.2da. Example: /playvfx 123";
                    }

                    if (!int.TryParse(args[0], out var vfxId))
                    {
                        return "Enter the ID from visauleffects.2da. Example: /playvfx 123";
                    }

                    try
                    {
                        var unused = (VisualEffect) vfxId;
                    }
                    catch
                    {
                        return "Enter the ID from visauleffects.2da. Example: /playvfx 123";
                    }
                    
                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var vfxId = Convert.ToInt32(args[0]);
                    var vfx = (VisualEffect) vfxId;
                    var effect = EffectVisualEffect(vfx);
                    ApplyEffectToObject(DurationType.Instant, effect, target);
                });
        }

        private void ResetAbilityRecastTimers()
        {
            _builder.Create("resetcooldown", "resetcooldowns")
                .Description("Resets a player's ability cooldowns.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
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
                    dbPlayer.RecastTimes.Clear();
                    DB.Set(dbPlayer);
                    
                    SendMessageToPC(user, $"You have reset all of {targetName}'s cooldowns.");
                    SendMessageToPC(target, "A DM has reset all of your cooldowns.");
                });
        }

        private void AdjustFactionStanding()
        {
            _builder.Create("adjustfactionstanding")
                .Description($"Modifies a player's standing toward a particular faction. Scale ranges from {Faction.MinimumFaction} to {Faction.MaximumFaction}")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if(!int.TryParse(args[0], out var factionId) ||
                       ((FactionType)factionId) == FactionType.Invalid)
                    {
                        var error = "Invalid faction Id. Must be one of the following values:";
                        foreach (var (faction, detail) in Faction.GetAllFactions())
                        {
                            error += $"{(int) faction} = {detail.Name}";
                        }

                        return error;
                    }

                    if(!int.TryParse(args[1], out var amount))
                    {
                        return $"Invalid amount. Must be a value ranging from {Faction.MinimumFaction} to {Faction.MaximumFaction}";
                    }

                    if (amount < Faction.MinimumFaction || amount > Faction.MaximumFaction)
                    {
                        return $"Invalid amount. Must be a value ranging from {Faction.MinimumFaction} to {Faction.MaximumFaction}";
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
                        var factionDetail = Faction.GetFactionDetail(faction);

                        SendMessageToPC(user, $"{factionDetail.Name}: {standingDetail.Standing}");
                    }
                });
        }

        private void GetFactionStanding()
        {
            _builder.Create("getfactionstanding")
                .Description($"Retrieves a player's standing towards all factions. Scale ranges from {Faction.MinimumFaction} to {Faction.MaximumFaction}")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
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
                        var factionDetail = Faction.GetFactionDetail(faction);

                        SendMessageToPC(user, $"{factionDetail.Name}: {standingDetail.Standing}");
                    }
                });
        }

        private void RestartServer()
        {
            _builder.Create("restartserver")
                .Description("Restarts the server. Requires CD Key to be entered. Example: /restartserver XXXXYYYY")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return "Requires CD Key to be entered. Example: /restartserver XXXXYYYY";
                    }
                    else if (String.IsNullOrWhiteSpace(args[0]))
                    {
                        return "Please enter your public CD Key to confirm the server reset. Use /cdkey to retrieve this. E.G: /restartserver XXXXYYYY";
                    }
                    else if (GetPCPublicCDKey(user) != args[0])
                    {
                        return $"Invalid public CD Key. {args[0]} does not match your CDKey:{GetPCPublicCDKey(user)}. Try again. E.G: /restartserver XXXXYYYY";
                    }
                    else
                    {
                        return string.Empty;
                    }
                })
                .Action((user, target, location, args) =>
                {
                    uint player = GetFirstPC();
                    while (player != OBJECT_INVALID)
                    {
                        BootPC(player, "The server is restarting.");
                        player = GetNextPC();
                    }
                    Core.NWNX.AdministrationPlugin.ShutdownServer();
                });
        }

        private void SetXPBonus()
        {
            _builder.Create("setxpbonus")
                .Description("Sets a player's XP bonus to the specified value. Example: /setxpbonus 10")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
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
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, location, args) =>
                {
                    var playerId = GetObjectUUID(target);
                    
                    SendMessageToPC(user, $"{GetName(target)}'s player Id is {playerId}.");
                });
        }

        private void GetTag()
        {
            _builder.Create("gettag")
                .Description("Gets a target's tag.")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    var tag = NWScript.GetTag(target);

                    SendMessageToPC(user, $"Target's tag: {tag}");
                });
        }

        private void Notes()
        {
            _builder.Create("notes", "note")
                .Description("Toggles the area notes window.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.AreaNotes);
                });
        }

        private void CreatureManager()
        {
            _builder.Create("cm")
                .Description("Toggles the Creature Manager window.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.CreatureManager);
                });
        }

        private void DMBroadcast()
        {
            _builder.Create("bc")
                .Description("Sends your DM shout to Discord.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    var dmMessage = string.Join(" ", args);
                    var url = Environment.GetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL");

                        for (var onlinePlayer = GetFirstPC(); GetIsObjectValid(onlinePlayer); onlinePlayer = GetNextPC())
                            ChatPlugin.SendMessage(ChatChannel.DMShout, dmMessage, user, onlinePlayer);

                    Task.Run(async () =>
                    {
                        using (var client = new DiscordWebhookClient(url))
                        {
                            var embed = new EmbedBuilder
                            {
                                Description = dmMessage,
                                Color = Color.Orange
                            };

                            await client.SendMessageAsync(string.Empty, embeds: new[] { embed.Build() });
                        }
                    });
                });
        }
    }
}
