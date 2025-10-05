using SWLOR.Component.Communication.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;
using System.Drawing;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Admin.Enums;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Communication.ValueObjects;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Events.Events.Space;
using ChatChannelType = SWLOR.NWN.API.NWNX.Enum.ChatChannelType;
using FactionType = SWLOR.Shared.Domain.Character.Enums.FactionType;

namespace SWLOR.Component.Communication.Feature.ChatCommandDefinition
{
    public class DMChatCommand: IChatCommandListDefinition
    {
        private readonly IGuiService _guiService;
        private readonly ChatCommandBuilder _builder = new();
        private readonly IDatabaseService _db;
        private readonly IAbilityService _abilityService;
        private readonly IStatService _statService;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IFactionService _faction;
        private readonly ISpaceService _space;
        private readonly IDiscordNotificationService _discord;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAdministrationPluginService _administrationPlugin;
        private readonly IChatPluginService _chatPlugin;
        

        public DMChatCommand(
            IGuiService guiService, 
            IDatabaseService db, 
            IAbilityService abilityService, 
            IStatService statService, 
            IBeastMasteryService beastMastery,
            IFactionService faction,
            ISpaceService space,
            IDiscordNotificationService discord,
            IEventAggregator eventAggregator,
            IAdministrationPluginService administrationPlugin,
            IChatPluginService chatPlugin)
        {
            _guiService = guiService;
            _db = db;
            _abilityService = abilityService;
            _statService = statService;
            _beastMastery = beastMastery;
            _faction = faction;
            _space = space;
            _discord = discord;
            _eventAggregator = eventAggregator;
            _administrationPlugin = administrationPlugin;
            _chatPlugin = chatPlugin;
        }

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
            ShipStats();
            RepairShip();

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
                        _abilityService.ReapplyPlayerAuraAOE(target);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(999), target);
                    _statService.RestoreFP(target, _statService.GetMaxFP(target));
                    _statService.RestoreStamina(target, _statService.GetMaxStamina(target));
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
                    var value = GetLocalFloat(target, variableName);

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
                        var dbPlayer = _db.Get<Player>(playerId);
                        dbPlayer.UnallocatedXP += amount;

