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
    /// World commands: time of day, teleporting, killing and reviving, scale, portrait and visual effects.
    /// </summary>
    public class DMWorldChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Day();
            Night();
            GetPlot();
            Kill();
            Resurrect();
            TeleportWaypoint();
            TeleportToTarget();
            SetTeleportTag();
            SetDestinationTag();
            SetPortrait();
            PlayVFX();
            GetTag();
            SetScale();
            GetScale();

            return _builder.Build();
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
                    using var recorderAttribution = CombatRecorder.BeginAttribution(user, CombatRecordSourceKind.DungeonMaster);
                    if (GetIsDead(target))
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
                        PlayerTemporaryEffects.RestoreGhostAfterRevive(target);
                        if (CombatRecorder.IsRecording)
                            CombatRecorder.RecordResurrection(target);
                        AbilityAura.ReapplyPlayerAuraAOE(target);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(999), target);
                    Stat.RestoreFP(target, Stat.GetMaxFP(target));
                    Stat.RestoreStamina(target, Stat.GetMaxStamina(target));
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

        private void TeleportToTarget()
        {
            _builder.Create("tpto")
                .Description("Teleports you to a selected creature or ground location.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget(ObjectType.Creature | ObjectType.Tile)
                .AllowsLocationTarget()
                .Action((user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionJumpToLocation(location));
                });
        }

        private void SetTeleportTag()
        {
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
        }

        private void SetDestinationTag()
        {
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

        private void PlayVFX()
        {
            _builder.Create("playvfx")
                .Description("Plays a visual effect from visualeffects.2da.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Validate((user, args) =>
                {
                    if (args.Length != 1 || !int.TryParse(args[0], out var vfxId) || vfxId < 0)
                        return "Enter a visualeffects.2da ID. Example: /playvfx 843";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                    ApplyEffectToObject(DurationType.Instant,
                        EffectVisualEffect((VisualEffect)int.Parse(args[0])), target));
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
    }
}
