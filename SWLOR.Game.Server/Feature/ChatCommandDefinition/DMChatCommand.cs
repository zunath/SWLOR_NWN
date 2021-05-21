using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.FactionService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Faction = SWLOR.Game.Server.Service.Faction;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class DMChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            _builder.Create("copyitem")
                .Description("Copies the targeted item.")
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

            _builder.Create("day")
                .Description("Sets the world time to 8 AM.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    SetTime(8, 0, 0, 0);
                });

            _builder.Create("night")
                .Description("Sets the world time to 8 PM.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    SetTime(20, 0, 0, 0);
                });

            _builder.Create("getplot")
                .Description("Gets whether an object is marked plot.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    SendMessageToPC(user, GetPlotFlag(target) ? "Target is marked plot." : "Target is NOT marked plot.");
                })
                .RequiresTarget();

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

            _builder.Create("name")
                .Description("Renames your target.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) => args.Length <= 0 ? "Please enter a name. Example: /name My Creature" : string.Empty)
                .Action((user, target, location, args) =>
                {
                    if (GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "PCs cannot be targeted with this command.");
                        return;
                    }

                    var name = string.Empty;
                    foreach (var arg in args)
                    {
                        name += " " + arg;
                    }

                    SetName(target, name);
                })
                .RequiresTarget();

            _builder.Create("rez")
                .Description("Revives you, heals you to full, and restores all FP/STM.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    if (GetIsDead(user))
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectResurrection(), user);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(999), user);
                    Stat.RestoreFP(user, Stat.GetMaxFP(user));
                    Stat.RestoreStamina(user, Stat.GetMaxStamina(user));
                });

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
            
            return _builder.Build();
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
            const int MaxAmount = 10000;
            
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
                    
                    DB.Set(playerId, dbPlayer);
                    SendMessageToPC(target, $"A DM has awarded you with {amount} roleplay XP.");
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

                    DB.Set(playerId, dbPlayer);
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
                    DB.Set(playerId, dbPlayer);
                    
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
                    if (args.Length < 0)
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
                    Core.NWNX.Administration.ShutdownServer();
                });
        }
    }
}