                        _db.Set(dbPlayer);
                        SendMessageToPC(target, $"A DM has awarded you with {amount} roleplay XP.");
                        _guiService.PublishRefreshEvent(target, new RPXPRefreshEvent());
                    }
                    else if (_beastMastery.IsPlayerBeast(target))
                    {
                        var player = GetMaster(target);
                        _beastMastery.GiveBeastXP(target, amount, true);

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
                    var dbPlayer = _db.Get<Player>(playerId);
                    dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow;

                    _db.Set(dbPlayer);
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
                        var unused = (VisualEffectType) vfxId;
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
                    var vfx = (VisualEffectType) vfxId;
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
                    var dbPlayer = _db.Get<Player>(playerId);
                    dbPlayer.RecastTimes.Clear();
                    _db.Set(dbPlayer);
                    
                    SendMessageToPC(user, $"You have reset all of {targetName}'s cooldowns.");
                    SendMessageToPC(target, "A DM has reset all of your cooldowns.");
                });
        }

        private void AdjustFactionStanding()
        {
            _builder.Create("adjustfactionstanding")
                .Description($"Modifies a player's standing toward a particular faction. Scale ranges from {_faction.MinimumFaction} to {_faction.MaximumFaction}")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if(!int.TryParse(args[0], out var factionId) ||
                       ((FactionType)factionId) == FactionType.Invalid)
                    {
                        var error = "Invalid faction Id. Must be one of the following values:";
                        foreach (var (faction, detail) in _faction.GetAllFactions())
                        {
                            error += $"{(int) faction} = {detail.Name}";
                        }

                        return error;
                    }

                    if(!int.TryParse(args[1], out var amount))
                    {
                        return $"Invalid amount. Must be a value ranging from {_faction.MinimumFaction} to {_faction.MaximumFaction}";
                    }

                    if (amount < _faction.MinimumFaction || amount > _faction.MaximumFaction)
                    {
                        return $"Invalid amount. Must be a value ranging from {_faction.MinimumFaction} to {_faction.MaximumFaction}";
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
                    var dbPlayer = _db.Get<Player>(playerId);

                    foreach (var (faction, standingDetail) in dbPlayer.Factions)
                    {
                        var factionDetail = _faction.GetFactionDetail(faction);

                        SendMessageToPC(user, $"{factionDetail.Name}: {standingDetail.Standing}");
                    }
                });
        }

        private void GetFactionStanding()
        {
            _builder.Create("getfactionstanding")
                .Description($"Retrieves a player's standing towards all factions. Scale ranges from {_faction.MinimumFaction} to {_faction.MaximumFaction}")
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
                    var dbPlayer = _db.Get<Player>(playerId);

                    foreach (var (faction, standingDetail) in dbPlayer.Factions)
                    {
                        var factionDetail = _faction.GetFactionDetail(faction);

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
                    _administrationPlugin.ShutdownServer();
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
                    var dbPlayer = _db.Get<Player>(playerId);
                    dbPlayer.DMXPBonus = amount;

                    _db.Set(dbPlayer);

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
                    var dbPlayer = _db.Get<Player>(playerId);
                    
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
                    _guiService.TogglePlayerWindow(user, GuiWindowType.AreaNotes);
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
                    _guiService.TogglePlayerWindow(user, GuiWindowType.CreatureManager);
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
                    for (var onlinePlayer = GetFirstPC(); GetIsObjectValid(onlinePlayer); onlinePlayer = GetNextPC())
                        _chatPlugin.SendMessage(ChatChannelType.DMShout, message, user, onlinePlayer);
                    
                    var authorName = $"{GetName(user)} ({GetPCPlayerName(user)}) [{GetPCPublicCDKey(user)}]";
                    _discord.PublishMessage(authorName, message, Color.Orange, DiscordNotificationType.DMShout);
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

                    SetObjectVisualTransform(target, ObjectVisualTransformType.Scale, finalValue);

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
                    var targetScale = GetObjectVisualTransform(target, ObjectVisualTransformType.Scale);
                    var targetName = GetName(target);
                    var shownScale = targetScale.ToString("0.###");

                    SendMessageToPC(user, $"{targetName} has a scale of {shownScale}.");
                });
        }

        private void ShipStats()
        {
            _builder.Create("shipstats")
                .Description("Provides a readout of a given ship's stats.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if (GetIsDM(target) != true && GetIsDMPossessed(target) != true)
                    {
                        if (_space.GetShipStatus(target) != null)
                        {
                            var npcStats = _statService.GetNPCStats(target);
                            var level = npcStats.Level;
                            if (GetIsPC(target) == true)
                            {
                                var playerId = GetObjectUUID(target);
                                var dbPlayer = _db.Get<Player>(playerId);
                                level = dbPlayer.Skills[SkillType.Piloting].Rank;
                            }
                            var targetName = GetName(target);
                            var targetStatus = _space.GetShipStatus(target);
                            SendMessageToPC(user, $"{targetName} stats: \n" +
                                $"Armor: {targetStatus.Hull} Max: {targetStatus.MaxHull} \n" +
                                $"Shields: {targetStatus.Shield} Max: {targetStatus.MaxShield} \n" +
                                $"Capacitor: {targetStatus.Capacitor} Max: {targetStatus.MaxCapacitor} \n" +
                                $"Shield Regen: {targetStatus.ShieldRechargeRate} \n" +
                                $"EM Damage Bonus: {targetStatus.EMDamage} \n" +
                                $"Thermal Damage Bonus: {targetStatus.ThermalDamage} \n" +
                                $"Explosive Damage Bonus: {targetStatus.ExplosiveDamage} \n" +
                                $"Accuracy: {targetStatus.Accuracy} \n" +
                                $"Evasion: {targetStatus.Evasion} \n" +
                                $"Thermal Defense: {targetStatus.ThermalDefense} \n" +
                                $"EM Defense: {targetStatus.EMDefense} \n" +
                                $"Explosive Defense: {targetStatus.ExplosiveDefense} \n" +
                                $"Skill Level: {level}");
                        }
                    }
                });
        }

        private void RepairShip()
        {
            _builder.Create("repairship")
                .Description("Restores a ship's armor, shield and capacitor by the indicated amount.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if (_space.GetShipStatus(target) != null && !GetIsDM(target) && !GetIsDMPossessed(target))
                    {
                        var targetStatus = _space.GetShipStatus(target);

                        if (args.Length <= 0)
                        {
                            targetStatus.Capacitor = targetStatus.MaxCapacitor;
                            targetStatus.Hull = targetStatus.MaxHull;
                            targetStatus.Shield = targetStatus.MaxShield;
                        }
                        else if (int.TryParse(args[0], out var amount))
                        {
                            _space.RestoreHull(target, _space.GetShipStatus(target), amount);
                            _space.RestoreShield(target, _space.GetShipStatus(target), amount);
                            _space.RestoreCapacitor(target, _space.GetShipStatus(target), amount);
                        }

                        if (GetIsPC(target))
                        {
                            var targetPlayerId = GetObjectUUID(target);
                            var dbTargetPlayer = _db.Get<Player>(targetPlayerId);
                            var dbPlayerShip = _db.Get<PlayerShip>(dbTargetPlayer.ActiveShipId);

                            dbPlayerShip.Status.Shield = targetStatus.Shield;
                            dbPlayerShip.Status.Hull = targetStatus.Hull;

                            _db.Set(dbPlayerShip);
                            
                            // Trigger UI refresh events after database update
                            _eventAggregator.Publish(new OnPlayerShieldAdjusted(), target);
                            _eventAggregator.Publish(new OnPlayerHullAdjusted(), target);
                            _eventAggregator.Publish(new OnPlayerCapAdjusted(), target);
                        }
                    }
                });
        }

    }
}
