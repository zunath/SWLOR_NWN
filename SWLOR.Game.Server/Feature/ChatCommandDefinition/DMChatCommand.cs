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
using SWLOR.Game.Server.Service.BeastMasteryService;

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
            Broadcast();
            SetScale();
            GetScale();
            GiveProperty();
            TogglePlot();
            RemoveAllProperties();

            return _builder.Build();
        }

        private void CopyTargetItem()
        {
            _builder.Create("copyitem")
                .Description("Copies the targeted item.")
                .RequiresTarget()
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    SendMessageToPC(user, GetPlotFlag(target) ? "Target is marked plot." : "Target is NOT marked plot.");
                })
                .RequiresTarget();
        }

        private void TogglePlot()
        {
            _builder.Create("toggleplot")
                .Description("Toggle the plot flag on an item on and off.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    if(GetObjectType(target) != ObjectType.Item)
                    {
                        SendMessageToPC(user, "This command can only be used on an item.");
                    }
                    else
                    {
                        if (GetPlotFlag(target) == true)
                        {
                            SetPlotFlag(target, false);
                        }
                        else { SetPlotFlag(target, true); }
                    }
                })
                .RequiresTarget();
        }

        private void Kill()
        {
            _builder.Create("kill")
                .Description("Kills your target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, location, args) =>
                {
                    if (GetIsDead(target))
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
                        Ability.ReapplyPlayerAuraAOE(target);
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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

            _builder.Create("tptag")
                .Description("Sets a local tag on a target Teleport Object placeable.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return "Missing arguments. Format should be: /TPTag <VALUE>. Example: /TPTag DUNGEON_ENTRANCE";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (GetResRef(target) != "tele_obj")
                    {
                        SendMessageToPC(user, "This command can only be used on the Teleport Object placeable.");
                    }
                    else
                    {
                        SetTag(target, args[0]);

                        SendMessageToPC(user, "Tag set to: " + args[0] + ".");
                    }
                });

            _builder.Create("tpdest")
                .Description("Changes the destination of a selected Teleport Object placeable to point toward a given waypoint or placeable tag.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return "Missing arguments. Format should be: /destination <VALUE>. Example: /destination EventEntrance";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (GetResRef(target) != "tele_obj")
                    {
                        SendMessageToPC(user, "Target is invalid. Please target a Teleport Object placeable.");
                    }
                    else
                    {
                        SetLocalString(target, "DESTINATION", args[0]);
                        SendMessageToPC(user, "Destination tag set to " + args[0] + ".");
                    }
                }); 
        }

        private void SetPortrait()
        {
            _builder.Create("setportrait")
                .Description("Sets portrait of the target player using the string specified. (Remember to add po_ to the portrait)")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                    SendMessageToPC(target, $"A DM has reset your perk refund cooldown.");
                });
        }

        private void PlayVFX()
        {
            _builder.Create("playvfx")
                .Description("Plays a visual effect from visualeffects.2da.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
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

        private void GetTag()
        {
            _builder.Create("gettag")
                .Description("Gets a target's tag.")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
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
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.CreatureManager);
                });
        }
        private void Broadcast()
        {
            _builder.Create("broadcast", "bc")
                .Description("Sends your DM shout to Discord.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                        return "Please enter a message.";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var message = string.Join(" ", args);
                    var url = Environment.GetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL");

                    for (var onlinePlayer = GetFirstPC(); GetIsObjectValid(onlinePlayer); onlinePlayer = GetNextPC())
                        ChatPlugin.SendMessage(ChatChannel.DMShout, message, user, onlinePlayer);
                    
                    var authorName = $"{GetName(user)} ({GetPCPlayerName(user)}) [{GetPCPublicCDKey(user)}]";
                    Task.Run(async () =>
                    {
                        using (var client = new DiscordWebhookClient(url))
                        {
                            var embed = new EmbedBuilder
                            {
                                Author = new EmbedAuthorBuilder
                                {
                                    Name = authorName
                                },
                                Description = message,
                                Color = Color.Orange
                            };

                            await client.SendMessageAsync(string.Empty, embeds: new[] { embed.Build() });
                        }
                    });
                });
        }

        private void SetScale()
        {
            const int MaxAmount = 50;

            _builder.Create("setscale")
                .Description("Sets an object's scale.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    // Missing an amount argument?
                    if (args.Length <= 0)
                    {
                        return "Please specify the object's scale you want to set to. Valid range: 0.1-" + MaxAmount;
                    }

                    // Can't parse the amount?
                    if (!float.TryParse(args[0], out var value))
                    {
                        return "Please specify a value between 0.1 and " + MaxAmount + ".";
                    }

                    // Amount is outside of our allowed range?
                    if (value < 0.1f || value > MaxAmount)
                    {
                        return "Please specify a value between 0.1 and " + MaxAmount + ".";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    // Allows the scale value to be a decimal number.
                    var finalValue = float.TryParse(args[0], out var value) ? value : 1f;

                    SetObjectVisualTransform(target, ObjectVisualTransform.Scale, finalValue);

                    // Lets the DM know what he set the scale to, but round it to the third decimal place.
                    var targetName = GetName(target);
                    var shownValue = finalValue.ToString("0.###");

                    SendMessageToPC(user, $"{targetName} scaled to {shownValue}.");
                });
        }

        private void GetScale()
        {
            _builder.Create("getscale")
                .Description("Gets an object's scale.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    var targetScale = GetObjectVisualTransform(target, ObjectVisualTransform.Scale);
                    var targetName = GetName(target);
                    var shownScale = targetScale.ToString("0.###");

                    SendMessageToPC(user, $"{targetName} has a scale of {shownScale}.");
                });
        }

        private void RemoveAllProperties()
        {
            _builder.Create("removeallproperties", "removeprops")
                .Description("Remove all item properties from an item.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                    {
                    if (GetObjectType(target) != ObjectType.Item)
                    {
                        SendMessageToPC(user, "This command can only target an item.");
                    }
                    else
                    {
                        for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                        {
                            RemoveItemProperty(target, ip);
                        }
                    }
                });
        }
        
        private void GiveProperty()
        {
            _builder.Create("giveproperty", "giveprop", "gp")
                .Description("Give an item property to an item. Type '/giveproperty and target yourself for a full breakdown.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if(target == user)
                    {
                        SendMessageToPC(user, "This command adds properties to an item. \n " +
                            "The format for this is /giveproperty (or /giveprop or /gp) followed by the property's ID, followed by its value. \n" +
                            "Invalid item properties for a given item type will automatically fail, as will invalid values. \n" +
                            "These are permanent item properties, and there is not at present a way to strip item properties, using /copyitem may be wise in some cases. \n\n" +
                            "The full list of item properties: \n" +
                            "1: Defense - Physical\n" +
                            "2: Defense - Force\n" +
                            "3: Defense - Fire\n" +
                            "4: Defense - Poison\n" +
                            "5: Defense - Electrical\n" +
                            "6: Defense - Ice\n" +
                            "7: Evasion\n" +
                            "8: HP\n" +
                            "9: FP\n" +
                            "10: Stamina\n" +
                            "11: Vitality\n" +
                            "12: Social\n" +
                            "13: Willpower\n" +
                            "14: Control - Smithery\n" +
                            "15: Craftsmanship - Smithery\n" +
                            "16 and 17: Not actual item properties (applied within the view model)\n" +
                            "18: DMG - Physical\n" +
                            "19: DMG - Force\n" +
                            "20: DMG - Fire\n" +
                            "21: DMG - Poison\n" +
                            "22: DMG - Electrical\n" +
                            "23: DMG - Ice\n" +
                            "24: Might\n" +
                            "25: Perception\n" +
                            "26: Accuracy\n" +
                            "27: Recast Reduction\n" +
                            "28: Structure Bonus\n" +
                            "29: Food Bonus - HP Regen\n" +
                            "30: Food Bonus - FP Regen\n" +
                            "31: Food Bonus - STM Regen\n" +
                            "32: Food Bonus - Rest Regen\n" +
                            "33: Food Bonus - XP Bonus\n" +
                            "34: Food Bonus - Recast Reduction\n" +
                            "35: Food Bonus - Duration\n" +
                            "36: Food Bonus - HP\n" +
                            "37: Food Bonus - FP\n" +
                            "38: Food Bonus - STM\n" +
                            "39: Control - Engineering\n" +
                            "40: Craftsmanship - Engineering\n" +
                            "41: Control - Fabrication\n" +
                            "42: Craftsmanship - Fabrication\n" +
                            "43: Control - Agriculture\n" +
                            "44: Craftsmanship - Agriculture\n" +
                            "45: Module Bonus\n" +
                            "46: Starship Hull\n" +
                            "47: Starship Capacitor\n" +
                            "48: Starship Shield\n" +
                            "49: Starship Shield Recharge Rate\n" +
                            "50: Starship EM Damage\n" +
                            "51: Starship Thermal Damage\n" +
                            "52: Starship Explosive Damage\n" +
                            "53: Starship Accuracy\n" +
                            "54: Starship Evasion\n" +
                            "55: Starship Thermal Defense\n" +
                            "56: Starship Explosive Defense\n" +
                            "57: Starship EM Defense\n" +
                            "58: Agility\n" +
                            "59: Attack\n" +
                            "60: Food Bonus - Attack\n" +
                            "61: Food Bonus - Accuracy\n" +
                            "62: Food Bonus - Physical Defense\n" +
                            "63: Food Bonus - Force Defense\n" +
                            "64: Food Bonus - Poison Defense\n" +
                            "65: Food Bonus - Fire Defense\n" +
                            "66: Food Bonus - Ice Defense\n" +
                            "67: Food Bonus - Electrical Defense\n" +
                            "68: Food Bonus - Evasion \n" +
                            "69: Food Bonus - Control Smithery\n" +
                            "70: Food Bonus - Craftsmanship Smithery\n" +
                            "71: Food Bonus - Control Fabrication\n" +
                            "72: Food Bonus - Craftsmanship Fabrication\n" +
                            "73: Food Bonus - Control Engineering\n" +
                            "74: Food Bonus - Craftsmanship Engineering\n" +
                            "75: Food Bonus - Control Agriculture\n" +
                            "76: Food Bonus - Craftsmanship Agriculture\n" +
                            "77: Food Bonus - Might\n" +
                            "78: Food Bonus - Perception\n" +
                            "79: Food Bonus - Vitality\n" +
                            "80: Food Bonus - Willpower\n" +
                            "81: Food Bonus - Agility\n" +
                            "82: Food Bonus - Social\n" +
                            "83: Attack\n" +
                            "84: Force Attack\n" +
                            "102: Droid: AI Slot\n" +
                            "103: Droid: HP\n" +
                            "104: Droid: STM\n" +
                            "105: Droid: MGT\n" +
                            "106: Droid: PER\n" +
                            "107: Droid: VIT\n" +
                            "108: Droid: WIL\n" +
                            "109: Droid: AGI\n" +
                            "110: Droid: SOC\n" +
                            "111: Droid: 1-Handed\n" +
                            "112: Droid: 2-Handed\n" +
                            "113: Droid: Martial Arts\n" +
                            "114: Droid: Ranged");
                    } else if (GetObjectType(target) != ObjectType.Item)
                    {
                        SendMessageToPC(user, "This command can only target yourself or an item.");
                    } else
                    {
                        var itemProperty = int.Parse(args[0]);
                        var value = int.Parse(args[1]);
                        AddItemProperty(DurationType.Permanent, Craft.BuildItemPropertyForEnhancement(itemProperty, value), target);
                    }
                });
        }
    }
}
